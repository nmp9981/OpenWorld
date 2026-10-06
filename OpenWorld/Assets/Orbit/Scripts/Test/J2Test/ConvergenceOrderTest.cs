using UnityEngine;
using MathD.Integration;
using SM = System.Math;
using static IntegratorUtility;
// Vector3D / Gravity 의 네임스페이스에 맞게 using을 추가할 것

/// <summary>
/// 0번 그룹: 새 프로젝트의 Gravity.TwoBody 기반 수렴 차수 재현
/// 케플러 궤도를 정확히 1주기 적분하면 이론적으로 초기 상태로 돌아온다
/// → 별도의 해석해(케플러 방정식) 없이 귀환 오차를 그대로 전역 오차로 사용
/// 단위: 정준 단위 (μ = 1, a = 1 → 주기 2π)
/// </summary>
public class ConvergenceOrderTest : MonoBehaviour
{
    const double Mu = 1.0;
    const double A = 1.0, E = 0.1;
    const int Levels = 6;           // N = N0, 2N0, ..., 32N0
    const double Floor = 1e-12;     // 이보다 작은 오차는 반올림 바닥 근처 → 기울기 판정에서 제외
    const double Tol = 0.15;        // 기울기 허용 오차 (이론 차수 ± 0.15)

    delegate void StepFn(ref Vector3D r, ref Vector3D v, double dt, AccelFunc f);

    int passCount, failCount;

    void Start()
    {
        passCount = failCount = 0;
        Debug.Log("════════ 수렴 차수 테스트 시작 ════════");

        AccelFunc accel = r => Gravity.TwoBody(r, Mu);

        // 적분기 이름은 실제 IntegratorUtility에 맞게 조정
        // N0는 적분기마다 점근 영역(오차가 차수대로 줄어드는 구간)에 들어가도록 다르게 잡음
        Run("ExplicitEuler", (ref Vector3D r, ref Vector3D v, double dt, AccelFunc f) => ExplicitEuler(ref r, ref v, dt, f), accel, 1, 2000);
        Run("SymplecticEuler", (ref Vector3D r, ref Vector3D v, double dt, AccelFunc f) => SymplecticEuler(ref r, ref v, dt, f), accel, 1, 2000);
        Run("VelocityVerlet", (ref Vector3D r, ref Vector3D v, double dt, AccelFunc f) => VelocityVerlet(ref r, ref v, dt, f), accel, 2, 200);
        Run("RK4", (ref Vector3D r, ref Vector3D v, double dt, AccelFunc f) => RK4(ref r, ref v, dt, f), accel, 4, 100);

        string summary = $"════════ 결과: {passCount} 통과 / {failCount} 실패 ════════";
        if (failCount == 0) Debug.Log(summary); else Debug.LogError(summary);
    }

    void Run(string name, StepFn step, AccelFunc accel, int expectedOrder, int n0)
    {
        Debug.Log($"───── {name} (이론 차수 {expectedOrder}) ─────");

        double nu = 60.0 * SM.PI / 180.0;
        double p = A * (1 - E * E);
        double rr = p / (1 + E * SM.Cos(nu));
        double vs = SM.Sqrt(Mu / p);
        var r0 = new Vector3D(rr * SM.Cos(nu), rr * SM.Sin(nu), 0);
        var v0 = new Vector3D(-vs * SM.Sin(nu), vs * (E + SM.Cos(nu)), 0);

        // 근지점 초기조건 (x-y 평면)
        //var r0 = new Vector3D(A * (1 - E), 0, 0);
        //var v0 = new Vector3D(0, SM.Sqrt(Mu / A * (1 + E) / (1 - E)), 0);
        double period = 2 * SM.PI * SM.Sqrt(A * A * A / Mu);

        double prevErr = 0;
        double slopeSum = 0;
        int slopeCount = 0;

        for (int L = 0; L < Levels; L++)
        {
            int n = n0 << L;
            double dt = period / n;      // 정확히 n 스텝 = 1주기 (누적 덧셈 오차 없음)

            Vector3D r = r0, v = v0;
            for (int s = 0; s < n; s++)
                step(ref r, ref v, dt, accel);

            double err = SM.Sqrt(SM.Pow((r - r0).Magnitude(), 2) + SM.Pow((v - v0).Magnitude(), 2));

            string slopeStr = "";
            if (L > 0)
            {
                double slope = SM.Log(prevErr / err) / SM.Log(2.0);
                bool usable = err > Floor && prevErr > Floor;
                slopeStr = $"slope={slope:F3}{(usable ? "" : "  (바닥 근처, 판정 제외)")}";

                // 마지막 3개 레벨 쌍만 판정에 사용 (점근 영역)
                if (usable && L >= Levels - 3) { slopeSum += slope; slopeCount++; }
            }

            Debug.Log($"  N={n,7}  dt={dt:E2}  err={err:E3}  {slopeStr}");
            prevErr = err;
        }

        if (slopeCount == 0)
        {
            Check($"{name} 수렴 차수", false, "판정 가능한 레벨 없음 (N0를 줄일 것)");
            return;
        }

        double meanSlope = slopeSum / slopeCount;
        Check($"{name} 수렴 차수", SM.Abs(meanSlope - expectedOrder) < Tol,
              $"평균 기울기 {meanSlope:F3} (이론 {expectedOrder} ± {Tol})");
    }

    void Check(string label, bool ok, string detail)
    {
        string msg = $"[{(ok ? "PASS" : "FAIL")}] {label}  {detail}";
        if (ok) { passCount++; Debug.Log(msg); }
        else { failCount++; Debug.LogError(msg); }
    }
}
