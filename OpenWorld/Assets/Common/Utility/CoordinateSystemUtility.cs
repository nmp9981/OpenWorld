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
    /// <summary>
    /// 세차 각도 구하기
    /// </summary>
    /// <returns></returns>
    public static (double gamb, double phib, double psib, double epsa) Fukushima_Williams_4Angle(double ttSeconds)
    {
        //율리우스 시간
        double T = ttSeconds / (86400.0 * 36525.0);

        //세차 각도 계산
        double gamb = -0.052928 + (10.556378 + (0.4932044 + (-0.00031238 + (-0.000002788 + 0.0000000260*T)*T)*T)*T)*T;
        double phib = 84381.412819 + (-46.811016 + (0.0511268 + (0.00053289 + (-0.000000440 + -0.0000000176 * T) * T) * T) * T) * T;
        double psib = -0.041775 + (5038.481484 + (1.5584175 + (-0.00018522 + (-0.000026452 + -0.0000000148 * T) * T) * T) * T) * T;
        double epsa = 84381.406 + (-46.836769 + (-0.0001831 + (0.00200340 + (-0.000000576 + -0.0000000434 * T) * T) * T) * T) * T;

        //라디안 변환
        gamb *= ConstUtility.AngleSecondToRad;
        phib *= ConstUtility.AngleSecondToRad;
        psib *= ConstUtility.AngleSecondToRad;
        epsa *= ConstUtility.AngleSecondToRad;

        return (gamb, phib, psib, epsa);
    }
}
