/// <summary>진근점이각(ν) ↔ 이심근점이각(E) ↔ 평균근점이각(M) 변환. 타원 궤도 전용.</summary>
public static class Anomaly
{
    /// <summary>β = √(1-e²). 상쇄를 피하려 (1-e)(1+e)로 인수분해해 계산한다.</summary>
    private static double Beta(double e) => MathUtility.Sqrt((1.0 - e) * (1.0 + e));

    public static double EccentricToTrue(double E, double e)
    => 2.0 * MathUtility.ArkTan2(
           MathUtility.Sqrt(1.0 + e) * MathUtility.Sin(0.5 * E),
           MathUtility.Sqrt(1.0 - e) * MathUtility.Cos(0.5 * E));

    public static double TrueToEccentric(double nu, double e)
        => 2.0 * MathUtility.ArkTan2(
               MathUtility.Sqrt(1.0 - e) * MathUtility.Sin(0.5 * nu),
               MathUtility.Sqrt(1.0 + e) * MathUtility.Cos(0.5 * nu));

    public static double EccentricToMean(double E, double e)
        => E - e * MathUtility.Sin(E);

    public static double MeanToEccentric(double M, double e)
        => KeplerEquation.SolveElliptic(M, e);

    public static double TrueToMean(double nu, double e)
        => EccentricToMean(TrueToEccentric(nu, e), e);

    public static double MeanToTrue(double M, double e)
        => EccentricToTrue(MeanToEccentric(M, e), e);
}
