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
        bool gregorian = Y > 1582 || (Y == 1582 && (M > 10 || (M == 10 && D >= 15)));
        if (M <= 2)
        {
            Y -= 1;
            M += 12;
        }
        
        double A = MathUtility.Floor(Y / 100);
        double B = gregorian ? 2 - A + MathUtility.Floor(A / 4) : 0;
        double jd = MathUtility.Floor(365.25 * (Y + 4716)) + MathUtility.Floor(30.6001 * (M + 1)) + D + B - 1524.5;
        return jd;
    }
  
    /// <summary>
    /// 초 변환, J2000 기준
    /// </summary>
    /// <param name="Y"></param>
    /// <param name="M"></param>
    /// <param name="D"></param>
    /// <param name="H"></param>
    /// <param name="Min"></param>
    /// <param name="S"></param>
    /// <returns></returns>
    public static double SecondsSinceJ2000(int Y, int M, int D, int H, int Min, double S)
    {
        double jd0 = JulianDate(Y, M, D);   // 항상 xxx.5, 정확히 표현됨
        return (jd0 - 2451545.0) * 86400.0 + H * 3600.0 + Min * 60.0 + S;
    }

    /// <summary>
    /// Julian Date → 달력 날짜/시각 (Meeus, Astronomical Algorithms 7장).
    /// 1582-10-15 이전은 율리우스력, 이후는 그레고리력으로 반환한다.
    /// 유효 범위: jd >= 0.
    /// </summary>
    /// <param name="jd">Julian Date</param>
    /// <param name="secondDecimals">초를 반올림할 소수 자릿수 (기본 3 = ms)</param>
    public static (int Y, int M, int D, int H, int Min, double S) JulianDateToCalendar(double jd, int secondDecimals = 3)
    {
        // 1단계: 정오 기준 → 자정 기준, 정수부/소수부 분리
        double jd5 = jd + 0.5;
        double Z = MathUtility.Floor(jd5);

        // 초를 먼저 반올림하고, 하루를 넘으면 날짜로 올림 (24:00:00 방지)
        double secOfDay = MathUtility.Round((jd5 - Z) * 86400.0, secondDecimals);
        if (secOfDay >= 86400.0)
        {
            Z += 1.0;
            secOfDay -= 86400.0;
        }

        // 2단계: 그레고리력 일수 → 율리우스력 일수 (생략된 윤일 복원)
        double A;
        if (Z < 2299161.0)
        {
            A = Z;
        }
        else
        {
            double alpha = MathUtility.Floor((Z - 1867216.25) / 36524.25);
            A = Z + 1.0 + alpha - MathUtility.Floor(alpha / 4.0);
        }

        // 3단계: 3월 시작 연도 기준으로 연·월 찾기
        double B = A + 1524.0;
        double C = MathUtility.Floor((B - 122.1) / 365.25);
        double Dp = MathUtility.Floor(365.25 * C);
        double E = MathUtility.Floor((B - Dp) / 30.6001);

        // 4단계: 3월 시작 달력 → 일반 달력
        int day = (int)(B - Dp - MathUtility.Floor(30.6001 * E));
        int month = E < 14.0 ? (int)E - 1 : (int)E - 13;
        int year = month > 2 ? (int)C - 4716 : (int)C - 4715;

        // 시각 (secOfDay >= 0 이므로 int 캐스팅 = 내림)
        int hour = (int)(secOfDay / 3600.0);
        int minute = (int)((secOfDay - hour * 3600.0) / 60.0);
        double second = secOfDay - hour * 3600.0 - minute * 60.0;

        return (year, month, day, hour, minute, second);
    }
}
