public static class Perturbation
{
    // a_J2 = -(3/2) J2 μ R² / r^5 · [ (1 - 5 (r·k)²/r²) r + 2 (r·k) k ]
    public static Vector3D J2Accel(in Vector3D r, in Vector3D k, double mu, double re, double j2)
    {
        double r2 = Vector3D.Dot(r, r);
        double rMag = MathUtility.Sqrt(r2);
        double rk = Vector3D.Dot(r, k);
        double coef = -1.5 * j2 * mu * re * re / (r2 * r2 * rMag);
        return coef * ((1.0 - 5.0 * rk * rk / r2) * r + (2.0 * rk) * k);
    }

    /// <summary>
    /// J2 항을 포함한 수정된 에너지
    /// kHat가 시간에 대해 고정일 때만 보존
    /// </summary>
    /// <param name="r"></param>
    /// <param name="v"></param>
    /// <param name="k"></param>
    /// <param name="mu"></param>
    /// <param name="re"></param>
    /// <param name="j2"></param>
    /// <returns></returns>
    public static double ModifiedEnergy(in Vector3D r, in Vector3D v, in Vector3D k,
                                        double mu, double re, double j2)
    {
        double r2 = Vector3D.Dot(r, r), rMag = MathUtility.Sqrt(r2);
        double rk = Vector3D.Dot(r, k);
        return 0.5 * Vector3D.Dot(v, v) - mu / rMag
             + 0.5 * mu * j2 * re * re / (r2 * rMag) * (3.0 * rk * rk / r2 - 1.0);
    }

    /// <summary>
    /// 극축(k) 방향 각운동량 성분 h·k.
    /// J2 퍼텐셜은 k축 대칭이므로 k가 고정일 때 보존된다. (전체 h 벡터는 세차하므로 비보존)
    /// </summary>
    /// <param name="r"></param>
    /// <param name="v"></param>
    /// <param name="k"></param>
    /// <returns></returns>
    public static double AxialAngularMomentum(in Vector3D r, in Vector3D v, in Vector3D k)
        => Vector3D.Dot(Vector3D.Cross(r, v), k);
}