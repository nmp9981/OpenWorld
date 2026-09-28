using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.Rendering;

public class TimeTest : MonoBehaviour
{
    void Start()
    {
        Fukushima_Williams_4Angle_Test();     
    }

    /// <summary>
    /// 세차 테스트
    /// </summary>
    void Fukushima_Williams_4Angle_Test()
    {

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
