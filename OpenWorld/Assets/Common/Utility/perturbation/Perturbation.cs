public static class Perturbation
{
    // a_J2 = -(3/2) J2 μ R² / r^5 · [ (1 - 5 (r·k)²/r²) r + 2 (r·k) k ]
    /// <summary>
    /// J2 항에 의한 섭동 가속도
    /// </summary>
    /// <param name="r">위치 벡터</param>
    /// <param name="k">극축</param>
    /// <param name="mu">천체 중력 상수</param>
    /// <param name="re">천체 반지름</param>
    /// <param name="j2">각 천체별 J2상수</param>
    /// <returns></returns>
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
    /// <param name="r">위치 벡터</param>
    /// <param name="v">속도 벡터</param>
    /// <param name="k">극축</param>
    /// <param name="mu">천체 중력 상수</param>
    /// <param name="re">천체 반지름</param>
    /// <param name="j2">각 천체별 J2상수</param>
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
    /// <param name="r">위치 벡터</param>
    /// <param name="v">속도 벡터</param>
    /// <param name="k">극축</param>
    /// <returns></returns>
    public static double AxialAngularMomentum(in Vector3D r, in Vector3D v, in Vector3D k)
        => Vector3D.Dot(Vector3D.Cross(r, v), k);


    /// <summary>
    /// 제3천체에 의한 섭동 가속도 (지구 중심 좌표계, 직접항 − 간접항)
    /// </summary>
    /// <param name="r">위성 위치, 지구 중심 [km]</param>
    /// <param name="s">제3천체 위치, 지구 중심 [km]</param>
    /// <param name="mu3">제3천체 중력상수 [km³/s²]</param>
    public static Vector3D ThirdBodyAccel(in Vector3D r, in Vector3D s, double mu3)
    {
        Vector3D rRel = s-r;
        double rRel2 = Vector3D.Dot(rRel, rRel);
        double rRelMag = MathUtility.Sqrt(rRel2);
        double s2 = Vector3D.Dot(s, s);
        double sMag = MathUtility.Sqrt(s2);
        
        double sr_accel = mu3 / (rRel2 * rRelMag);
        double s_accel = mu3 / (s2 * sMag);
        return sr_accel*rRel - s_accel * s;
    }

    /// <summary>
    /// Battin 정식화
    /// </summary>
    /// <returns></returns>
    public static Vector3D BattinAccel(Vector3D r, Vector3D s, double mu)
    {
        double r_s = (r-s).Magnitude();
        double s_r = (s-r).Magnitude();
        double rSize = r.Magnitude();
        double sSize = s.Magnitude();

        double fq = (MathUtility.Pow(r_s,3)/MathUtility.Pow(sSize,3))-1;

        Vector3D r_Fqs = r+fq * s;
        return -mu*r_Fqs/(MathUtility.Pow(s_r,3));
    }
}