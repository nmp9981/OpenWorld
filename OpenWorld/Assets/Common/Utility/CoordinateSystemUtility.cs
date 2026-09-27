using UnityEngine;

public class CoordinateSystemUtility
{

    /// <summary>
    /// ERA 계산, 지구의 실제 회전 각도
    /// </summary>
    /// <param name="ut1"></param>
    /// <returns></returns>
    public static double ERA(double tuDays)
    {
        double f = tuDays - MathUtility.Floor(tuDays);                 // Tu 소수부
        double rev = f + ConstUtility.ERA_UT1 + (ConstUtility.ERA_UT1_RATE - 1) * tuDays;
        rev -= MathUtility.Floor(rev);
        return ConstUtility.TWO_PI * rev;
    }
}
