using System.Collections.Generic;
using System.Globalization;
using System;

/// <summary>
/// IERS finals2000A EOP 테이블 (UT1-UTC, 극운동 xp, yp)
/// - 하루 단위 값을 선형 보간
/// - UT1-UTC는 윤초에 1초 점프하므로 UT1-TAI(연속)로 바꿔 보간한 뒤 되돌림
/// - 파일 범위 밖이면 ArgumentOutOfRangeException
/// </summary>
public sealed class EopTable
{
    // J2000(JD 2451545.0)의 MJD
    const double MjdJ2000 = 51544.5;

    readonly double[] mjd;      // 각 줄의 MJD (UTC 자정)
    readonly double[] ut1Tai;   // UT1 - TAI [s] (연속값)
    readonly double[] xpAs;     // xp [각초]
    readonly double[] ypAs;     // yp [각초]
    readonly bool[] predicted;  // 예측값(P) 여부

    EopTable(List<double> m, List<double> u, List<double> x, List<double> y, List<bool> p)
    {
        mjd = m.ToArray(); ut1Tai = u.ToArray(); xpAs = x.ToArray(); ypAs = y.ToArray(); predicted = p.ToArray();
    }

    /// <summary>finals2000A 파일의 줄 목록을 파싱</summary>
    public static EopTable Parse(IEnumerable<string> lines)
    {
        var m = new List<double>(); var u = new List<double>();
        var x = new List<double>(); var y = new List<double>(); var p = new List<bool>();
        var inv = CultureInfo.InvariantCulture;

        foreach (var line in lines)
        {
            // UT1-UTC 값(59~68열)까지 있어야 유효한 줄
            if (line == null || line.Length < 68) continue;
            char pmFlag = line[16];
            char utFlag = line[57];
            if (pmFlag == ' ' || utFlag == ' ') continue;   // 값이 없는 줄(예측 구간 이후)

            double mjdRow = double.Parse(line.Substring(7, 8), inv);
            double xp = double.Parse(line.Substring(18, 9), inv);
            double yp = double.Parse(line.Substring(37, 9), inv);
            double dut1 = double.Parse(line.Substring(58, 10), inv);

            // 그 줄 날짜의 ΔAT로 UT1-TAI 계산
            var c = TimeUtility.JulianDateToCalendar(mjdRow + 2400000.5);
            double ut1MinusTai = dut1 - TimeUtility.DeltaAT(c.Y, c.M, c.D);

            m.Add(mjdRow); u.Add(ut1MinusTai); x.Add(xp); y.Add(yp);
            p.Add(pmFlag == 'P' || utFlag == 'P');
        }

        if (m.Count < 2) throw new FormatException("EOP 데이터가 2줄 미만입니다.");
        return new EopTable(m, u, x, y, p);
    }

    /// <summary>
    /// UTC 달력 시각의 EOP 값.
    /// 반환: UT1-UTC [s], xp [rad], yp [rad], 예측값 사용 여부
    /// </summary>
    public (double ut1MinusUtc, double xp, double yp, bool predicted) At(int Y, int M, int D, int H, int Min, double S)
    {
        // UTC MJD (하루 86400초 눈금, 23:59:60.x는 다음 날 자정 직후 위치)
        double t = TimeUtility.SecondsSinceJ2000(Y, M, D, H, Min, S) / 86400.0 + MjdJ2000;

        if (t < mjd[0] || t > mjd[mjd.Length - 1])
            throw new ArgumentOutOfRangeException(nameof(Y), "EOP 테이블 범위 밖의 시각입니다.");

        // 하루 간격이므로 인덱스를 바로 계산 (마지막 줄과 정확히 같으면 직전 구간 사용)
        int i = (int)Math.Floor(t - mjd[0]);
        if (i >= mjd.Length - 1) i = mjd.Length - 2;
        double w = (t - mjd[i]) / (mjd[i + 1] - mjd[i]);

        double ut1Tai_ = ut1Tai[i] + w * (ut1Tai[i + 1] - ut1Tai[i]);
        double xp = xpAs[i] + w * (xpAs[i + 1] - xpAs[i]);
        double yp = ypAs[i] + w * (ypAs[i + 1] - ypAs[i]);

        // 입력 날짜의 ΔAT로 UT1-UTC 복원 (윤초 순간 23:59:60도 그날 값 사용)
        double ut1Utc = ut1Tai_ + TimeUtility.DeltaAT(Y, M, D);

        double as2r = ConstUtility.AngleSecondToRad;
        return (ut1Utc, xp * as2r, yp * as2r, predicted[i] || predicted[i + 1]);
    }
}
