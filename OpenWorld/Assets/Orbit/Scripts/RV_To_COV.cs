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
    public OrbitGeometry Geometry;
}

public enum OrbitGeometry { General, CircularInclined, EllipticalEquatorial, CircularEquatorial }

public class RV_To_COV : MonoBehaviour
{
    // 기준평면(적도면)의 법선 = 자전축 방향
    static readonly Vector3D ReferenceNormal = new Vector3D(0, 0, 1);
    const double E_TOL = 1e-8;   // 측정 근거: Δω ≈ 1e-16/e, 1e-6 rad 기준
    const double I_TOL = 1e-8;   // 판단 근거: 실 위성 i가 1e-3 수준

    private void Start()
    {
        var original = new StateVector();
        original.Position = new Vector3D(7000,0,0);
        //original.Velocity = new Vector3D(0, -7.54605329010754185, 0);
        original.Velocity = new Vector3D(0, 6.5350, 3.7730);

        CentralBody central = CentralBody.Earth;
        var el = ToElements(central, original);

        Debug.Log($"Orbital Elements: { el.Geometry}, e: {el.e}, i: {el.i}, raan: {el.raan}, argp : {el.argp}, nu : {el.nu}, p : {el.p}");
        var back = ToState(central, el);

        double posErr = (back.Position - original.Position).Magnitude() / original.Position.Magnitude();
        double velErr = (back.Velocity - original.Velocity).Magnitude() / original.Velocity.Magnitude();


    }

    /// <summary>
    /// 변환
    /// </summary>
    /// <param name="central"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public static OrbitalElements ToElements(CentralBody central, StateVector state)
    {
        // 공통량 한 번만
        Vector3D r = state.Position, v = state.Velocity;
        double rMag = r.Magnitude();
        double vSq = v.SqrMagnitude();
        double rv = Vector3D.Dot(r, v);

        Vector3D h = Vector3D.Cross(r, v);
        double hMag = h.Magnitude();
        Vector3D hHat = h / hMag;
        
        Vector3D e = ((vSq - central.Mu / rMag) * r - rv * v) / central.Mu;
        Vector3D n = Vector3D.Cross(ReferenceNormal, h);
        Vector3D nHat = n / n.Magnitude();
        Vector3D mHat = Vector3D.Cross(hHat, nHat);
        double nMag = n.Magnitude();

        OrbitalElements orbitalElements = new OrbitalElements();
        bool circular = orbitalElements.e < E_TOL;// 이심률이 0에 가까운가?
        bool equatorial = nMag < I_TOL;// 승교점 직경이 0에 가까운가?

        orbitalElements.p = hMag * hMag / central.Mu;
        orbitalElements.e = e.Magnitude();
        orbitalElements.i = MathUtility.ArkTan2(MathUtility.Sqrt(h.x * h.x + h.y * h.y), h.z);
        orbitalElements.raan = MathUtility.ArkTan2(n.y, n.x);
        orbitalElements.argp = MathUtility.ArkTan2(Vector3D.Dot(e, mHat), Vector3D.Dot(e, nHat));
        orbitalElements.nu = MathUtility.ArkTan2(hMag * rv / central.Mu, Vector3D.Dot(e, r));

        if (circular && equatorial)
        {
            orbitalElements.raan = 0.0;
            orbitalElements.argp = 0.0;
            orbitalElements.nu = MathUtility.ArkTan2(r.y, r.x);
            if (h.z < 0) orbitalElements.nu = -orbitalElements.nu;
            orbitalElements.Geometry = OrbitGeometry.CircularEquatorial;
        }
        else if (circular)
        {
            nHat = n / nMag;
            mHat = Vector3D.Cross(hHat, nHat);

            orbitalElements.raan =MathUtility.ArkTan2(n.y, n.x);
            orbitalElements.argp = 0.0;
            orbitalElements.nu = MathUtility.ArkTan2(Vector3D.Dot(r, mHat), Vector3D.Dot(r, nHat));
            orbitalElements.Geometry = OrbitGeometry.CircularInclined;
        }
        else if (equatorial)
        {
            orbitalElements.raan = 0.0;
            orbitalElements.argp = MathUtility.ArkTan2(e.y, e.x);
            if (h.z < 0) orbitalElements.argp = -orbitalElements.argp;
            orbitalElements.nu = MathUtility.ArkTan2(hMag * rv /central.Mu, Vector3D.Dot(e, r));
            orbitalElements.Geometry = OrbitGeometry.EllipticalEquatorial;
        }
        else
        {
            nHat = n / nMag;
            mHat = Vector3D.Cross(hHat, nHat);

            orbitalElements.raan = MathUtility.ArkTan2(n.y, n.x);
            orbitalElements.argp = MathUtility.ArkTan2(Vector3D.Dot(e, mHat), Vector3D.Dot(e, nHat));
            orbitalElements.nu = MathUtility.ArkTan2(hMag * rv / central.Mu, Vector3D.Dot(e, r));
            orbitalElements.Geometry = OrbitGeometry.General;
        }
        return orbitalElements;
    }
    /// <summary>
    /// 역변환
    /// </summary>
    /// <param name="central"></param>
    /// <param name="elements"></param>
    /// <returns></returns>
    public static StateVector ToState(CentralBody central, OrbitalElements elements)
    {
        double cosNu = MathUtility.Cos(elements.nu);
        double sinNu = MathUtility.Sin(elements.nu);
        double rMag = elements.p/(1+elements.e*cosNu);
        double rootUP = MathUtility.Sqrt(central.Mu / elements.p);

        Vector3D rPQW = new Vector3D(cosNu,sinNu,0)* rMag;
        Vector3D vPQW = new Vector3D(-sinNu, elements.e+cosNu, 0) * rootUP;

        //회전 행렬
        Matrix3x3D R = Matrix3x3D.R3(elements.raan)*Matrix3x3D.R1(elements.i)*Matrix3x3D.R3(elements.argp);

        //결과 반환
        StateVector s = new StateVector();
        s.Position = R * rPQW;
        s.Velocity = R * vPQW;
        return s;
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
