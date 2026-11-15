using static IntegratorUtility;

public sealed class ForceModel
{
    public double Mu = 398600.4418;   // km^3/s^2 (EGM2008)
    public double Re = 6378.1363;     // km      (EGM2008, J2와 짝)
    public double J2 = 1.08262668e-3;
    public Vector3D PoleAxis = new Vector3D(0, 0, 1); // 단위벡터, 검증 단계에선 고정

    public bool useJ2 = true;
    public bool useMoon = false;//달 사용 여부
    public bool useSun = false;//태양 사용 여부

    public double MuMoon = 4902.800;                       // km^3/s^2
    public Vector3D MoonPosition = new Vector3D(384400, 0, 0); // 지심 [km], 구현 J 전까지 고정

    public double EpochJD;   // 적분 t = 0에 해당하는 율리우스일
    public double B = 0.022;
    Vector3D K = new Vector3D(0, 0, 1);

    public Vector3D Accel(double t, Vector3D r)
    {
        Vector3D a = Gravity.TwoBody(r, Mu);

        if (useJ2)
            a += Perturbation.J2Accel(r, PoleAxis, Mu, Re, J2);

        if (useMoon)
        {
            Vector3D s = MoonPosition;   // 구현 J에서 GetMoonPosition(t)로 교체
            a += Perturbation.ThirdBodyAccel(r, s, MuMoon);
        }

        return a;
    }

    /// <summary>
    /// 2체(점질량) 중력 가속도 a = -μ r / r³
    /// </summary>
    public static Vector3D TwoBody(Vector3D r, double mu)
    {
        double r2 = Vector3D.Dot(r, r);
        double rMag = MathUtility.Sqrt(r2);
        return (-mu / (r2 * rMag)) * r;
    }

    public Vector3D Accel(double t, Vector3D r,Vector3D v)
    {
        // 1. 지금 시각의 JD
        double jd = EpochJD + t / 86400.0;

        // 2. 그 시각의 태양·달 위치
        Vector3D sunPos = CoorninateObjectUtility.Sun_In_JD(jd);
        Vector3D moonPos = CoorninateObjectUtility.Moon_In_JD(jd);

        // 3. 각 가속도 (Vector3D)
        Vector3D twoBody = Gravity.TwoBody(r, Mu);
        Vector3D drag = Perturbation.DragAccel(r, v, B);
        Vector3D j2 = Perturbation.J2Accel(r, K, Mu, ConstUtility.EarthRadius, ConstUtility.EarthJ2);
        Vector3D moonA = Perturbation.ThirdBodyAccel(r, moonPos, ConstUtility.muMoon);
        Vector3D sunA = Perturbation.ThirdBodyAccel(r, sunPos, ConstUtility.muSun);

        // 4. 벡터로 더해서 반환
        return twoBody + drag + j2 + moonA + sunA;
    }
}
