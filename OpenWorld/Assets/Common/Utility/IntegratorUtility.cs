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
