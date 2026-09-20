using System;
using System.IO;
using UnityEngine;

public class IntegralTest : MonoBehaviour
{
    public enum Method { ExplicitEuler, SymplecticEuler, VelocityVerlet, RK4 }

    [Header("설정")]
    public Method method = Method.VelocityVerlet;
    [Range(0f, 0.9f)] private float eccentricity = 0.9f;
    public double dt = 2.0 * Math.PI / 2000.0;   // 주기당 2000스텝
    public int stepsPerFixedUpdate = 10;
    public float renderScale = 5f;

    [Header("관측값 (읽기 전용)")]
    public double elapsedOrbits;
    public double energyErrorNow;
    public double energyErrorMax;

    Vector3D pos, vel;
    double E0, simT;

    [Header("RK45 적분기 테스트")]
    private double rTol = 1e-8, aTol = 1e-10;
    double T => 2.0 * ConstUtility.PI;
    CentralBody body;
    Orbit orbit;
    // 확인할 값
    long acceptedSteps = 0;             // 채택 스텝 수
    long rejectedSteps = 0;             // 거부 스텝 수
    double rejectRate;                  // = rejectedSteps / (acceptedSteps + rejectedSteps)
    long funcEvals;                     // = (acceptedSteps + rejectedSteps) * 7
    double posErr;                      // = (pos - orbit.StateAt(T).r).Magnitude()
    double energyErr;                   // = (Energy(pos, vel) - E0) / |E0|

    //csv
    StreamWriter writer;
    string filePath = "D:\\Data\\Obit";

    // ── 시스템 정의: μ=1 케플러. 유틸리티에 델리게이트로 전달 ──
    static Vector3D Gravity(Vector3D r)
    {
        double d = r.Magnitude();
        return r * (-1.0 / (d * d * d));
    }

    static double Energy(Vector3D r, Vector3D v)
        => 0.5 * v.SqrMagnitude() - 1.0 / r.Magnitude();

    void Start()
    {
        double e = eccentricity;
        pos = new Vector3D(1.0 - e, 0, 0);
        vel = new Vector3D(0, MathUtility.Sqrt((1.0 + e) / (1.0 - e)), 0);
        E0 = Energy(pos, vel);
        simT = 0;
        energyErrorMax = 0;
        body = new CentralBody(1.0, 1.0);

        writer = new StreamWriter(Path.Combine(filePath, $"RK45_e{e}.csv"));   // 한 번만
        writer.WriteLine("rTol,t,h,energyErr,posErr");


        double[] rTolList = { 1e-6, 1e-8, 1e-10, 1e-12 };
        foreach (var rt in rTolList)
        {
            rTol = rt;
            aTol = rt / 100;
            var oe = new OrbitalElements { p = 1.0 - e * e, e = e, i = 0, raan = 0, argp = 0, nu = ConstUtility.PI };
            orbit = new Orbit(body, oe, epoch: 0.0);

            var s0 = orbit.StateAt(0.0);
            pos = s0.Position;
            vel = s0.Velocity;
            E0 = Energy(pos, vel);
            RK45Test();
        }

        writer.Close(); writer = null;
        enabled = false;
    }

    void FixedUpdate()
    {
        for (int i = 0; i < stepsPerFixedUpdate; i++)
        {
            switch (method)
            {
                case Method.ExplicitEuler: IntegratorUtility.ExplicitEuler(ref pos, ref vel, dt, Gravity); break;
                case Method.SymplecticEuler: IntegratorUtility.SymplecticEuler(ref pos, ref vel, dt, Gravity); break;
                case Method.VelocityVerlet: IntegratorUtility.VelocityVerlet(ref pos, ref vel, dt, Gravity); break;
                case Method.RK4: IntegratorUtility.RK4(ref pos, ref vel, dt, Gravity); break;
            }
            simT += dt;
        }
     
        elapsedOrbits = simT / (2.0 * ConstUtility.PI);
        energyErrorNow = (Energy(pos, vel) - E0) / MathUtility.Abs(E0);
        energyErrorMax = MathUtility.Max(energyErrorMax, MathUtility.Abs(energyErrorNow));

        transform.position = new Vector3((float)pos.x, (float)pos.y, (float)pos.z) * renderScale;
    }

    void RK45Test()
    {
        double t = 0, tEnd = 10 * T, h = T / 1000;
        double hMin = double.MaxValue, hMax = 0;

        while (t < tEnd)
        {
            if (t + h > tEnd) h = tEnd - t;
            double hUsed = h;
            IntegratorUtility.RK45(ref pos, ref vel, ref h, Gravity, rTol, aTol, out bool ok);
           
            // 루프 안, 채택 시:
            if (ok)
            {
                t += hUsed;
                acceptedSteps++;
                hMin = MathUtility.Min(hMin, hUsed);
                hMax = MathUtility.Max(hMax, hUsed);
                writer.WriteLine($"{rTol},{t},{hUsed},{(Energy(pos, vel) - E0) / MathUtility.Abs(E0)},{(pos - orbit.StateAt(t).Position).Magnitude()}");
            }
            else { rejectedSteps++; }
        }

        rejectRate = (double)rejectedSteps / (acceptedSteps + rejectedSteps);
        funcEvals = (acceptedSteps + rejectedSteps) * 7;

        posErr = (pos - orbit.StateAt(T).Position).Magnitude();
        energyErr = (Energy(pos, vel) - E0) / MathUtility.Abs(E0);
    }
    void OnDestroy() => writer?.Close();

}
