using System;
using Unity.VisualScripting;

public static class IntegratorUtility
{
    /// <summary>가속도 함수: 위치 → 가속도. 시스템 정의는 호출자가 이 델리게이트로 넘긴다.</summary>
    public delegate Vector3D AccelFunc(Vector3D pos);

    /// <summary>다물체용 가속도 함수: positions를 읽어 accelOut을 전부 덮어쓴다.</summary>
    public delegate void AccelFuncN(Vector3D[] positions, Vector3D[] accelOut);

    /// <summary>
    /// RK45용 상수 계수. 7단계 Runge-Kutta-Fehlberg (RKF45) Butcher tableau.
    /// </summary>
    static readonly double a21 = 0.2;
    static readonly double[] a3 = { 3.0 / 40, 9.0 / 40 };
    static readonly double[] a4 = { 44.0 / 45.0, -56.0 / 15, 32.0 / 9 };
    static readonly double[] a5 = { 19372.0 / 6561, -25360.0 / 2187, 64448.0 / 6561, -212.0 / 729 };
    static readonly double[] a6 = { 9017.0 / 3168, -355.0 / 33, 46732.0 / 5247, 49.0 / 176, -5103.0 / 18656 };
    static readonly double[] a7 = { 35.0 / 384, 0.0, 500.0 / 1113, 125.0 / 192, -2187.0 / 6784, 11.0 / 84 };

    //4차 정확도 계수
    static readonly double[] b4 = { 5179.0 / 57600, 0, 7571.0 / 16695, 393.0 / 640, -92097.0 / 339200, 187.0 / 2100, 1.0 / 40 };
    //5차 정확도 계수 (y_next 계산용)
    static readonly double[] b5 = { 35.0 / 384, 0.0, 500.0 / 1113, 125.0 / 192, -2187.0 / 6784, 11.0 / 84, 0.0 };
    //오차 계수
    static readonly double[] e = BuildErrCoeff();
    //b5-b4
    static double[] BuildErrCoeff()
    {
        var e = new double[7];
        for (int i = 0; i < 7; i++) e[i] = b5[i] - b4[i];
        return e;
    }

    /// r ← r + v·dt (이전 v),  v ← v + a(r₀)·dt
    public static void ExplicitEuler(ref Vector3D r, ref Vector3D v, double dt, AccelFunc a)
    {
        Vector3D a0 = a(r);
        r += v * dt;
        v += a0 * dt;
    }

    /// v ← v + a(r₀)·dt 먼저, 갱신된 v로 r — ExplicitEuler와 두 줄 순서만 다름
    public static void SymplecticEuler(ref Vector3D r, ref Vector3D v, double dt, AccelFunc a)
    {
        v += a(r) * dt;
        r += v * dt;
    }

    /// r ← r + v·dt + ½a₀dt²,  v ← v + ½(a₀+a₁)dt
    public static void VelocityVerlet(ref Vector3D r, ref Vector3D v, double dt, AccelFunc a)
    {
        Vector3D a0 = a(r);
        r += v * dt + a0 * (0.5 * dt * dt);
        Vector3D a1 = a(r);
        v += (a0 + a1) * (0.5 * dt);
    }

    /// 고전 RK4. y=(r,v), f(y)=(v, a(r))에 적용
    public static void RK4(ref Vector3D r, ref Vector3D v, double dt, AccelFunc a)
    {
        double h = 0.5 * dt;

        Vector3D k1r = v; Vector3D k1v = a(r);
        Vector3D k2r = v + k1v * h; Vector3D k2v = a(r + k1r * h);
        Vector3D k3r = v + k2v * h; Vector3D k3v = a(r + k2r * h);
        Vector3D k4r = v + k3v * dt; Vector3D k4v = a(r + k3r * dt);

        double w = dt / 6.0;
        r += (k1r + k2r * 2.0 + k3r * 2.0 + k4r) * w;
        v += (k1v + k2v * 2.0 + k3v * 2.0 + k4v) * w;
    }

    /// 고전 RK45. y=(r,v), f(y)=(v, a(r))에 적용
    public static void RK45(ref Vector3D r, ref Vector3D v, ref double h,AccelFunc a, double rTol, double aTol, out bool accepted)
    {
        Vector3D k1r = v; Vector3D k1v = a(r);

        Vector3D r2 = r + (k1r * a21)*h;
        Vector3D v2 = v + (k1v * a21)*h;
        Vector3D k2r = v2; Vector3D k2v = a(r2);

        Vector3D r3 = r + (k1r * a3[0] + k2r * a3[1]) * h;
        Vector3D v3 = v + (k1v * a3[0] + k2v * a3[1]) * h;
        Vector3D k3r = v3; Vector3D k3v = a(r3);

        Vector3D r4 = r + (k1r * a4[0] + k2r * a4[1] + k3r * a4[2]) * h;
        Vector3D v4 = v + (k1v * a4[0] + k2v * a4[1] + k3v * a4[2]) * h;
        Vector3D k4r = v4; Vector3D k4v = a(r4);

        Vector3D r5 = r + (k1r * a5[0] + k2r * a5[1] + k3r * a5[2] + k4r * a5[3]) * h;
        Vector3D v5 = v + (k1v * a5[0] + k2v * a5[1] + k3v * a5[2] + k4v * a5[3]) * h;
        Vector3D k5r = v5; Vector3D k5v = a(r5);

        Vector3D r6 = r + (k1r * a6[0] + k2r * a6[1] + k3r * a6[2] + k4r * a6[3] + k5r * a6[4]) * h;
        Vector3D v6 = v + (k1v * a6[0] + k2v * a6[1] + k3v * a6[2] + k4v * a6[3] + k5v * a6[4]) * h;
        Vector3D k6r = v6; Vector3D k6v = a(r6);

        //5차해
        Vector3D rNew = r + (k1r * a7[0] + k3r * a7[2] + k4r * a7[3] + k5r * a7[4] + k6r * a7[5]) * h;
        Vector3D vNew = v + (k1v * a7[0] + k3v * a7[2] + k4v * a7[3] + k5v * a7[4] + k6v * a7[5]) * h;
        Vector3D k7r = vNew, k7v = a(rNew);

        //오차
        Vector3D errR = (k1r * e[0] + k2r * e[1] + k3r * e[2] + k4r * e[3] + k5r * e[4] + k6r * e[5] + k7r * e[6]) * h;
        Vector3D errV = (k1v * e[0] + k2v * e[1] + k3v * e[2] + k4v * e[3] + k5v * e[4] + k6v * e[5] + k7v * e[6]) * h;

        //오차 정규화
        double sc1 = aTol + rTol * MathUtility.Max(MathUtility.Abs(r.x),MathUtility.Abs(rNew.x));
        double sc2 = aTol + rTol * MathUtility.Max(MathUtility.Abs(r.y), MathUtility.Abs(rNew.y));
        double sc3 = aTol + rTol * MathUtility.Max(MathUtility.Abs(r.z), MathUtility.Abs(rNew.z));
        double sc4 = aTol + rTol * MathUtility.Max(MathUtility.Abs(v.x), MathUtility.Abs(vNew.x));
        double sc5 = aTol + rTol * MathUtility.Max(MathUtility.Abs(v.y), MathUtility.Abs(vNew.y));
        double sc6 = aTol + rTol * MathUtility.Max(MathUtility.Abs(v.z), MathUtility.Abs(vNew.z));
        double errNorm = MathUtility.Sqrt((errR.x * errR.x) / (sc1 * sc1) + (errR.y * errR.y) / (sc2 * sc2) + (errR.z * errR.z) / (sc3 * sc3)
                                    + (errV.x * errV.x) / (sc4 * sc4) + (errV.y * errV.y) / (sc5 * sc5) + (errV.z * errV.z) / (sc6 * sc6)) / MathUtility.Sqrt(6.0);

        accepted = errNorm <= 1.0;
        if (accepted) { r = rNew;v = vNew; }

        //h갱신
        h = h * MathUtility.Min(5, MathUtility.Max(0.2, 0.9 * MathUtility.Pow(MathUtility.Max(errNorm, 1e-16), -0.2)));
        
       //바닥한계
       if(h<ConstUtility.Epcilon12) throw new InvalidOperationException("RK45: 스텝 크기가 바닥에 닿음");
    }

    // ═══════════════════════════════════════════════════════
    //  다물체 — 배열 버전
    //  버퍼(accel 등)는 호출자가 만들어 재사용한다. 유틸리티는 상태를 갖지 않는다.
    // ═══════════════════════════════════════════════════════

    public static void ExplicitEuler(Vector3D[] pos, Vector3D[] vel, double dt,
                                      AccelFuncN a, Vector3D[] aBuf)
    {
        a(pos, aBuf);
        for (int i = 0; i < pos.Length; i++)
        {
            pos[i] += vel[i] * dt;
            vel[i] += aBuf[i] * dt;
        }
    }

    /// <param name="aBuf">길이 n 임시 버퍼. 내용은 덮어써진다.</param>
    public static void SymplecticEuler(Vector3D[] pos, Vector3D[] vel, double dt,
                                       AccelFuncN a, Vector3D[] aBuf)
    {
        a(pos, aBuf);
        for (int i = 0; i < pos.Length; i++)
        {
            vel[i] += aBuf[i] * dt;
            pos[i] += vel[i] * dt;
        }
    }

    /// <param name="a0Buf">길이 n 임시 버퍼 (스텝 시작 가속도)</param>
    /// <param name="a1Buf">길이 n 임시 버퍼 (스텝 끝 가속도)</param>
    public static void VelocityVerlet(Vector3D[] pos, Vector3D[] vel, double dt,
                                      AccelFuncN a, Vector3D[] a0Buf, Vector3D[] a1Buf)
    {
        a(pos, a0Buf);
        for (int i = 0; i < pos.Length; i++)
            pos[i] += vel[i] * dt + a0Buf[i] * (0.5 * dt * dt);

        a(pos, a1Buf);
        for (int i = 0; i < pos.Length; i++)
            vel[i] += (a0Buf[i] + a1Buf[i]) * (0.5 * dt);
    }

    /// 고전 RK4 — 다물체 버전.
    /// 핵심: 각 k 단계마다 "전체 시스템"을 중간 상태로 옮긴 뒤 AccelFuncN을 배열 단위로 1회 호출.
    /// 물체별로 독립 RK4를 돌리면 상호작용이 사라지므로 절대 금지.
    ///
    /// 버퍼 8개 (모두 길이 n, 호출자가 만들어 재사용):
    ///   k1v~k4v : 각 단계의 가속도
    ///   tmpPos  : 중간 위치 (매 단계 재사용)
    ///   k2r~k4r : 중간 속도 (= 각 단계에서 r의 기울기)   ※ k1r은 vel 그 자체라 버퍼 불필요
    public static void RK4(Vector3D[] pos, Vector3D[] vel, double dt, AccelFuncN a,
                       Vector3D[] k1v, Vector3D[] k2v, Vector3D[] k3v, Vector3D[] k4v,
                       Vector3D[] tmpPos,
                       Vector3D[] k2r, Vector3D[] k3r, Vector3D[] k4r)
    {
        int n = pos.Length;
        double h = 0.5 * dt;

        // k1: 현재 상태.  k1r = vel,  k1v = a(pos)
        a(pos, k1v);

        // k2: 전 물체를 k1 방향으로 반 스텝 옮긴 구성에서 평가
        for (int i = 0; i < n; i++)
        {
            tmpPos[i] = pos[i] + vel[i] * h;      // pos + h·k1r
            k2r[i] = vel[i] + k1v[i] * h;
        }
        a(tmpPos, k2v);

        // k3: 전 물체를 k2 방향으로 반 스텝 옮긴 구성에서 평가
        for(int i = 0; i < n; i++)
        {
            tmpPos[i] = pos[i] + k2r[i] * h;      // pos + h·k2r
            k3r[i] = vel[i] + k2v[i] * h;
        }
        a(tmpPos, k3v);

        // k4: 전 물체를 k3 방향으로 한 스텝 옮긴 구성에서 평가
        for (int i = 0; i < n; i++)
        {
            tmpPos[i] = pos[i] + k3r[i] * dt;     // pos + dt·k3r
            k4r[i] = vel[i] + k3v[i] * dt;
        }
        a(tmpPos, k4v);

        // 합성 1:2:2:1.  k1r = vel
        double w = dt / 6.0;
        for (int i = 0; i < n; i++)
        {
            pos[i] += (vel[i] + k2r[i] * 2.0 + k3r[i] * 2.0 + k4r[i]) * w;
            vel[i] += (k1v[i] + k2v[i] * 2.0 + k3v[i] * 2.0 + k4v[i]) * w;
        }
    }
}
