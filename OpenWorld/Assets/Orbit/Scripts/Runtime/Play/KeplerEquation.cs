using System;

/// <summary>
/// 케플러 방정식
/// </summary>
public static class KeplerEquation
{
    public static double SolveKeplerElliptic(
    double meanAnomaly, double e, double tol = 1e-12, int maxIter = 50)
    {
        if (e < 0.0 || e >= 1.0)
            throw new ArgumentOutOfRangeException(nameof(e), "타원 궤도만 지원 (0 <= e < 1)");

        double m = WrapToPi(meanAnomaly);
        if (e == 0.0) return m;                 // 원궤도: E = M

        double sign = m < 0.0 ? -1.0 : 1.0;
        m = MathUtility.Abs(m);                        // m ∈ [0, π]

        // 초기값: 저이심률은 고정점 1회, 고이심률은 볼록성 보장점
        double E = (e < 0.8) ? m + e * MathUtility.Sin(m) : ConstUtility.PI;

        for (int i = 0; i < maxIter; i++)
        {
            double f = E - e * MathUtility.Sin(E) - m;
            double fp = 1.0 - e * MathUtility.Cos(E);

            double dE = f / fp;
            // 안전장치: 한 스텝이 반주기를 넘지 못하게
            if (dE > 1.0) dE = 1.0;
            if (dE < -1.0) dE = -1.0;

            E -= dE;
            if (MathUtility.Abs(dE) < tol) return sign * E;
        }

        throw new InvalidOperationException(
            $"케플러 방정식 미수렴: e={e}, M={meanAnomaly}");
    }

    private static double WrapToPi(double x)
    {
        // Math.IEEERemainder(x, 2π)가 정확히 [-π, π]를 줍니다
        return Math.IEEERemainder(x, 2.0 * Math.PI);
    }
}
