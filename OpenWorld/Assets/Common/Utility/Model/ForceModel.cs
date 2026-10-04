using Unity.Android.Gradle.Manifest;
using static IntegratorUtility;

public sealed class ForceModel
{
    public double Mu = 398600.4418;   // km^3/s^2 (EGM2008)
    public double Re = 6378.1363;     // km      (EGM2008, J2와 짝)
    public double J2 = 1.08262668e-3;
    public Vector3D PoleAxis = new Vector3D(0, 0, 1); // 단위벡터, 검증 단계에선 고정

    public bool UseJ2 = true;

    public AccelFunc Build()
    {
        // 클로저가 필드를 직접 참조하지 않도록 지역 변수로 복사.
        // Build() 이후 필드를 바꿔도 이미 만든 델리게이트에는 영향이 없게 하기 위함.
        double mu = Mu, re = Re, j2 = J2;
        Vector3D k = PoleAxis;

        return r => Gravity.TwoBody(r,mu) + Perturbation.J2Accel(r, k, mu, re, j2);
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
}
