using MathD.Integration;
using UnityEngine;

/// <summary>조화진동자. 해석해가 있어 차수 측정 기준으로 쓴다.</summary>
public sealed class HarmonicOscillator : ISeparableSystem
{
    public int BodyCount => 1;

    // a = -x  (ω=1, m=1)
    public void Acceleration(double t, Vector3D[] pos, Vector3D[] accelOut)
        => accelOut[0] = new Vector3D(-pos[0].x, 0, 0);

    // x0=1, v0=0 에서 출발하면 x(t) = cos t
    public static double ExactX(double t) => MathUtility.Cos(t);

    public static double Energy(Vector3D[] pos, Vector3D[] vel)
        => 0.5 * (vel[0].x * vel[0].x + pos[0].x * pos[0].x);
}