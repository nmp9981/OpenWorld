using UnityEngine;
using NUnit.Framework;
using UnityEditor;

public class KeplerEquationTests : MonoBehaviour
{
    static readonly Vector3D X = new Vector3D(1, 0, 0);
    static readonly Vector3D Y = new Vector3D(0, 1, 0);
    static readonly Vector3D Z = new Vector3D(0, 0, 1);

    const double TOL = 1e-15;
    static int failCount;

    [MenuItem("Orbital/Check Rotation Convention")]
    public static void Run()
    {
        failCount = 0;

        Debug.Log("===== 1. 축 회전 규약 (능동 + 순환 순서) =====");
        CheckAxisRotations();

        Debug.Log("===== 2. 직교성 · 행렬식 =====");
        CheckOrthonormal();

        Debug.Log("===== 3. ToState 기하 검증 =====");
        CheckToStateGeometry();

        if (failCount == 0) Debug.Log($"<color=#4CAF50>ALL PASS</color>");
        else Debug.LogError($"{failCount}개 실패");
    }

    // ---------- 1. R1(90°)ŷ=ẑ,  R2(90°)ẑ=x̂,  R3(90°)x̂=ŷ ----------
    static void CheckAxisRotations()
    {
        double h = ConstUtility.PI / 2.0;
        AssertVec(Matrix3x3D.R1(h) * Y, Z, "R1(90°)·ŷ → ẑ");
        AssertVec(Matrix3x3D.R2(h) * Z, X, "R2(90°)·ẑ → x̂");
        AssertVec(Matrix3x3D.R3(h) * X, Y, "R3(90°)·x̂ → ŷ");
    }

    // ---------- 2. 열벡터가 정규직교인가, det = +1 인가 ----------
    static void CheckOrthonormal()
    {
        double t = 0.7;   // 임의 각도
        CheckOne(Matrix3x3D.R1(t), "R1");
        CheckOne(Matrix3x3D.R2(t), "R2");
        CheckOne(Matrix3x3D.R3(t), "R3");
    }

    static void CheckOne(Matrix3x3D R, string name)
    {
        // 단위벡터를 곱해 열벡터를 추출 (Matrix3x3D 내부 API 가정 없이)
        Vector3D c0 = R * X, c1 = R * Y, c2 = R * Z;

        AssertNear(c0.Magnitude(), 1.0, $"{name} 열0 크기");
        AssertNear(c1.Magnitude(), 1.0, $"{name} 열1 크기");
        AssertNear(c2.Magnitude(), 1.0, $"{name} 열2 크기");
        AssertNear(Vector3D.Dot(c0, c1), 0.0, $"{name} 열0·열1");
        AssertNear(Vector3D.Dot(c1, c2), 0.0, $"{name} 열1·열2");
        AssertNear(Vector3D.Dot(c2, c0), 0.0, $"{name} 열2·열0");

        double det = Vector3D.Dot(c0, Vector3D.Cross(c1, c2));
        AssertNear(det, 1.0, $"{name} det");   // -1이면 반사가 섞인 것
    }

    // ---------- 3. 축퇴 케이스로 합성 순서 검증 ----------
    static void CheckToStateGeometry()
    {
        var body = CentralBody.Earth;
        double D = ConstUtility.PI / 180.0;

        // (a) Ω=90°, i=0, ω=0, ν=0  →  근점이 승교점(경도 90°) → 위치는 +ŷ
        var oeA = new OrbitalElements { p = 10000, e = 0.1, i = 0, raan = 90 * D, argp = 0, nu = 0 };
        var sA = OrbitConverter.ToState(body, oeA);
        double rA = sA.Position.Magnitude();
        AssertVec(sA.Position / rA, Y, "(a) Ω=90° 근점 방향 → +ŷ", 1e-12);

        // (b) Ω=0, i=90°, ω=0, ν=90°  →  극궤도 1/4바퀴 → 위치는 +ẑ
        var oeB = new OrbitalElements { p = 10000, e = 0.1, i = 90 * D, raan = 0, argp = 0, nu = 90 * D };
        var sB = OrbitConverter.ToState(body, oeB);
        double rB = sB.Position.Magnitude();
        AssertVec(sB.Position / rB, Z, "(b) i=90°, ν=90° → +ẑ", 1e-12);

        // (c) 순행 궤도(i<90°)면 (r × v)·ẑ > 0  — Ω,ω,ν 무관한 불변량
        var oeC = new OrbitalElements { p = 10000, e = 0.3, i = 30 * D, raan = 40 * D, argp = 70 * D, nu = 123 * D };
        var sC = OrbitConverter.ToState(body, oeC);
        double hz = Vector3D.Cross(sC.Position, sC.Velocity).z;
        AssertTrue(hz > 0, $"(c) 순행 i=30° → hz>0", $"hz={hz:E6}");

        // (d) 역행 궤도(i>90°)면 부호 반대
        var oeD = new OrbitalElements { p = 10000, e = 0.3, i = 150 * D, raan = 40 * D, argp = 70 * D, nu = 123 * D };
        var sD = OrbitConverter.ToState(body, oeD);
        double hzD = Vector3D.Cross(sD.Position, sD.Velocity).z;
        AssertTrue(hzD < 0, $"(d) 역행 i=150° → hz<0", $"hz={hzD:E6}");
    }

    // ---------- 판정 헬퍼 ----------
    static void AssertVec(Vector3D got, Vector3D want, string msg, double tol = TOL)
    {
        double d = (got - want).Magnitude();
        Report(d < tol, msg, $"got=({got.x:F6}, {got.y:F6}, {got.z:F6})  |Δ|={d:E3}");
    }

    static void AssertNear(double got, double want, string msg, double tol = TOL)
    {
        double d = MathUtility.Abs(got - want);
        Report(d < tol, msg, $"got={got:F17}  want={want:F1}  |Δ|={d:E3}");
    }

    static void AssertTrue(bool ok, string msg, string detail) => Report(ok, msg, detail);

    static void Report(bool ok, string msg, string detail)
    {
        if (ok) Debug.Log($"<color=#4CAF50>PASS</color>  {msg}   ({detail})");
        else { Debug.LogError($"FAIL  {msg}   ({detail})"); failCount++; }
    }

    [MenuItem("Orbital/Check Orbit Propagation")]
    public static void RunPropagation()
    {
        failCount = 0;
        var body = CentralBody.Earth;
        double D = ConstUtility.PI / 180.0;

        double[] eccs = { 0.0, 0.1, 0.5, 0.9, 0.99 };

        foreach (double e in eccs)
        {
            var oe = new OrbitalElements
            {
                p = 11000.0,
                e = e,
                i = 30 * D,
                raan = 40 * D,
                argp = 70 * D,
                nu = 123 * D
            };
            var orbit = new Orbit(body, oe, epoch: 0.0);
            var s0 = orbit.StateAt(0.0);
            var sT = orbit.StateAt(orbit.Period);

            double dr = (sT.Position - s0.Position).Magnitude() / s0.Position.Magnitude();
            double dv = (sT.Velocity - s0.Velocity).Magnitude() / s0.Velocity.Magnitude();

            // 예측: M 누적오차 ε·n·T = 2πε 가 dν/dM 배로 증폭
            //       dν/dM |근점 = 1/((1-e)^1.5 (1+e)^0.5)
            double amp = 1.0 / (MathUtility.Pow(1.0 - e, 1.5) * MathUtility.Sqrt(1.0 + e));
            double bound = MathUtility.Max(50.0 * 2.22e-16 * amp, 1e-14);

            Debug.Log($"e={e,-6} Δr/r={dr:E3}  Δv/v={dv:E3}  (예측상한={bound:E3})");
            Report(dr < bound, $"e={e} 한 주기 위치 복귀", $"{dr:E3} / {bound:E3}");
            Report(dv < bound, $"e={e} 한 주기 속도 복귀", $"{dv:E3} / {bound:E3}");
        }
        Debug.Log(failCount == 0 ? "<color=#4CAF50>ALL PASS</color>" : $"{failCount}개 실패");
    }

    [MenuItem("Orbital/Check Against Vallado")]
    public static void RunVallado()
    {
        failCount = 0;
        var body = CentralBody.Earth;   // μ = 398600.4418
        double R2D = 180.0 / ConstUtility.PI;

        var sv = new StateVector
        (
            new Vector3D(6524.834, 6862.875, 6448.296),   // km
            new Vector3D(4.901327, 5.533756, -1.976341)    // km/s
        );

        var oe = OrbitConverter.ToElements(body, sv);

        Debug.Log($"p    = {oe.p:F3} km      (기대 11067.790)");
        Debug.Log($"e    = {oe.e:F6}         (기대 0.832853)");
        Debug.Log($"i    = {oe.i * R2D:F4}°   (기대 87.8709)");
        Debug.Log($"Ω    = {WrapTo2Pi(oe.raan) * R2D:F4}°   (기대 227.8982)");
        Debug.Log($"ω    = {WrapTo2Pi(oe.argp) * R2D:F4}°   (기대 53.3849)");
        Debug.Log($"ν    = {WrapTo2Pi(oe.nu) * R2D:F4}°   (기대 92.3352)");
        Debug.Log($"Geometry = {oe.Geometry}   (기대 General)");

        // 책의 유효숫자 한계에 맞춘 허용치 — 수치 정밀도가 아니라 참값 정밀도
        Report(MathUtility.Abs(oe.p - 11067.790) < 5e-3, "p", $"{oe.p:F4}");
        Report(MathUtility.Abs(oe.e - 0.832853) < 5e-6, "e", $"{oe.e:F7}");
        ReportDeg(oe.i, 87.870, "i");
        ReportDeg(oe.raan, 227.8982, "Ω");
        ReportDeg(oe.argp, 53.3849, "ω");
        ReportDeg(oe.nu, 92.3352, "ν");

        // 역변환 왕복 — 원래 (r, v)로 돌아오는가
        var sv2 = OrbitConverter.ToState(body, oe);
        double dr = (sv2.Position - sv.Position).Magnitude() / sv.Position.Magnitude();
        double dv = (sv2.Velocity - sv.Velocity).Magnitude() / sv.Velocity.Magnitude();
        Debug.Log($"왕복 Δr/r={dr:E3}  Δv/v={dv:E3}");
        Report(dr < 1e-14, "왕복 위치", $"{dr:E3}");
        Report(dv < 1e-14, "왕복 속도", $"{dv:E3}");

        Debug.Log(failCount == 0 ? "<color=#4CAF50>ALL PASS</color>" : $"{failCount}개 실패");
    }

    static void ReportDeg(double rad, double wantDeg, string name)
    {
        double got = WrapTo2Pi(rad) * 180.0 / ConstUtility.PI;
        double d = MathUtility.Abs(got - wantDeg);
        Report(d < 5e-4, name, $"got={got:F4}°  want={wantDeg:F4}°  Δ={d:E2}°");
    }

    static double WrapTo2Pi(double x)
    {
        double y = MathUtility.WrapToPi(x);
        return y < 0.0 ? y + 2.0 * ConstUtility.PI : y;
    }
    [MenuItem("Orbital/Measure Kepler Iterations")]
    public static void MeasureIterations()
    {
        // 1-e 를 로그 스케일로
        double[] oneMinusE = { 1e-1, 1e-2, 1e-3, 1e-4, 1e-5, 1e-6, 1e-7, 1e-8, 1e-10, 1e-12, 1e-14 };

        Debug.Log("1-e        maxIter  @M              예측  |  실패건수");

        foreach (double ome in oneMinusE)
        {
            double e = 1.0 - ome;
            int worstIter = 0; double worstM = 0; int failures = 0;

            foreach (double M in SweepM())
            {
                if (!KeplerEquation.TrySolveElliptic(M, e, out double E, out int it))
                    failures++;
                if (it > worstIter) { worstIter = it; worstM = M; }
            }

            // 모델: clamp 소모 + 선형정체(비 2/3) + 2차수렴 마무리
            double stall = MathUtility.Log(ConstUtility.PI / MathUtility.Sqrt(2.0 * ome))
                         / MathUtility.Log(1.5);
            double pred = 3.0 + stall + 4.0;

            Debug.Log($"{ome:E0}   {worstIter,3}      {worstM:E3}     {pred:F1}    |  {failures}");
        }
    }

    // 균등 + 근점/원점 근처 로그 격자
    static System.Collections.Generic.IEnumerable<double> SweepM()
    {
        const int N = 2001;
        for (int i = 0; i < N; i++)
            yield return -ConstUtility.PI + 2.0 * ConstUtility.PI * i / (N - 1);

        for (int k = -16; k <= 0; k++)
            foreach (double s in new[] { 1.0, 2.0, 5.0 })
            {
                double u = s * MathUtility.Pow(10.0, k);
                if (u >= ConstUtility.PI) continue;
                yield return u;
                yield return -u;
                yield return ConstUtility.PI - u;
                yield return -ConstUtility.PI + u;
            }
    }
   
}
