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
        original.Velocity = new Vector3D(0, -7.54605329010754185, 0);
        
        double[] es = { 1.1e-8, 0.9e-8 };

        CentralBody central = CentralBody.Earth;
        var el = ToElements(central, original);
        var back = ToState(central, el);

        double posErr = (back.Position - original.Position).Magnitude() / original.Position.Magnitude();
        double velErr = (back.Velocity - original.Velocity).Magnitude() / original.Velocity.Magnitude();

        // 4-a: raan = 0 인 경우 — argp 가 그대로 나와야 함
        RoundTripFromElements(central, "4-a", 7000.0, 0.1, 0.0, 0.0, 1.2, 0.4);

        // 4-b: raan ≠ 0 인 경우 — raan+argp 가 argp 자리로 합쳐져야 함
        RoundTripFromElements(central, "4-b", 7000.0, 0.1, 0.0, 0.3, 0.9, 0.4);
        //                                              ϖ = 0.3 + 0.9 = 1.2

        // 4-c: 합이 2π 를 넘는 경우
        RoundTripFromElements(central, "4-c", 7000.0, 0.1, 0.0, 2.5, 2.8, 0.4);
        //

        Debug.Log("=== e 경계 ===");
        CompareBoundary(central, "e", 1.1e-8, 0.9e-8, isEcc: true);

        Debug.Log("=== i 경계 ===");
        CompareBoundary(central, "i", 1.1e-8, 0.9e-8, isEcc: false);
    }
    void RoundTripFromElements(CentralBody central, string name,
                           double p, double e, double i,
                           double raan, double argp, double nu)
    {
        OrbitalElements el0 = new OrbitalElements();
        el0.p = p; el0.e = e; el0.i = i;
        el0.raan = raan; el0.argp = argp; el0.nu = nu;

        StateVector s = ToState(central, el0);
        OrbitalElements el1 = ToElements(central, s);
        StateVector back = ToState(central, el1);

        double dPos = (back.Position - s.Position).Magnitude() / s.Position.Magnitude();
        double dVel = (back.Velocity - s.Velocity).Magnitude() / s.Velocity.Magnitude();

        Debug.Log($"[{name}] {el1.Geometry}  e={el1.e:F6} i={el1.i:E3} " +
                  $"raan={el1.raan:F6} argp={el1.argp:F6} nu={el1.nu:F6}  " +
                  $"dPos={dPos:E3} dVel={dVel:E3}");
    }

    void CompareBoundary(CentralBody central, string tag,
                     double above, double below, bool isEcc)
    {
        StateVector s1 = MakeBoundaryState(central, above, isEcc);
        StateVector s2 = MakeBoundaryState(central, below, isEcc);

        OrbitalElements e1 = ToElements(central, s1);
        OrbitalElements e2 = ToElements(central, s2);

        StateVector b1 = ToState(central, e1);
        StateVector b2 = ToState(central, e2);

        double dPos1 = (b1.Position - s1.Position).Magnitude() / s1.Position.Magnitude();
        double dPos2 = (b2.Position - s2.Position).Magnitude() / s2.Position.Magnitude();

        // 두 궤도 자체의 차이 — 분기가 달라도 위치는 거의 같아야 함
        double gap = (b1.Position - b2.Position).Magnitude() / b1.Position.Magnitude();

        Debug.Log($"[{tag} 위] {e1.Geometry}  dPos={dPos1:E3}");
        Debug.Log($"[{tag} 아래] {e2.Geometry}  dPos={dPos2:E3}");
        Debug.Log($"[{tag} 점프] gap={gap:E3}");
    }

    StateVector MakeBoundaryState(CentralBody central, double val, bool isEcc)
    {
        OrbitalElements el = new OrbitalElements();
        el.p = 7000.0;
        el.e = isEcc ? val : 0.1;
        el.i = isEcc ? 0.5 : val;
        el.raan = 0.8; el.argp = 1.2; el.nu = 0.4;
        return ToState(central, el);
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
        double nMag = n.Magnitude();
        double eMag = e.Magnitude();
        double sinI = nMag / hMag;

        bool circular = eMag < E_TOL;// 이심률이 0에 가까운가?
        bool equatorial = sinI < I_TOL;// 승교점 직경이 0에 가까운가?

        OrbitalElements orbitalElements = new OrbitalElements();

        orbitalElements.p = hMag * hMag / central.Mu;
        orbitalElements.e = e.Magnitude();
        orbitalElements.i = MathUtility.ArkTan2(MathUtility.Sqrt(h.x * h.x + h.y * h.y), h.z);

        Vector3D nHat = n / n.Magnitude();
        Vector3D mHat = Vector3D.Cross(hHat, nHat);

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
