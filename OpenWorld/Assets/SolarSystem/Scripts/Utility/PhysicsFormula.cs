public static class PhysicsFormula
{
    /// <summary>
    /// 만유인력 힘
    /// </summary>
    /// <returns></returns>
    public static double Force_UniversalGravitation(float r, double M, double m)
    {
        double up = M * m * ConstUtility.G;//10^-11을 추후 곱해야함
        double down = r * r;
        return up / down;
    }
}
