using UnityEngine;

public readonly struct CentralBody
{
    public readonly double Mu;          // km³/s²
    public readonly double Radius;      // 2단계에서 측지좌표에 필요
    public readonly double J2;          // 3단계에서 채움
    public readonly double RotationRate;// 2단계 ECEF 변환

    public CentralBody(double mu, double radius, double j2 = 0, double rotationRate = 0)
    {
        Mu = mu;
        Radius = radius;
        J2 = j2;
        RotationRate = rotationRate;
    }

    public static readonly CentralBody Earth =
        new CentralBody(398600.4418, 6378.137, 1.08262668e-3, 7.2921159e-5);

    public static readonly CentralBody Sun =
        new CentralBody(1.32712440018e11, 695700.0);
}

public struct StateVector
{
    public Vector3D Position;   // km
    public Vector3D Velocity;   // km/s
}

public struct OrbitalElements
{
    public double p, e, i, raan, argp, nu;
}

public enum OrbitGeometry { General, CircularInclined, EllipticalEquatorial, CircularEquatorial }

public class RV_To_COV : MonoBehaviour
{
    // 기준평면(적도면)의 법선 = 자전축 방향
    static readonly Vector3D ReferenceNormal = new Vector3D(0, 0, 1);

    public static OrbitalElements ToElements(CentralBody central, StateVector state)
    {
        // 공통량 한 번만
        Vector3D r = state.Position, v = state.Velocity;
        double rMag = r.Magnitude();
        double vSq = v.SqrMagnitude();
        double rv = Vector3D.Dot(r, v);

        Vector3D h = Vector3D.Cross(r, v);
        double hMag = h.Magnitude();
        Vector3D e = ((vSq - central.Mu / rMag) * r - rv * v) / central.Mu;
        Vector3D n = Vector3D.Cross(ReferenceNormal, h);
        Vector3D m = Vector3D.Cross(h, n);

        OrbitalElements orbitalElements = new OrbitalElements();
        orbitalElements.p = hMag / central.Mu;
        orbitalElements.e = e.Magnitude();
        orbitalElements.i = MathUtility.ArkTan2(MathUtility.Sqrt(h.x * h.x + h.y * h.y), h.z);
        orbitalElements.raan = MathUtility.ArkTan2(n.y, n.x);
        orbitalElements.argp = MathUtility.ArkTan2(Vector3D.Dot(e,m), Vector3D.Dot(e, n));
        orbitalElements.nu = MathUtility.ArkTan2(hMag * rv / central.Mu, Vector3D.Dot(e, r));

        return orbitalElements;
    }

    public void COEToRV(CentralBody central, StateVector state)
    {
        OrbitalElements elements = ToElements(central, state);
        double cosNu = MathUtility.Cos(elements.nu);
        double sinNu = MathUtility.Sin(elements.nu);
        double eccentricity = elements.p/(1+elements.e*cosNu);
        double rootUP = MathUtility.Sqrt(central.Mu / elements.p);

        Vector3D rPQW = new Vector3D(cosNu,sinNu,0)*eccentricity;
        Vector3D vPQW = new Vector3D(-sinNu, elements.e+cosNu, 0) * rootUP;


    }


    #region 보존량 계산
    /// <summary>
    /// 보존량 H
    /// </summary>
    /// <returns></returns>
    Vector3D SpecificAngularMomentum(StateVector state)
    {
        return Vector3D.Cross(state.Position, state.Velocity);
    }

    /// <summary>
    /// 에너지 
    /// </summary>
    /// <param name="central"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    double SpecificMechanicalEnergy(CentralBody central, StateVector state)
    {
        double kineticEnergy = 0.5 * state.Velocity.SqrMagnitude();
        double potentialEnergy = -central.Mu / state.Position.Magnitude();
        return kineticEnergy + potentialEnergy;
    }

    /// <summary>
    ///궤도 이심률 계산
    /// </summary>
    /// <returns></returns>
    Vector3D EccentricityVector(CentralBody central, StateVector state)
    {
        double velocitySq = state.Velocity.SqrMagnitude();
        double r = state.Position.Magnitude();
        //BAC
        Vector3D BAC = (velocitySq - central.Mu / r) * state.Position;
        //CAB
        Vector3D CAB = Vector3D.Dot(state.Position, state.Velocity) * state.Velocity;

        //이심률
        Vector3D eccentricityVector = (BAC - CAB) / central.Mu;
        return eccentricityVector;
    }
    #endregion

    /// <summary>
    /// 경사각 계산
    /// </summary>
    /// <param name="h"></param>
    /// <returns></returns>
    double TiltAngle(Vector3D h)
    {
        double hz = h.z;
        double hMagXY = MathUtility.Sqrt(h.x*h.x+h.y*h.y);
        double tiltAngle = MathUtility.ArkTan2(hMagXY, hz);
        return tiltAngle;
    }

    /// <summary>
    /// 근점인수
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    double ArgumentOfPeriapsis(CentralBody central, StateVector state)
    {
        Vector3D v = state.Velocity;
        Vector3D h = SpecificAngularMomentum(state);
        Vector3D rHat = state.Position.Normalized();

        Vector3D e = Vector3D.Cross(v,h)/central.Mu - rHat;

        Vector3D n = Vector3D.Cross(ReferenceNormal, h);
        Vector3D m = Vector3D.Cross(h.Normalized(), n);
        return MathUtility.ArkTan2(Vector3D.Dot(e,m), Vector3D.Dot(e,n));
    }
    /// <summary>
    /// 승교점 직경
    /// </summary>
    /// <returns></returns>
    double LongitudeAscending(StateVector state)
    {
        Vector3D h = SpecificAngularMomentum(state);
        Vector3D n = Vector3D.Cross(ReferenceNormal, h);

        return MathUtility.ArkTan2(n.y, n.x);
    }

    /// <summary>
    /// 진금점이각
    /// </summary>
    /// <param name="central"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    double TrueAnomaly(CentralBody central, StateVector state)
    {
        Vector3D v = state.Velocity;
        Vector3D h = SpecificAngularMomentum(state);
        Vector3D rHat = state.Position.Normalized();
        Vector3D e = Vector3D.Cross(v, h) / central.Mu - rHat;

        double rv = Vector3D.Dot(state.Position, state.Velocity);
        return MathUtility.ArkTan2((h.Magnitude()*rv)/central.Mu, Vector3D.Dot(e, state.Position));
    }
}
