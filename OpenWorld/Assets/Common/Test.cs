using MathD.Integration;
using UnityEngine;
using SM = System.Math;

public class Test : MonoBehaviour
{
    public static double ExactV(double t) => -MathUtility.Sin(t);
    void Start()
    {
        Tests(new ForestRuth(), "ExplicitEuler");
        Tests(new RungeKutta4(), "SymplecticEuler");
    }

    void Tests(ISymplecticIntegrator integ, string name)
    {
        Debug.Log($"───── {name} (theory {integ.Order}) ─────");
        MeasureOrder(integ);
        MeasureEnergy(integ);
    }

    // dt를 절반으로 줄일 때 오차가 몇 배 줄어드는가 → log2 = 차수
    void MeasureOrder(ISymplecticIntegrator integ)
    {
        var sys = new HarmonicOscillator();
        
        double prevErr = 0;
        for (int L = 0; L < 6; L++)
        {
            double dt = 0.1 * MathUtility.Pow(0.5, L);
            int steps = (int)(10.0 / dt + 0.5);

            Vector3D[] pos = { new Vector3D(1, 0, 0) };
            Vector3D[] vel = { Vector3D.Zero };
            integ.Prepare(1);

            for (int s = 0; s < steps; s++)
                integ.Step(sys, s * dt, dt, pos, vel);

            double dx = pos[0].x - MathUtility.Cos(steps * dt);
            double dv = vel[0].x - (-MathUtility.Sin(steps * dt));
            double err = MathUtility.Sqrt(dx * dx + dv * dv);
            //double err = MathUtility.Abs(pos[0].x - HarmonicOscillator.ExactX(steps * dt));
            string slope = (L > 0)
                ? $"slope={MathUtility.Log(prevErr / err) / MathUtility.Log(2.0):F2}"
                : "";

            Debug.Log($"  dt={dt:E2}  err={err:E3}  {slope}");
            prevErr = err;
        }
    }

    // 장시간 에너지 거동 — symplectic이면 유계, 아니면 발산
    void MeasureEnergy(ISymplecticIntegrator integ)
    {
        var sys = new HarmonicOscillator();
        integ.Prepare(1);

        Vector3D[] pos = { new Vector3D(1, 0, 0) };
        Vector3D[] vel = { Vector3D.Zero };
        double e0 = HarmonicOscillator.Energy(pos, vel);

        for (int s = 0; s < 200000; s++)
            integ.Step(sys, s * 0.05, 0.05, pos, vel);

        double e = HarmonicOscillator.Energy(pos, vel);
        Debug.Log($"  energy: {e0:F4} → {e:F4}  (ratio {e / e0:F4})");
    }
}
