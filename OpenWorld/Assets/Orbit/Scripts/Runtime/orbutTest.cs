using UnityEngine;
using static IntegratorUtility;

public class orbutTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StateVector state0 = new StateVector(new Vector3D(-6045.0, -3490.0, 2500.0),
            new Vector3D(-3.457, 6.618, 2.533));

        OrbitalElements orbit = StateVector_To_OrbitElement(state0, 398600.4418);
        Debug.Log(orbit.p+" "+orbit.e+" "+orbit.i+" "+orbit.raan+" "+orbit.argp+" "+orbit.nu);
    }
    /// <summary>
    /// 적분기 연결
    /// </summary>
    void ConnectIntegralTest()
    {
        double Mu = 1.0;
        AccelFunc twoBody = r => Gravity.TwoBody(r, Mu);

        var r0 = new Vector3D(1, 0, 0);
        var v0 = new Vector3D(0, 1, 0);
        double E0 = 0.5 * v0.SqrMagnitude() - Mu / r0.Magnitude();

        int N = 100;
        double T = 2 * ConstUtility.PI;
        double dt = T / N;
        double maxDiff = 0;

        for (int s = 1; s <= 10 * N; s++)
        {
            VelocityVerlet(ref r0, ref v0, dt, twoBody);
            double energy = 0.5 * v0.SqrMagnitude() - Mu / r0.Magnitude();
            double diff = energy - E0;
            maxDiff = MathUtility.Max(maxDiff, MathUtility.Abs(diff));

            if (s % N == 0)
            {
                Debug.Log($"Energy : {maxDiff:E12}");
                maxDiff = 0;
            }
        }
    }
    /// <summary>
    /// 상태벡터 -> 궤도요소
    /// </summary>
    OrbitalElements StateVector_To_OrbitElement(StateVector state, double mu)
    {
        double r = state.Position.Magnitude();
        double v = state.Velocity.Magnitude();

        double vr = Vector3D.Dot(state.Position, state.Velocity) / r;

        Vector3D hVec = Vector3D.Cross(state.Position, state.Velocity);
        double h = hVec.Magnitude();

        double incl_Angle = MathUtility.ArkCos(hVec.z / h);

        Vector3D nVec = Vector3D.Cross(Vector3D.Forward, hVec);
        double n = nVec.Magnitude();

        double RAAN = MathUtility.ArkCos(nVec.x / n);
        if (nVec.y>=0) RAAN = MathUtility.ArkCos(nVec.x / n);
        else RAAN = 2 * ConstUtility.PI - MathUtility.ArkCos(nVec.x / n);

        Vector3D eVec = ((state.Position * (v * v - mu / r)) - (state.Velocity *r* vr)) / mu;
        double e = eVec.Magnitude();

        double w = MathUtility.ArkCos(Vector3D.Dot(nVec, eVec) / (n * e));
        if (eVec.z < 0) w = 2 * ConstUtility.PI - w;

        double trueAnomaly = MathUtility.ArkCos(Vector3D.Dot(eVec, state.Position) / (e * r));
        if (vr < 0) trueAnomaly = 2 * ConstUtility.PI - trueAnomaly;

        OrbitalElements newOb = new OrbitalElements
        {
            p = h * h / mu,
            e = e,
            i = incl_Angle,
            raan = RAAN,
            argp = w,
            nu = trueAnomaly,
            Geometry = OrbitGeometry.General
        };
        return newOb;
    }
    /// <summary>
    /// 궤도요소 -> 상태벡터
    /// </summary>
    /// <param name="orbit"></param>
    /// <returns></returns>
    StateVector OrbitElemental_To_StateVector(OrbitalElements orbit, double mu)
    {
        double r = orbit.p / (1 + orbit.e * MathUtility.Cos(orbit.nu));
        Vector3D r_x = new Vector3D(MathUtility.Cos(orbit.nu), MathUtility.Sin(orbit.nu), 0);

        double pp = MathUtility.Sqrt(1 / orbit.p * mu);
        Vector3D v_x =pp* new Vector3D(-MathUtility.Sin(orbit.nu), orbit.e+MathUtility.Cos(orbit.nu),0);

        Matrix3x3D Qxx = Matrix3x3D.R3(orbit.argp) * Matrix3x3D.R1(orbit.i) * Matrix3x3D.R3(orbit.raan);

        Vector3D rx = Qxx*r_x;
        Vector3D vx = Qxx * v_x;

        StateVector newState = new StateVector(rx, vx);
        return newState;
    }
}
