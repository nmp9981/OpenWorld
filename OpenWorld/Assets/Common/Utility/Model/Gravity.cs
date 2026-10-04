public static class Gravity
{
    /// <summary>
    /// 2체(점질량) 중력 가속도 a = -μ r / |r|³
    /// </summary>
    /// <param name="r">중심천체 기준 위치</param>
    /// <param name="mu">중력 상수 GM. 위치와 같은 단위계로 넣을 것 (정준: 1, km: 398600.4418)</param>
    public static Vector3D TwoBody(in Vector3D r, double mu)
    {
        double d = r.Magnitude();
        return r * (-mu / (d * d * d));
    }
}
