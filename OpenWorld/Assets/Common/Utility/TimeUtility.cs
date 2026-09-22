using UnityEngine;

public static class TimeUtility
{
   /// <summary>
   /// 시간->실수
   /// </summary>
   /// <param name="time"></param>
   /// <returns></returns>
    public static double JulianDate(double Y, double M, double D)
    {
        if (M <= 2)
        {
            Y -= 1;
            M += 12;
        }
        double A = MathUtility.Floor(Y / 100);
        double B = 2 - A + MathUtility.Floor(A / 4);
        double jd = MathUtility.Floor(365.25 * (Y + 4716)) + MathUtility.Floor(30.6001 * (M + 1)) + D + B - 1524.5;
        return jd;
    }
    /// <summary>
    /// 시간->실수
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static double JulianDate(double Y, double M, double D, double H, double Min, double S)
    {
        double jd = JulianDate(Y, M, D);
        double dayFraction = (H + (Min / 60.0) + (S / 3600.0)) / 24.0;
        return jd + dayFraction;
    }
    /// <summary>
    /// 실수->시간
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static (double Y, double M, double D) Meeus(double time)
    {
        return (0,0,0);
    }
}
