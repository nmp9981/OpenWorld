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
