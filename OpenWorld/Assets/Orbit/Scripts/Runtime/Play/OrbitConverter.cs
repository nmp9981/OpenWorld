public static class OrbitConverter
{
    // 기준평면(적도면)의 법선 = 자전축 방향
    static readonly Vector3D ReferenceNormal = new Vector3D(0, 0, 1);
    const double E_TOL = 1e-8;   // 측정 근거: Δω ≈ 1e-16/e, 1e-6 rad 기준
    const double I_TOL = 1e-8;   // 판단 근거: 실 위성 i가 1e-3 수준

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
        orbitalElements.e = eMag;
        orbitalElements.i = MathUtility.ArkTan2(nMag, h.z);

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
            Vector3D nHat = n / nMag;
            Vector3D mHat = Vector3D.Cross(hHat, nHat);

            orbitalElements.raan = MathUtility.ArkTan2(n.y, n.x);
            orbitalElements.argp = 0.0;
            orbitalElements.nu = MathUtility.ArkTan2(Vector3D.Dot(r, mHat), Vector3D.Dot(r, nHat));
            orbitalElements.Geometry = OrbitGeometry.CircularInclined;
        }
        else if (equatorial)
        {
            orbitalElements.raan = 0.0;
            orbitalElements.argp = MathUtility.ArkTan2(e.y, e.x);
            if (h.z < 0) orbitalElements.argp = -orbitalElements.argp;
            orbitalElements.nu = MathUtility.ArkTan2(hMag * rv / central.Mu, Vector3D.Dot(e, r));
            orbitalElements.Geometry = OrbitGeometry.EllipticalEquatorial;
        }
        else
        {
            Vector3D nHat = n / nMag;
            Vector3D mHat = Vector3D.Cross(hHat, nHat);

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
        double rMag = elements.p / (1 + elements.e * cosNu);
        double rootUP = MathUtility.Sqrt(central.Mu / elements.p);

        Vector3D rPQW = new Vector3D(cosNu, sinNu, 0) * rMag;
        Vector3D vPQW = new Vector3D(-sinNu, elements.e + cosNu, 0) * rootUP;

        //회전 행렬
        Matrix3x3D R = Matrix3x3D.R3(elements.raan) * Matrix3x3D.R1(elements.i) * Matrix3x3D.R3(elements.argp);

        //결과 반환
        StateVector s = new StateVector(R * rPQW, R * vPQW);
        return s;
    }
}
