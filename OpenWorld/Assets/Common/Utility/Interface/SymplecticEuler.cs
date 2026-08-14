namespace MathD.Integration
{
    /// <summary>
    /// Symplectic Euler (semi-implicit Euler).
    /// 1차 정확도지만 위상공간 넓이를 정확히 보존하여 장기 에너지가 유계.
    /// v를 먼저 갱신하고 그 v로 x를 갱신하는 순서가 핵심.
    /// </summary>
    public sealed class SymplecticEuler : ISymplecticIntegrator
    {
        private Vector3D[] accel;

        public int Order => 1;
        public bool IsSymplectic => true;
        public bool IsTimeReversible => false;   // 순서 비대칭 때문
        public int ForceEvaluationsPerStep => 1;

        public void Prepare(int bodyCount)
        {
            if (accel == null || accel.Length != bodyCount)
                accel = new Vector3D[bodyCount];
        }

        public void Step(ISeparableSystem sys, double t, double dt,
                         Vector3D[] pos, Vector3D[] vel)
        {
            int n = pos.Length;
            if (accel == null || accel.Length != n) Prepare(n);

            sys.Acceleration(t, pos, accel);

            for (int i = 0; i < n; i++)
            {
                vel[i] += accel[i] * dt;   // 먼저 속도
                pos[i] += vel[i] * dt;     // 새 속도로 위치
            }
        }
    }

    /// <summary>
    /// Explicit Euler. 위상공간 넓이가 팽창하여 에너지가 지수적으로 증가한다.
    /// 실사용 금지 — symplectic 적분기와의 대비를 보이기 위한 참조 구현.
    /// </summary>
    public sealed class ExplicitEuler : ISymplecticIntegrator
    {
        private Vector3D[] accel;

        public int Order => 1;
        public bool IsSymplectic => false;
        public bool IsTimeReversible => false;
        public int ForceEvaluationsPerStep => 1;

        public void Prepare(int bodyCount)
        {
            if (accel == null || accel.Length != bodyCount)
                accel = new Vector3D[bodyCount];
        }

        public void Step(ISeparableSystem sys, double t, double dt,
                         Vector3D[] pos, Vector3D[] vel)
        {
            int n = pos.Length;
            if (accel == null || accel.Length != n) Prepare(n);

            sys.Acceleration(t, pos, accel);

            for (int i = 0; i < n; i++)
            {
                pos[i] += vel[i] * dt;     // 옛 속도로 위치
                vel[i] += accel[i] * dt;
            }
        }
    }
}