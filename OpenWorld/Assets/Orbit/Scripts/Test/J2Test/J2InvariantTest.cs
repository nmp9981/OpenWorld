using UnityEngine;
using MathD.Integration;
using SM = System.Math;
using static IntegratorUtility;

/// <summary>
/// 2번 그룹: J2 보존량 테스트 (k = z 고정)
///  8. 수정 에너지 E'  — 심플렉틱: 유계 / 비심플렉틱: 증가
///  9. 축각운동량 h_k  — 심플렉틱: 반올림 수준으로 정확 보존 / 비심플렉틱: 드리프트
/// 10. 대조군: 전체 h의 xy 성분이 회전(세차)하는지
/// 단위: 정준 단위 (DU = R_E, μ = 1)
/// </summary>
public class J2InvariantTest : MonoBehaviour
{
    const double Mu = 1.0;
    const double Re = 1.0;
    const double J2 = 1.08262668e-3;
    const double Deg = SM.PI / 180.0;

    // 궤도: a = 1.2 DU, e = 0.1 (근지점 고도 약 510 km), i = 51.6°
    const double A = 1.2, E = 0.1, Inc = 51.6 * Deg;
    const int Orbits = 200;
    const int StepsPerOrbit = 500;
    const int SampleEvery = 10;

    static readonly Vector3D K = new Vector3D(0, 0, 1);

    delegate void StepFn(ref Vector3D r, ref Vector3D v, double dt, AccelFunc f);

    int passCount, failCount;

    void Start()
    {
        passCount = failCount = 0;
        Debug.Log("════════ J2 보존량 테스트 시작 ════════");

        AccelFunc accel = r => Gravity.TwoBody(r, Mu) + Perturbation.J2Accel(r, K, Mu, Re, J2);

        // 적분기 이름은 실제 IntegratorUtility에 맞게 조정
        Run("ExplicitEuler", (ref Vector3D r, ref Vector3D v, double dt, AccelFunc f) => ExplicitEuler(ref r, ref v, dt, f), accel, symplectic: false);
        Run("SymplecticEuler", (ref Vector3D r, ref Vector3D v, double dt, AccelFunc f) => SymplecticEuler(ref r, ref v, dt, f), accel, symplectic: true);
        Run("VelocityVerlet", (ref Vector3D r, ref Vector3D v, double dt, AccelFunc f) => VelocityVerlet(ref r, ref v, dt, f), accel, symplectic: true);
        Run("RK4", (ref Vector3D r, ref Vector3D v, double dt, AccelFunc f) => RK4(ref r, ref v, dt, f), accel, symplectic: false);

        string summary = $"════════ 결과: {passCount} 통과 / {failCount} 실패 ════════";
        if (failCount == 0) Debug.Log(summary); else Debug.LogError(summary);
    }

    void Run(string name, StepFn step, AccelFunc accel, bool symplectic)
    {
        Debug.Log($"───── {name} ({(symplectic ? "심플렉틱" : "비심플렉틱")}) ─────");

        // 근지점에서 시작. 속도를 x축 기준으로 Inc만큼 기울여 경사 궤도 생성
        var r = new Vector3D(A * (1 - E), 0, 0);
        double vp = SM.Sqrt(Mu / A * (1 + E) / (1 - E));
        var v = new Vector3D(0, vp * SM.Cos(Inc), vp * SM.Sin(Inc));

        double period = 2 * SM.PI * SM.Sqrt(A * A * A / Mu);
        double dt = period / StepsPerOrbit;
        int totalSteps = Orbits * StepsPerOrbit;

        double e0 = Perturbation.ModifiedEnergy(r, v, K, Mu, Re, J2);
        double hk0 = Perturbation.AxialAngularMomentum(r, v, K);

        // 구간(4등분)별 최대 상대오차
        double[] maxDE = new double[4];
        double[] maxDH = new double[4];

        // 대조군: h의 xy 투영 방향 각도 (unwrap 누적)
        Vector3D h0 = Vector3D.Cross(r, v);
        double prevAng = SM.Atan2(h0.y, h0.x);
        double totalRot = 0;
        double maxDEEarly = 0;

        for (int s = 1; s <= totalSteps; s++)
        {
            step(ref r, ref v, dt, accel);
            if (s % SampleEvery != 0) continue;

            int q = SM.Min(3, (s - 1) * 4 / totalSteps);

            double de = SM.Abs((Perturbation.ModifiedEnergy(r, v, K, Mu, Re, J2) - e0) / e0);
            if (s <= totalSteps / 100 && de > maxDEEarly) maxDEEarly = de;
            double dh = SM.Abs((Perturbation.AxialAngularMomentum(r, v, K) - hk0) / hk0);
            if (de > maxDE[q]) maxDE[q] = de;
            if (dh > maxDH[q]) maxDH[q] = dh;

            Vector3D h = Vector3D.Cross(r, v);
            double ang = SM.Atan2(h.y, h.x);
            double d = ang - prevAng;
            if (d > SM.PI) d -= 2 * SM.PI;
            if (d < -SM.PI) d += 2 * SM.PI;
            totalRot += d;
            prevAng = ang;
        }

        // ── 8. E': 마지막 구간 최대 / 첫 구간 최대 비율로 유계·증가 판정
        double ratioE = maxDE[3] / maxDEEarly;
        bool boundedE = ratioE < 2.0;
        Debug.Log($"  E'  구간별 최대 |ΔE'/E'|: {maxDE[0]:E2} | {maxDE[1]:E2} | {maxDE[2]:E2} | {maxDE[3]:E2}  (Q4/Q1 = {ratioE:F2} → {(boundedE ? "유계" : "증가")})");
        Check($"8. {name} E' 거동", boundedE == symplectic,
              symplectic ? "유계 기대" : "증가 기대");

        // ── 9. h_k: 심플렉틱은 반올림 수준, 비심플렉틱은 드리프트
        double maxH = SM.Max(SM.Max(maxDH[0], maxDH[1]), SM.Max(maxDH[2], maxDH[3]));
        Debug.Log($"  h_k 구간별 최대 |Δh_k/h_k|: {maxDH[0]:E2} | {maxDH[1]:E2} | {maxDH[2]:E2} | {maxDH[3]:E2}");
        if (symplectic)
            Check($"9. {name} h_k 정확 보존", maxH < 1e-11, $"최대 {maxH:E2} < 1E-11 기대 (반올림 누적 수준)");
        else
            Check($"9. {name} h_k 드리프트", maxDH[3] > maxDH[0], $"Q4 > Q1 기대");

        // ── 10. 대조군: h의 xy 성분 회전 (세차)
        double rotDeg = totalRot / Deg;
        double p = A * (1 - E * E);
        double n = SM.Sqrt(Mu / (A * A * A));
        double omegaDotRef = -1.5 * n * J2 * (Re / p) * (Re / p) * SM.Cos(Inc);  // 1차 이론, 초기 접촉요소 기준
        double refDeg = omegaDotRef * period * Orbits / Deg;
        Debug.Log($"  h_xy 회전각: {rotDeg:F3}°   (참고: 1차 이론 Ω̇·T ≈ {refDeg:F3}°, 3번 그룹에서 정밀 비교)");
        Check($"10. {name} h 세차 발생", SM.Abs(rotDeg) > 1.0, "|회전각| > 1° 기대");
    }

    void Check(string label, bool ok, string detail)
    {
        string msg = $"[{(ok ? "PASS" : "FAIL")}] {label}  {detail}";
        if (ok) { passCount++; Debug.Log(msg); }
        else { failCount++; Debug.LogError(msg); }
    }
}
