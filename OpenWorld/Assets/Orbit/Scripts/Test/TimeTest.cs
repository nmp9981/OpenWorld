using NUnit.Framework;
using System;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;

public class TimeTest : MonoBehaviour
{
    const string FilePath = @"D:\DownLoad\Project_Data\Orbit\finals2000A.all";

    const double TolUt1 = 1e-7;   // 초. 파일 값이 소수 7자리
    const double TolPm = 1e-11;  // 라디안. 각초 1e-6 ≈ 4.8e-12 rad
    int pass, fail;

    void Start()
    {
        EOPTest();
    }


    void EOPTest()
    {
        var eop = EopTable.Parse(File.ReadAllLines(FilePath));
        double as2r = ConstUtility.AngleSecondToRad;

        // 1. 자정이면 파일 값 그대로
        var a = eop.At(2017, 1, 1, 0, 0, 0.0);
        Check("1a UT1-UTC 2017-01-01 00:00", a.ut1MinusUtc, 0.5912821, TolUt1);
        Check("1a xp", a.xp, 0.080504 * as2r, TolPm);
        Check("1a yp", a.yp, 0.263145 * as2r, TolPm);

        var b = eop.At(2016, 12, 30, 0, 0, 0.0);
        Check("1b UT1-UTC 2016-12-30 00:00", b.ut1MinusUtc, -0.4069180, TolUt1);
        Check("1b xp", b.xp, 0.082883 * as2r, TolPm);
        Check("1b yp", b.yp, 0.263539 * as2r, TolPm);

        // 2. 윤초 전날 정오: UT1-TAI로 보간해야 함
        //    올바른 값 -0.4082390, UT1-UTC를 그대로 보간하면 +0.0917610 (0.5초 차이)
        var c = eop.At(2016, 12, 31, 12, 0, 0.0);
        Check("2 UT1-UTC 2016-12-31 12:00", c.ut1MinusUtc, -0.4082390, TolUt1);
        Check("2 xp", c.xp, 0.080952 * as2r, TolPm);
        Check("2 yp", c.yp, 0.2631195 * as2r, TolPm);
        if (Math.Abs(c.ut1MinusUtc - 0.0917610) < 1e-3)
            Debug.LogError("  → UT1-UTC를 직접 보간하고 있음 (윤초 처리 누락)");

        // 3. 윤초 순간: 위치는 다음 날 자정 근처, ΔAT는 입력 날짜(12-31) 값 36
        //    올바른 값 약 -0.4087179, ΔAT를 37로 쓰면 +0.5912821
        var d = eop.At(2016, 12, 31, 23, 59, 60.5);
        Check("3 UT1-UTC 2016-12-31 23:59:60.5", d.ut1MinusUtc, -0.4087179, TolUt1);
        if (Math.Abs(d.ut1MinusUtc - 0.5912821) < 1e-3)
            Debug.LogError("  → 윤초 순간에 다음 날 ΔAT를 쓰고 있음");

        // 4. 범위 밖: 파일 첫 줄(1973-01-02)보다 이전
        try
        {
            eop.At(1973, 1, 1, 0, 0, 0.0);
            Fail("4 범위 밖 1973-01-01", "예외가 나지 않음");
        }
        catch (ArgumentOutOfRangeException)
        {
            Pass("4 범위 밖 1973-01-01");
        }

        Debug.Log($"EOP 테스트: 통과 {pass}, 실패 {fail}");
    }
    void Check(string name, double actual, double expected, double tol)
    {
        double diff = Math.Abs(actual - expected);
        if (diff <= tol) Pass(name);
        else Fail(name, $"실제 {actual:R}, 기대 {expected:R}, 차이 {diff:E2}");
    }

    void Pass(string name) { pass++; Debug.Log($"[통과] {name}"); }
    void Fail(string name, string why) { fail++; Debug.LogError($"[실패] {name}: {why}"); }

    public void GCRS_ITRS_Test()
    {
        Vector3D rg = new Vector3D(6778137.0, 0, 0);
        Vector3D vg = new Vector3D(0, 4000.0, 6440.0);
        var res1 = CoordinateSystemUtility.GCRS_To_ITRS(189345600, rg, vg, 2.55060238e-7, 1.860359247e-6);

        Vector3D rl = new Vector3D(-1195854.58017826, -6671810.67625475, 3913.31685439506);
        Vector3D vl = new Vector3D(3451.1427048424, -614.804980499884, 6440.15771238911);
        var res2 = CoordinateSystemUtility.ITRS_To_GCRS(189345600, rl, vl, 2.55060238e-7, 1.860359247e-6);
    }

    public void ITRS_TIRS_Test()
    {
        var M = CoordinateSystemUtility.PrecessionNutationMatrix(189345600);
        var M2 = CoordinateSystemUtility.GCRS_To_CIRSMatrix(M.m20, M.m21, -1.2253712485230263e-08);
        var M3 = CoordinateSystemUtility.GCRS_To_TIRSMatrix(189345600);

        double xp = 2.55060238e-7, yp = 1.860359247e-6;
        var W = CoordinateSystemUtility.WMatrix(xp, yp, 189345600);
        var IRTS = CoordinateSystemUtility.GCRS_To_ITRSMatrix(189345600, xp, yp);
    }

    /// <summary>
    /// 세차 테스트
    /// </summary>
    void Fukushima_Williams_4Angle_Test()
    {
        var angle4 = CoordinateSystemUtility.Fukushima_Williams_4Angle(-122731208.64);
        Debug.Log(angle4.gamb);
        Debug.Log(angle4.phib);
        Debug.Log(angle4.psib);
        Debug.Log(angle4.epsa);

        Debug.Log(CoordinateSystemUtility.Nutation(189345600.0));
    }

    void ERATest()
    {
        Debug.Log(CoordinateSystemUtility.ERA(0.0));
        Debug.Log(CoordinateSystemUtility.ERA(54388.0 - 51544.5));
        Debug.Log(TimeUtility.UtcToTuDays(2000, 1, 1, 12, 0, 0));
    }
    void Add1Day_Test()
    {
        var rng = new System.Random(3);
        int fail = 0;
        double expected = ConstUtility.TWO_PI * 0.00273781191135448;   // ≈ 0.0172022 rad ≈ 0.9856°
        for (int i = 0; i < 10000; i++)
        {
            double tu = -10000.0 + rng.NextDouble() * 20000.0;         // 약 1972 ~ 2027
            double diff = CoordinateSystemUtility.ERA(tu + 1.0) - CoordinateSystemUtility.ERA(tu);
            if (diff < 0) diff += ConstUtility.TWO_PI;                  // 2π 경계를 넘은 경우
            if (Math.Abs(diff - expected) > 1e-10) fail++;
        }
        Debug.Log($"ERA 1일 증가: 10000개 중 실패 {fail}");
    }

    void UTCTest()
    {
        int count = 0, fail = 0;
        for (int y = 1972; y <= 2016; y++)
            foreach (var (m, d) in new[] { (6, 30), (12, 31) })
            {
                int nextDAT = m == 12 ? TimeUtility.DeltaAT(y + 1, 1, 1) : TimeUtility.DeltaAT(y, 7, 1);
                if (nextDAT - TimeUtility.DeltaAT(y, m, d) != 1) continue;
                count++;
                var r = TimeUtility.TTSecondsToUtc(TimeUtility.UtcToTTSeconds(y, m, d, 23, 59, 60.5));
                if (r != (y, m, d, 23, 59, 60.5)) fail++;
            }
        Debug.Log($"윤초 왕복: {count}개 중 실패 {fail}");

        var rng = new System.Random(1);
        fail = 0;
        for (int i = 0; i < 10000; i++)
        {
            int y = 1972 + rng.Next(55), m = 1 + rng.Next(12), d = 1 + rng.Next(28);
            int h = rng.Next(24), mi = rng.Next(60);
            double s = rng.Next(60000) / 1000.0;
            var r = TimeUtility.TTSecondsToUtc(TimeUtility.UtcToTTSeconds(y, m, d, h, mi, s));
            if ((r.Y, r.M, r.D, r.H, r.Min) != (y, m, d, h, mi) || Math.Abs(r.S - s) > 1e-6) fail++;
        }
        Debug.Log($"UTC 왕복: 10000개 중 실패 {fail}");

        rng = new System.Random(2);
        fail = 0;
        double tMin = -883655957.816, tMax = 843652869.184;
        for (int i = 0; i < 10000; i++)
        {
            double tt = tMin + rng.NextDouble() * (tMax - tMin);
            var u = TimeUtility.TTSecondsToUtc(tt);
            double back = TimeUtility.UtcToTTSeconds(u.Y, u.M, u.D, u.H, u.Min, u.S);
            if (Math.Abs(back - tt) > 1e-3) fail++;
        }
        Debug.Log($"TT 왕복: 10000개 중 실패 {fail}");
    }
}
