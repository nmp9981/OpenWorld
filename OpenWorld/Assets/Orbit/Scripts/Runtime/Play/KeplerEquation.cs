using System;

/// <summary>
/// 케플러 방정식
/// </summary>
public static class KeplerEquation
{
    public const int DefaultMaxIter = 50;
    public const double DefaultTol = 1e-12;

    /// <summary>수렴 시 true. 실패해도 E에는 마지막 근사값이 들어감.</summary>
    public static bool TrySolveElliptic(
        double meanAnomaly, double e, out double E, out int iterations,
        double tol = DefaultTol, int maxIter = DefaultMaxIter)
    {
        iterations = 0;
        E = 0.0;

        if (e < 0.0 || e >= 1.0)
            throw new ArgumentOutOfRangeException(nameof(e), "타원 궤도만 지원 (0 <= e < 1)");

        double m = MathUtility.WrapToPi(meanAnomaly);
        if (e == 0.0) { E = m; return true; }      // 원궤도

        double sign = m < 0.0 ? -1.0 : 1.0;
        m = MathUtility.Abs(m);                    // m ∈ [0, π]

        // 초기값: 저이심률은 고정점 1회, 고이심률은 볼록성 보장점(E=π)
        double x = (e < 0.8) ? m + e * MathUtility.Sin(m) : ConstUtility.PI;

        for (int i = 0; i < maxIter; i++)
        {
            iterations = i + 1;

            double f = x - e * MathUtility.Sin(x) - m;
            double fp = 1.0 - e * MathUtility.Cos(x);

            double dx = f / fp;
            if (dx > 1.0) dx = 1.0;              // 볼록성을 깨지 않는 범위로 제한
            if (dx < -1.0) dx = -1.0;

            x -= dx;

            if (MathUtility.Abs(dx) < tol) { E = sign * x; return true; }
        }

        E = sign * x;
        return false;
    }

    /// <summary>수렴 실패 시 예외를 던지는 버전.</summary>
    public static double SolveElliptic(
        double meanAnomaly, double e,
        double tol = DefaultTol, int maxIter = DefaultMaxIter)
    {
        if (!TrySolveElliptic(meanAnomaly, e, out double E, out int iters, tol, maxIter))
            throw new InvalidOperationException(
                $"케플러 방정식 미수렴: e={e}, M={meanAnomaly}, iters={iters}");
        return E;
    }
}
