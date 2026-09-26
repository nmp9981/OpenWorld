using System;
using System.Globalization;
using UnityEngine.Assertions.Must;

public static class TimeUtility
{
    public const double TT_TAI = 32.184;   // TT - TAI (초)

    //윤초 배열
    private static readonly (int Year, int Month, int DeltaAT)[] LeapSecondTable =
{
    (1972, 1, 10),
    (1972, 7, 11),
    (1973, 1, 12),
    (1974,1,13),
    (1975,1,14),
    (1976,1,15),
    (1977,1,16),
    (1978,1,17),
    (1979,1,18),
    (1980,1,19),
    (1981,7,20),
    (1982,7,21),
    (1983,7,22),
    (1985,7,23),
    (1988,1,24),
    (1990,1,25),
    (1991,1,26),
    (1992,7,27),
    (1993,7,28),
    (1994,7,29),
    (1996,1,30),
    (1997,7,31),
    (1999,1,32),
    (2006,1,33),
    (2009,1,34),
    (2012,7,35),
    (2015,7,36),
    (2017, 1, 37),
};

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

    /// <summary>
    /// delta AT 조회
    /// </summary>
    /// <param name="Y"></param>
    /// <param name="M"></param>
    /// <param name="D"></param>
    /// <returns></returns>
    public static int DeltaAT(int Y, int M, int D)
    {
        int deltaAT = 0;
        int key = Y * 12 + (M - 1);
        for (int i = LeapSecondTable.Length - 1; i >= 0; i--)
        {
            var (year, month, delta) = LeapSecondTable[i];
            int tableKey = year * 12 + (month - 1);
            if (key >= tableKey)
            {
                deltaAT = delta;
                return deltaAT;
            }
        }
        throw new ArgumentOutOfRangeException(nameof(Y), "UTC는 1972-01-01 이후만 지원합니다.");
    }
    /// <summary>
    /// UTC -> TT 초
    /// </summary>
    /// <param name="Y"></param>
    /// <param name="M"></param>
    /// <param name="D"></param>
    /// <param name="H"></param>
    /// <param name="Min"></param>
    /// <param name="S"></param>
    /// <returns></returns>
    public static double UtcToTTSeconds(int Y, int M, int D, int H, int Min, double S)
    {
        //윤초 삽입 조건
        if(DeltaAT(Y, M, D) - DeltaAT(Y, M, D - 1)==1 && H==23 && Min==59)
        {
            S = 60.0;
        }

        double tt = SecondsSinceJ2000(Y, M, D, H, Min, S) + DeltaAT(Y, M, D) + TT_TAI;
        return tt;
    }
    /// <summary>
    /// TT 초 -> UTC
    /// </summary>
    /// <param name="tt"></param>
    /// <returns></returns>
    public static (int Y, int M, int D, int H, int Min, double S) TTSecondsToUtc(double tt)
    {
        //TT -> TAI
        double tai = tt - TT_TAI;

        //예외 처리
        double taiMin = SecondsSinceJ2000(1972, 1, 1, 0, 0, 0) + DeltaAT(1972, 1, 1);
        if (tai < taiMin)
            throw new ArgumentOutOfRangeException(nameof(tt), "UTC는 1972-01-01 이후만 지원합니다.");

        //날짜 계산
        var date = JulianDateToCalendar((tai-10)/86400.0+ 2451545.0);

        var d = (date.Y, date.M, date.D);
        double sod = 0;
        double len = 0;
        while (true)
        {
            var next = AddDays(d.Y, d.M, d.D, 1);//다음 날

            //후보 날짜의 TAI 구간 계산
            double start = SecondsSinceJ2000(d.Y, d.M, d.D, 0, 0, 0) + DeltaAT(d.Y, d.M, d.D);
            len = 86400.0 + DeltaAT(next.Y, next.M, next.D) - DeltaAT(d.Y, d.M, d.D);
            sod = tai - start;
            if (sod < 0) d = AddDays(d.Y, d.M, d.D, -1);
            else if (sod >= len) d = next;
            else break;
        }

        //시각 분해
        double secOfDay = MathUtility.Round(sod,3);
        if(secOfDay >= len)
        {
            var n = AddDays(d.Y, d.M, d.D, 1);
            return (n.Y, n.M, n.D, 0, 0, 0.0);
        }

        if (secOfDay >= 86400.0)//윤초
        {
            return (d.Y, d.M, d.D, 23, 59, 60.0+ (secOfDay - 86400.0));
        }

        //시분초 분해
        int h = (int)(secOfDay / 3600.0);
        int m = (int)((secOfDay - h * 3600.0) / 60.0);
        double s = secOfDay - h * 3600.0 - m * 60.0;
        return (d.Y, d.M, d.D, h, m, s);
    }
    /// <summary>
    /// 날짜 가감
    /// </summary>
    /// <param name="Y"></param>
    /// <param name="M"></param>
    /// <param name="D"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    private static (int Y, int M, int D) AddDays(int Y, int M, int D, int n)
    {
        var c = JulianDateToCalendar(JulianDate(Y, M, D) + n);
        return (c.Y, c.M, c.D);
    }
}
