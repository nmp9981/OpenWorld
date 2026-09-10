using System;
using UnityEngine;

public class IntegralTest : MonoBehaviour
{
    public enum Method { ExplicitEuler, SymplecticEuler, VelocityVerlet, RK4 }

    [Header("설정")]
    public Method method = Method.VelocityVerlet;
    [Range(0f, 0.9f)] public float eccentricity = 0.3f;
    public double dt = 2.0 * Math.PI / 2000.0;   // 주기당 2000스텝
    public int stepsPerFixedUpdate = 10;
    public float renderScale = 5f;

    [Header("관측값 (읽기 전용)")]
    public double elapsedOrbits;
    public double energyErrorNow;
    public double energyErrorMax;

    Vector3D pos, vel;
    double E0, simT;

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
}
