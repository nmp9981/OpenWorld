using System.Collections.Generic;
using UnityEngine;
using static IntegratorUtility;
using MathD.Integration;
using SM = System.Math;

/// <summary>
/// 3번 그룹: J2 영년 변화율 (Ω̇, ω̇) — 수치 적분 vs 1차 해석식
///
/// 방법
///  - 승교점 통과(z: 음 → 양) 시점마다 Ω, ω를 한 번씩 표본 추출
///    (단주기 항은 위도 인수 u의 함수 → 같은 u=0에서 찍으면 스트로보스코프처럼 상쇄)
///  - 표본을 unwrap 후 선형 최소제곱으로 기울기 = 영년 변화율
///  - 해석식에는 "전 구간 시간평균한 접촉요소(≈ 평균요소)"를 넣음
///    비교용으로 "초기 접촉요소"를 넣은 결과도 같이 출력
///
/// 판정은 두 층
///  ① 적분기 층: dt를 절반으로 줄여도 추출한 변화율이 변하지 않는가
///  ② 이론 층  : 수렴한 변화율이 해석식과 O(J2) 상대오차 안에서 일치하는가
///
/// 단위: 정준 단위 (DU = R_E, μ = 1). 결과는 °/일로도 출력
/// </summary>
public class J2SecularRateTest : MonoBehaviour
{
    const double Mu = 1.0;
    const double Re = 1.0;
    const double J2 = 1.08262668e-3;
    const double Deg = SM.PI / 180.0;

    // 정준 → 물리 단위 변환 (EGM2008)
    const double ReKm = 6378.1363;
    const double MuKm = 398600.4418;
    static readonly double TU = SM.Sqrt(ReKm * ReKm * ReKm / MuKm);   // ≈ 806.81 s

    static readonly Vector3D K = new Vector3D(0, 0, 1);

    // 판정 허용치
    const double TolConvergence = 1e-6;   // ① dt 수렴
    const double TolTheoryRaan = 3e-3;    // ② Ω̇ vs 이론 (상대)
    const double TolTheoryArgp = 5e-3;    // ② ω̇ vs 이론 (상대)

    [Tooltip("4번 그룹(태양동기궤도 1년 적분) 실행 여부. 오래 걸리므로 평소엔 끔")]
    public bool RunSSO = true;

    AccelFunc accel;
    int passCount, failCount;

    struct RateResult
    {
        public double RaanDot, ArgpDot;     // rad/TU
        public double MeanA, MeanE, MeanI;  // 시간평균 접촉요소
        public int Nodes;
    }

    void Start()
    {
        passCount = failCount = 0;
        accel = r => Gravity.TwoBody(r, Mu) + Perturbation.J2Accel(r, K, Mu, Re, J2);

        Debug.Log($"════════ J2 영년 변화율 테스트 시작 (TU = {TU:F3} s) ════════");

        Case_ISS();
        Case_Polar();
        Case_Retrograde();
        Case_CriticalInclination();
        if (RunSSO) Case_SunSynchronous();

        string summary = $"════════ 결과: {passCount} 통과 / {failCount} 실패 ════════";
        if (failCount == 0) Debug.Log(summary); else Debug.LogError(summary);
    }

    // ─────────────────────────────────────────────
    //  11. ISS급: a = 6778 km, e ≈ 0, i = 51.6°  → Ω̇ ≈ -5.0°/일
    // ─────────────────────────────────────────────

    void Case_ISS()
    {
        Debug.Log("───── 11. ISS급 궤도 (i = 51.6°) ─────");
        double a = 6778.0 / ReKm, e = 0.001, inc = 51.6 * Deg;

        var coarse = Measure(a, e, inc, 100, 250);
        var fine = Measure(a, e, inc, 100, 500);

        // ① 적분기 층
        double conv = SM.Abs(fine.RaanDot - coarse.RaanDot) / SM.Abs(fine.RaanDot);
        Check("11-① dt 수렴 (250 → 500 스텝/주기)", conv < TolConvergence, $"상대변화 {conv:E2} < {TolConvergence:E0}");

        // ② 이론 층
        Theory(fine.MeanA, fine.MeanE, fine.MeanI, out double raanMean, out _);
        Theory(a, e, inc, out double raanOsc, out _);
        double relMean = SM.Abs(fine.RaanDot - raanMean) / SM.Abs(raanMean);
        double relOsc = SM.Abs(fine.RaanDot - raanOsc) / SM.Abs(raanOsc);

        Debug.Log($"  수치 Ω̇ = {PerDay(fine.RaanDot):F5}°/일  (승교점 {fine.Nodes}개)");
        Debug.Log($"  이론 Ω̇ (평균요소)     = {PerDay(raanMean):F5}°/일  상대오차 {relMean:E2}");
        Debug.Log($"  이론 Ω̇ (초기 접촉요소) = {PerDay(raanOsc):F5}°/일  상대오차 {relOsc:E2}  ← 비교용");
        Check("11-② Ω̇ vs 1차 이론 (평균요소)", relMean < TolTheoryRaan, $"상대오차 {relMean:E2} < {TolTheoryRaan:E0}");
    }

    // ─────────────────────────────────────────────
    //  12. 극궤도: i = 90° → Ω̇ = 0 (대칭성으로 정확히 0)
    // ─────────────────────────────────────────────

    void Case_Polar()
    {
        Debug.Log("───── 12. 극궤도 (i = 90°) ─────");
        double a = 6778.0 / ReKm, e = 0.001;

        var res = Measure(a, e, 90.0 * Deg, 100, 250);
        Theory(a, e, 51.6 * Deg, out double refScale, out _);   // 비교 기준 크기 (같은 a의 51.6°)
        double ratio = SM.Abs(res.RaanDot) / SM.Abs(refScale);

        Debug.Log($"  수치 Ω̇ = {PerDay(res.RaanDot):E3}°/일");
        Check("12. Ω̇ ≈ 0", ratio < 1e-6, $"|Ω̇| / |Ω̇(51.6°)| = {ratio:E2} < 1E-06");
    }

    // ─────────────────────────────────────────────
    //  13. 역행 궤도: i = 120° → Ω̇ > 0 (부호 반전)
    // ─────────────────────────────────────────────

    void Case_Retrograde()
    {
        Debug.Log("───── 13. 역행 궤도 (i = 120°) ─────");
        double a = 6778.0 / ReKm, e = 0.001, inc = 120.0 * Deg;

        var res = Measure(a, e, inc, 100, 250);
        Theory(res.MeanA, res.MeanE, res.MeanI, out double raanMean, out _);
        double rel = SM.Abs(res.RaanDot - raanMean) / SM.Abs(raanMean);

        Debug.Log($"  수치 Ω̇ = {PerDay(res.RaanDot):F5}°/일   이론(평균요소) = {PerDay(raanMean):F5}°/일");
        Check("13-a Ω̇ 부호 반전 (> 0)", res.RaanDot > 0, "");
        Check("13-b Ω̇ vs 1차 이론", rel < TolTheoryRaan, $"상대오차 {rel:E2} < {TolTheoryRaan:E0}");
    }

    // ─────────────────────────────────────────────
    //  14. 임계 경사각: i = 63.43° → ω̇ ≈ 0, 60°/66°에서 부호 반대
    //      ω는 e ≈ 0이면 정의되지 않으므로 e = 0.1
    // ─────────────────────────────────────────────

    void Case_CriticalInclination()
    {
        Debug.Log("───── 14. 임계 경사각 (ω̇) ─────");
        double a = 1.2, e = 0.1;
        double iCrit = SM.Acos(1.0 / SM.Sqrt(5.0));   // 63.4349°

        var r60 = Measure(a, e, 60.0 * Deg, 200, 250);
        var rCrit = Measure(a, e, iCrit, 200, 250);
        var r66 = Measure(a, e, 66.0 * Deg, 200, 250);

        Theory(r60.MeanA, r60.MeanE, r60.MeanI, out _, out double th60);
        Theory(r66.MeanA, r66.MeanE, r66.MeanI, out _, out double th66);
        double rel60 = SM.Abs(r60.ArgpDot - th60) / SM.Abs(th60);
        double rel66 = SM.Abs(r66.ArgpDot - th66) / SM.Abs(th66);
        double critRatio = SM.Abs(rCrit.ArgpDot) / SM.Abs(r60.ArgpDot);

        Debug.Log($"  i = 60.00°: 수치 ω̇ = {PerDay(r60.ArgpDot):F5}°/일   이론 = {PerDay(th60):F5}°/일  상대오차 {rel60:E2}");
        Debug.Log($"  i = {iCrit / Deg:F2}°: 수치 ω̇ = {PerDay(rCrit.ArgpDot):E3}°/일");
        Debug.Log($"  i = 66.00°: 수치 ω̇ = {PerDay(r66.ArgpDot):F5}°/일   이론 = {PerDay(th66):F5}°/일  상대오차 {rel66:E2}");

        Check("14-a ω̇(60°) > 0, ω̇(66°) < 0", r60.ArgpDot > 0 && r66.ArgpDot < 0, "");
        Check("14-b ω̇(63.43°) ≈ 0", critRatio < 0.05, $"|ω̇(63.43°)| / |ω̇(60°)| = {critRatio:E2} < 5E-02");
        Check("14-c ω̇ vs 1차 이론 (60°, 66°)", rel60 < TolTheoryArgp && rel66 < TolTheoryArgp,
              $"상대오차 {rel60:E2}, {rel66:E2} < {TolTheoryArgp:E0}");
    }

    // ─────────────────────────────────────────────
    //  15. 태양동기궤도: 고도 700 km 원궤도, 1년 적분
    //      (a) 1차 해석식으로 경사각 설계 → 1년 적분 → 태양 대비 궤도면 드리프트
    //      (b) 측정된 Ω̇로 경사각을 뉴턴 보정 1회 → 재적분 → 드리프트 감소 확인
    // ─────────────────────────────────────────────

    void Case_SunSynchronous()
    {
        Debug.Log("───── 15. 태양동기궤도 (700 km, 1년) ─────");

        double a = (ReKm + 700.0) / ReKm, e = 0.001;
        double year = 365.2422;                                   // 일
        double sunRate = 2 * SM.PI / (year * 86400.0) * TU;       // 태양 평균 이동 각속도 (rad/TU)

        double period = 2 * SM.PI * SM.Sqrt(a * a * a / Mu);
        int orbits = (int)(year * 86400.0 / TU / period);         // 약 1년

        // (a) 해석 설계: cos i = -Ω̇_sun / (1.5 n J2 (Re/p)²)  — 초기 접촉요소 기준
        double n = SM.Sqrt(Mu / (a * a * a));
        double p = a * (1 - e * e);
        double f = n * J2 * (Re / p) * (Re / p);
        double inc0 = SM.Acos(-sunRate / (1.5 * f));

        var res0 = Measure(a, e, inc0, orbits, 200);
        double rel0 = SM.Abs(res0.RaanDot - sunRate) / sunRate;
        double drift0 = (res0.RaanDot - sunRate) * orbits * period / Deg;   // 1년간 태양 대비 궤도면 드리프트 (°)

        Debug.Log($"  (a) 해석 설계 i = {inc0 / Deg:F4}°");
        Debug.Log($"      수치 Ω̇ = {PerDay(res0.RaanDot):F5}°/일  (목표 {PerDay(sunRate):F5}°/일)  상대오차 {rel0:E2}");
        Debug.Log($"      1년 드리프트 {drift0:F3}°  (= 지방시 {drift0 / 15.0 * 60.0:F1}분)");
        Check("15-a Ω̇ ≈ 태양 이동률 (해석 설계)", rel0 < 5e-3, $"상대오차 {rel0:E2} < 5E-03");

        // (b) 뉴턴 보정: Ω̇(i) ≈ -1.5 f cos i 이므로 dΩ̇/di ≈ 1.5 f sin i
        double inc1 = inc0 - (res0.RaanDot - sunRate) / (1.5 * f * SM.Sin(inc0));

        var res1 = Measure(a, e, inc1, orbits, 200);
        double rel1 = SM.Abs(res1.RaanDot - sunRate) / sunRate;
        double drift1 = (res1.RaanDot - sunRate) * orbits * period / Deg;

        Debug.Log($"  (b) 보정 후 i = {inc1 / Deg:F4}°  (Δi = {(inc1 - inc0) / Deg:+0.0000;-0.0000}°)");
        Debug.Log($"      수치 Ω̇ = {PerDay(res1.RaanDot):F5}°/일  상대오차 {rel1:E2}");
        Debug.Log($"      1년 드리프트 {drift1:F3}°  (= 지방시 {drift1 / 15.0 * 60.0:F1}분)");
        Check("15-b 보정 후 드리프트 감소", SM.Abs(drift1) < 0.1 * SM.Abs(drift0),
              $"|{drift1:F3}°| < 0.1 × |{drift0:F3}°|");
    }

    // ─────────────────────────────────────────────
    //  측정: 적분 + 승교점 표본 추출 + 회귀
    // ─────────────────────────────────────────────

    RateResult Measure(double a, double e, double inc, int orbits, int stepsPerOrbit)
    {
        // Ω0 = 0, ω0 = 0, ν0 = 30° 에서 출발
        ElementsToState(a, e, inc, 30.0 * Deg, out Vector3D r, out Vector3D v);

        double period = 2 * SM.PI * SM.Sqrt(a * a * a / Mu);
        double dt = period / stepsPerOrbit;
        int total = orbits * stepsPerOrbit;

        var tList = new List<double>();
        var raanList = new List<double>();
        var argpList = new List<double>();
        var raanUnwrap = new Unwrapper();
        var argpUnwrap = new Unwrapper();

        double sumA = 0, sumE = 0, sumI = 0;

        for (int s = 0; s < total; s++)
        {
            Vector3D rPrev = r, vPrev = v;
            RK4(ref r, ref v, dt, accel);

            Osculating(r, v, out double oa, out double oe, out double oi);
            sumA += oa; sumE += oe; sumI += oi;

            // 승교점 통과: z 음 → 양. 선형 보간으로 통과 시점의 상태 추정
            if (rPrev.z < 0 && r.z >= 0)
            {
                double f = -rPrev.z / (r.z - rPrev.z);
                Vector3D rn = rPrev + (r - rPrev) * f;
                Vector3D vn = vPrev + (v - vPrev) * f;

                NodeAngles(rn, vn, out double raan, out double argp);
                tList.Add((s + f) * dt);
                raanList.Add(raanUnwrap.Next(raan));
                argpList.Add(argpUnwrap.Next(argp));
            }
        }

        return new RateResult
        {
            RaanDot = FitSlope(tList, raanList),
            ArgpDot = FitSlope(tList, argpList),
            MeanA = sumA / total,
            MeanE = sumE / total,
            MeanI = sumI / total,
            Nodes = tList.Count
        };
    }

    // ─────────────────────────────────────────────
    //  이론 / 궤도요소 보조 함수
    // ─────────────────────────────────────────────

    /// <summary>J2 1차 영년 변화율 (rad/TU)</summary>
    static void Theory(double a, double e, double inc, out double raanDot, out double argpDot)
    {
        double n = SM.Sqrt(Mu / (a * a * a));
        double p = a * (1 - e * e);
        double f = n * J2 * (Re / p) * (Re / p);
        double c = SM.Cos(inc);
        raanDot = -1.5 * f * c;
        argpDot = 0.75 * f * (5 * c * c - 1);
    }

    /// <summary>궤도요소(Ω = ω = 0) → 상태벡터</summary>
    static void ElementsToState(double a, double e, double inc, double nu, out Vector3D r, out Vector3D v)
    {
        double p = a * (1 - e * e);
        double rr = p / (1 + e * SM.Cos(nu));
        double vs = SM.Sqrt(Mu / p);

        // 근점 좌표계 (Ω = ω = 0이면 x축 = 승교점 방향 = 근지점 방향)
        double xp = rr * SM.Cos(nu), yp = rr * SM.Sin(nu);
        double vxp = -vs * SM.Sin(nu), vyp = vs * (e + SM.Cos(nu));

        // x축 기준 경사각 회전
        double ci = SM.Cos(inc), si = SM.Sin(inc);
        r = new Vector3D(xp, yp * ci, yp * si);
        v = new Vector3D(vxp, vyp * ci, vyp * si);
    }

    /// <summary>접촉요소 a, e, i (2체 기준)</summary>
    static void Osculating(in Vector3D r, in Vector3D v, out double a, out double e, out double inc)
    {
        double rm = r.Magnitude();
        double v2 = Vector3D.Dot(v, v);
        a = 1.0 / (2.0 / rm - v2 / Mu);
        e = EccVector(r, v).Magnitude();
        Vector3D h = Vector3D.Cross(r, v);
        inc = SM.Acos(SM.Max(-1.0, SM.Min(1.0, h.z / h.Magnitude())));
    }

    /// <summary>이심률 벡터 e = ((v² - μ/r) r - (r·v) v) / μ</summary>
    static Vector3D EccVector(in Vector3D r, in Vector3D v)
    {
        double rm = r.Magnitude();
        double v2 = Vector3D.Dot(v, v);
        return (r * (v2 - Mu / rm) - v * Vector3D.Dot(r, v)) * (1.0 / Mu);
    }

    /// <summary>Ω, ω 계산 (승교점 벡터 N = k × h 기준)</summary>
    static void NodeAngles(in Vector3D r, in Vector3D v, out double raan, out double argp)
    {
        Vector3D h = Vector3D.Cross(r, v);
        Vector3D hHat = h * (1.0 / h.Magnitude());

        var nodeVec = new Vector3D(-h.y, h.x, 0);   // k × h
        Vector3D nHat = nodeVec * (1.0 / nodeVec.Magnitude());
        raan = SM.Atan2(nHat.y, nHat.x);

        Vector3D ev = EccVector(r, v);
        Vector3D eHat = ev * (1.0 / ev.Magnitude());
        double cosW = Vector3D.Dot(nHat, eHat);
        double sinW = Vector3D.Dot(Vector3D.Cross(nHat, eHat), hHat);
        argp = SM.Atan2(sinW, cosW);
    }

    /// <summary>선형 최소제곱 기울기</summary>
    static double FitSlope(List<double> t, List<double> y)
    {
        int n = t.Count;
        if (n < 2) return double.NaN;
        double tm = 0, ym = 0;
        for (int k = 0; k < n; k++) { tm += t[k]; ym += y[k]; }
        tm /= n; ym /= n;
        double num = 0, den = 0;
        for (int k = 0; k < n; k++) { num += (t[k] - tm) * (y[k] - ym); den += (t[k] - tm) * (t[k] - tm); }
        return num / den;
    }

    /// <summary>각도 연속화 (±π 경계 넘을 때 2π 보정 누적)</summary>
    class Unwrapper
    {
        bool first = true;
        double prev, offset;
        public double Next(double ang)
        {
            if (!first)
            {
                double d = ang - prev;
                if (d > SM.PI) offset -= 2 * SM.PI;
                else if (d < -SM.PI) offset += 2 * SM.PI;
            }
            first = false;
            prev = ang;
            return ang + offset;
        }
    }

    /// <summary>rad/TU → °/일</summary>
    static double PerDay(double radPerTU) => radPerTU / TU * 86400.0 / Deg;

    void Check(string label, bool ok, string detail)
    {
        string msg = $"[{(ok ? "PASS" : "FAIL")}] {label}  {detail}";
        if (ok) { passCount++; Debug.Log(msg); }
        else { failCount++; Debug.LogError(msg); }
    }
}
