using System.Text.RegularExpressions;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class Inspect_Data_Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Example_P_Run();
    }
    // 입력: jdTT = JD(TT), rJ2000 [km], vJ2000 [km/s]  (J2000 좌표)
    // 출력: rEcef [km], vEcef [km/s]  (지구 고정 좌표)
    // 정방향 변환 + 같은 행렬로 역방향 왕복 + 검사 로그까지 이 안에서 전부 함
    void Example_P(double jdTT, Vector3D rJ2000, Vector3D vJ2000,
                 out Vector3D rEcef, out Vector3D vEcef)
    {
        // 1. 시간계 변환 (이 함수에서 시각을 바꾸는 곳은 여기 한 곳)
        double jdUTC = jdTT - 69.184 / 86400.0;

        // 2. 세차: J2000 → 그날의 평균 적도·춘분점 (전치 없음)
        //    Example_J와 같은 식, 고칠 때 둘 다
        double T = (jdTT - 2451545.0) / 36525.0;
        double arcsec2Rad = ConstUtility.Deg2Rad / 3600.0;
        double zeta = (2306.2181 * T + 0.30188 * T * T + 0.017998 * T * T * T) * arcsec2Rad;
        double z = (2306.2181 * T + 1.09468 * T * T + 0.018203 * T * T * T) * arcsec2Rad;
        double precTheta = (2004.3109 * T - 0.42665 * T * T - 0.041833 * T * T * T) * arcsec2Rad;
        Matrix3x3D P = Matrix3x3D.R3Frame(-z) * Matrix3x3D.R2Frame(precTheta) * Matrix3x3D.R3Frame(-zeta);

        // 3. 지구 자전각: 그날의 평균 적도·춘분점 → 지구 고정
        //    ERA는 CIO 기준이라, 춘분점 기준 좌표를 돌리려면 GMST = ERA + 적경 세차분
        double du = jdUTC - 2451545.0;
        double turns = 0.7790572732640 + 1.00273781191135448 * du; // ≈ 1 + 1/(1년 일수): 1년에 한 바퀴 더 돎
        double era = ConstUtility.TWO_PI * (turns - MathUtility.Floor(turns));

        double gmstCorrArcsec = 0.014506 + 4612.156534 * T + 1.3915817 * T * T;   // ★ T는 위의 TT 세기 재사용
        double gmst = era + gmstCorrArcsec * arcsec2Rad;                          // ★
        gmst = gmst - ConstUtility.TWO_PI * MathUtility.Floor(gmst / ConstUtility.TWO_PI); // ★ [0, 2π)

        Matrix3x3D R = Matrix3x3D.R3Frame(gmst);                                 // ★ 행렬을 변수로 (역방향에서 재사용)

        // 4. 정방향: 위치
        Vector3D rMod = P * rJ2000;
        rEcef = R * rMod;

        // 5. 정방향: 속도 (회전 좌표 속도 = 관성 속도 − ω × r, r은 지구 고정 좌표)
        double omegaE = 7.292115e-5;                                             // ★ rad/s, r이 km면 결과 km/s
        Vector3D omega = new Vector3D(0.0, 0.0, omegaE);
        Vector3D vMod = P * vJ2000;
        vEcef = R * vMod - Vector3D.Cross(omega, rEcef);                         // ★

        // ===== 이하 검사 (같은 P, 같은 R 사용) =====

        // 6. 역방향: 지구 고정 → J2000 (순서 거꾸로, 회전 행렬의 역 = 전치)
        Matrix3x3D Rt = R.Transpose();
        Matrix3x3D Pt = P.Transpose();
        Vector3D rBack = Pt * (Rt * rEcef);
        Vector3D vInertialMod = Rt * (vEcef + Vector3D.Cross(omega, rEcef));    // ω×r을 먼저 되돌린 뒤 회전
        Vector3D vBack = Pt * vInertialMod;

        double rRoundTrip = (rBack - rJ2000).Magnitude();
        double vRoundTrip = (vBack - vJ2000).Magnitude();

        // 7. 대조 실험: 세차를 끄면 위치가 얼마나 달라지나
        Vector3D rEcefNoPrec = R * rJ2000;
        double precOffDiff = (rEcefNoPrec - rEcef).Magnitude();

        // 8. 각도 검사
        double gmstMinusEraDeg = (gmst - era) * ConstUtility.Rad2Deg;
        if (gmstMinusEraDeg < 0.0)
        {
            gmstMinusEraDeg = gmstMinusEraDeg + 360.0; // 한쪽만 2π를 넘어 감긴 경우
        }

        Debug.Log($"P ERA = {era * ConstUtility.Rad2Deg} deg, GMST = {gmst * ConstUtility.Rad2Deg} deg, GMST-ERA = {gmstMinusEraDeg} deg");
        Debug.Log($"P 위치 왕복 = {rRoundTrip} km, 속도 왕복 = {vRoundTrip} km/s");
        Debug.Log($"P 세차 끔 차이 = {precOffDiff} km");
        Debug.Log($"P |vJ2000| = {vJ2000.Magnitude()} km/s, |vEcef| = {vEcef.Magnitude()} km/s");
    }

    // 호출 예
    void Example_P_Run()
    {
        double jdTT = 2461368.5;                          // 2026-11-24 근처
        Vector3D r = new Vector3D(4000.0, 3000.0, 4500.0); // 축 위가 아닌 점

        // r에 수직이고 크기 7.6 km/s인 순행 속도
        Vector3D dir = Vector3D.Cross(new Vector3D(0.0, 0.0, 1.0), r);
        Vector3D v = dir * (7.6 / dir.Magnitude());

        Vector3D rEcef;
        Vector3D vEcef;
        Example_P(jdTT, r, v, out rEcef, out vEcef);
    }
}
