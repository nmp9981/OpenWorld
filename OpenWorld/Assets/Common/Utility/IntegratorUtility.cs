public static class IntegratorUtility
{
    /// <summary>가속도 함수: 위치 → 가속도. 시스템 정의는 호출자가 이 델리게이트로 넘긴다.</summary>
    public delegate Vector3D AccelFunc(Vector3D pos);

    /// <summary>다물체용 가속도 함수: positions를 읽어 accelOut을 전부 덮어쓴다.</summary>
    public delegate void AccelFuncN(Vector3D[] positions, Vector3D[] accelOut);

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

    // ═══════════════════════════════════════════════════════
    //  다물체 — 배열 버전
    //  버퍼(accel 등)는 호출자가 만들어 재사용한다. 유틸리티는 상태를 갖지 않는다.
    // ═══════════════════════════════════════════════════════

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
}
