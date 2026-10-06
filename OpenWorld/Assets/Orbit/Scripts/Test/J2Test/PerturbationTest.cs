using UnityEngine;
using rng = System.Random;
using static IntegratorUtility;

public class PerturbationTest : MonoBehaviour
{
    const double Mu = 1.0;
    const double Re = 1.0;
    const double J2 = 1.08262668e-3;   // EGM2008
    const double Deg = ConstUtility.PI / 180.0;

    static readonly Vector3D Z = new Vector3D(0, 0, 1);

    int passCount, failCount;

    void Start()
    {
        passCount = failCount = 0;
        Debug.Log("════════ J2Accel 단위 테스트 시작 ════════");

        Test_J2Zero_BitIdentical();
        Test_Equator();
        Test_Pole();
        Test_VectorFormMatchesComponentForm();
        Test_GradientOfModifiedEnergy();
        Test_RotationCovariance();

        string summary = $"════════ 결과: {passCount} 통과 / {failCount} 실패 ════════";
        if (failCount == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    // ─────────────────────────────────────────────
    //  보조 함수
    // ─────────────────────────────────────────────

    /// <summary>측정값과 허용치를 같이 출력. 실패는 LogError로 눈에 띄게.</summary>
    void Report(string name, double measured, double tol, string detail = "")
    {
        bool ok = measured < tol;
        string msg = $"[{(ok ? "PASS" : "FAIL")}] {name}  측정={measured:E3}  허용치={tol:E0}  {detail}";
        if (ok) { passCount++; Debug.Log(msg); }
        else { failCount++; Debug.LogError(msg); }
    }

    /// <summary>|a - b| / |b|</summary>
    static double RelErr(in Vector3D a, in Vector3D b)
        => (a - b).Magnitude() / b.Magnitude();

    /// <summary>로드리게스 회전: 단위축 u 기준 θ 회전</summary>
    static Vector3D Rotate(in Vector3D v, in Vector3D u, double theta)
    {
        double c = MathUtility.Cos(theta), s = MathUtility.Sin(theta);
        return v * c + Vector3D.Cross(u, v) * s + u * (Vector3D.Dot(u, v) * (1.0 - c));
    }

    /// <summary>구면 위 균일 분포 단위벡터 (시드 고정 → 재현 가능)</summary>
    static Vector3D RandomUnit(System.Random rng)
    {
        double z = 2.0 * rng.NextDouble() - 1.0;
        double phi = 2.0 * System.Math.PI * rng.NextDouble();
        double s = MathUtility.Sqrt(1.0 - z * z);
        return new Vector3D(s * MathUtility.Cos(phi), s * MathUtility.Sin(phi), z);
    }

    /// <summary>|r| ∈ [1.05, 2.0] DU 무작위 위치</summary>
    static Vector3D RandomPosition(System.Random rng)
        => RandomUnit(rng) * (1.05 + 0.95 * rng.NextDouble());

    // ─────────────────────────────────────────────
    //  3. J2 = 0 비트 일치
    // ─────────────────────────────────────────────

    void Test_J2Zero_BitIdentical()
    {
        AccelFunc twoBody = r => Gravity.TwoBody(r, Mu);
        AccelFunc withJ2 = r => Gravity.TwoBody(r, Mu) + Perturbation.J2Accel(r, Z, Mu, Re, 0.0);

        // 경사 이심 궤도 초기조건 (근점, e = 0.1, i = 51.6°)
        double e = 0.1, a = 1.2;
        var r0 = new Vector3D(a * (1 - e), 0, 0);
        double vp = MathUtility.Sqrt(Mu / a * (1 + e) / (1 - e));
        var v0 = Rotate(new Vector3D(0, vp, 0), new Vector3D(1, 0, 0), 51.6 * Deg);

        Vector3D r1 = r0, v1 = v0, r2 = r0, v2 = v0;
        const double dt = 1e-3;
        for (int s = 0; s < 20000; s++)
        {
            VelocityVerlet(ref r1, ref v1, dt, twoBody);
            VelocityVerlet(ref r2, ref v2, dt, withJ2);
        }

        bool identical = r1.x == r2.x && r1.y == r2.y && r1.z == r2.z
                      && v1.x == v2.x && v1.y == v2.y && v1.z == v2.z;

        // 비트 일치는 허용치 개념이 없으므로 별도 출력
        if (identical) { passCount++; Debug.Log("[PASS] 3. J2=0 비트 일치  (20000 스텝 후 상태 완전 일치)"); }
        else
        {
            failCount++;
            Debug.LogError($"[FAIL] 3. J2=0 비트 일치  |Δr|={(r1 - r2).Magnitude():E3}  |Δv|={(v1 - v2).Magnitude():E3}");
        }
    }

    // ─────────────────────────────────────────────
    //  4. 적도면: 중심 방향, 크기 (3/2) J2 μ R² / r⁴
    // ─────────────────────────────────────────────

    void Test_Equator()
    {
        double rMag = 1.2, ang = 40.0 * Deg;
        var r = new Vector3D(rMag * MathUtility.Cos(ang), rMag * MathUtility.Sin(ang), 0);

        double mag = 1.5 * J2 * Mu * Re * Re / (rMag * rMag * rMag * rMag);
        Vector3D expected = r * (-mag / rMag);   // -mag · r̂

        Vector3D actual = Perturbation.J2Accel(r, Z, Mu, Re, J2);
        Report("4. 적도면 (중심 방향, 1.5·J2μR²/r⁴)", RelErr(actual, expected), 1e-14);
    }

    // ─────────────────────────────────────────────
    //  5. 극 방향: 바깥 방향, 크기 3 J2 μ R² / r⁴
    // ─────────────────────────────────────────────

    void Test_Pole()
    {
        double rMag = 1.2;
        var r = Z * rMag;

        double mag = 3.0 * J2 * Mu * Re * Re / (rMag * rMag * rMag * rMag);
        Vector3D expected = Z * mag;             // +mag · k̂

        Vector3D actual = Perturbation.J2Accel(r, Z, Mu, Re, J2);
        Report("5. 극 방향 (바깥 방향, 3·J2μR²/r⁴)", RelErr(actual, expected), 1e-14);
    }

    // ─────────────────────────────────────────────
    //  보강: k = z일 때 벡터형 == 교과서 성분식 (100점 중 최대 오차)
    // ─────────────────────────────────────────────

    void Test_VectorFormMatchesComponentForm()
    {
        var rng = new System.Random(12345);
        double worst = 0; int worstN = -1;

        for (int n = 0; n < 100; n++)
        {
            Vector3D r = RandomPosition(rng);
            double r2 = Vector3D.Dot(r, r), rm = MathUtility.Sqrt(r2);
            double c = -1.5 * J2 * Mu * Re * Re / (r2 * r2 * rm);
            double q = 5.0 * r.z * r.z / r2;
            var expected = new Vector3D(c * r.x * (1 - q), c * r.y * (1 - q), c * r.z * (3 - q));

            double err = RelErr(Perturbation.J2Accel(r, Z, Mu, Re, J2), expected);
            if (err > worst) { worst = err; worstN = n; }
        }
        Report("보강. 벡터형 == 성분식 (100점 최대)", worst, 1e-13, $"최악 n={worstN}");
    }

    // ─────────────────────────────────────────────
    //  6. 퍼텐셜 기울기 일치: a = -∇U  (50점 중 최대 오차)
    //     U(r) = ModifiedEnergy(r, v=0)
    //     중심차분에서 2체 항(정확값)을 빼서 J2 성분만 비교 → 민감도 확보
    // ─────────────────────────────────────────────

    void Test_GradientOfModifiedEnergy()
    {
        var rng = new System.Random(2026);
        var zero = new Vector3D(0, 0, 0);
        var ex = new Vector3D(1, 0, 0);
        var ey = new Vector3D(0, 1, 0);
        var ez = new Vector3D(0, 0, 1);

        double worst = 0; int worstN = -1;

        for (int n = 0; n < 50; n++)
        {
            Vector3D r = RandomPosition(rng);
            double h = 1e-5 * r.Magnitude();

            double gx = (U(r - ex * h) - U(r + ex * h)) / (2 * h);
            double gy = (U(r - ey * h) - U(r + ey * h)) / (2 * h);
            double gz = (U(r - ez * h) - U(r + ez * h)) / (2 * h);

            Vector3D numericJ2 = new Vector3D(gx, gy, gz) - Gravity.TwoBody(r, Mu);
            Vector3D analyticJ2 = Perturbation.J2Accel(r, Z, Mu, Re, J2);

            double err = RelErr(numericJ2, analyticJ2);
            if (err > worst) { worst = err; worstN = n; }
        }
        // 정상이면 1e-8 근처. 1e-3 근처면 계수 오류, 정확히 0.5/2.0 배 차이면 ModifiedEnergy의 0.5 계수 의심
        Report("6. a = -∇U (50점 최대, J2 성분만)", worst, 1e-6, $"최악 n={worstN}  (정상이면 ~1e-8)");

        double U(Vector3D p) => Perturbation.ModifiedEnergy(p, zero, Z, Mu, Re, J2);
    }

    // ─────────────────────────────────────────────
    //  7. 회전 불변성: J2Accel(R r, R k) == R J2Accel(r, k)  (100점 중 최대 오차)
    // ─────────────────────────────────────────────

    void Test_RotationCovariance()
    {
        var rng = new System.Random(777);
        double worst = 0; int worstN = -1;

        for (int n = 0; n < 100; n++)
        {
            Vector3D r = RandomPosition(rng);
            Vector3D k = RandomUnit(rng);          // 극축도 임의 방향
            Vector3D axis = RandomUnit(rng);
            double theta = 2.0 * System.Math.PI * rng.NextDouble();

            Vector3D lhs = Perturbation.J2Accel(Rotate(r, axis, theta), Rotate(k, axis, theta), Mu, Re, J2);
            Vector3D rhs = Rotate(Perturbation.J2Accel(r, k, Mu, Re, J2), axis, theta);

            double err = RelErr(lhs, rhs);
            if (err > worst) { worst = err; worstN = n; }
        }
        Report("7. 회전 불변성 (100점 최대)", worst, 1e-13, $"최악 n={worstN}");
    }
}
