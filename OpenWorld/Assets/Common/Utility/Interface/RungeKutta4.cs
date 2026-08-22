namespace MathD.Integration
{
    /// <summary>
    /// 고전적 4단계 Runge-Kutta.
    /// 4차 정확도지만 symplectic이 아니어서 장기 에너지가 드리프트한다.
    /// 스텝당 힘 계산 4회 — Verlet 대비 4배 비용.
    /// </summary>
    public class RungeKutta4 : ISymplecticIntegrator
    {
        // 중간 상태
        private Vector3D[] tmpPos, tmpVel;
        // 각 단계의 기울기 (x' = v, v' = a)
        private Vector3D[] k1v, k2v, k3v, k4v;   // 속도의 기울기 = 가속도
        private Vector3D[] k1x, k2x, k3x, k4x;   // 위치의 기울기 = 속도

        public int Order => 4;
        public bool IsSymplectic => false;
        public bool IsTimeReversible => false;
        public int ForceEvaluationsPerStep => 4;

        public void Prepare(int n)
        {
            if (tmpPos != null && tmpPos.Length == n) return;

            tmpPos = new Vector3D[n]; tmpVel = new Vector3D[n];
            k1v = new Vector3D[n]; k2v = new Vector3D[n];
            k3v = new Vector3D[n]; k4v = new Vector3D[n];
            k1x = new Vector3D[n]; k2x = new Vector3D[n];
            k3x = new Vector3D[n]; k4x = new Vector3D[n];
        }

        public void Step(ISeparableSystem sys, double t, double dt,
                         Vector3D[] pos, Vector3D[] vel)
        {
            int n = pos.Length;
            if (tmpPos == null || tmpPos.Length != n) Prepare(n);

            double h2 = dt * 0.5;

            // ── k1: 시작점 ──
            for (int i = 0; i < n; i++) k1x[i] = vel[i];
            sys.Acceleration(t, pos, k1v);

            // ── k2: 중간점, k1으로 전진 ──
            for (int i = 0; i < n; i++)
            {
                tmpPos[i] = pos[i] + k1x[i] * h2;
                k2x[i] = vel[i] + k1v[i] * h2;
            }
            sys.Acceleration(t + h2, tmpPos, k2v);

            // ── k3: 중간점, k2로 전진 ──
            for (int i = 0; i < n; i++)
            {
                tmpPos[i] = pos[i] + k2x[i] * h2;
                k3x[i] = vel[i] + k2v[i] * h2;
            }
            sys.Acceleration(t + h2, tmpPos, k3v);

            // ── k4: 끝점, k3으로 전진 ──
            for (int i = 0; i < n; i++)
            {
                tmpPos[i] = pos[i] + k3x[i] * dt;
                k4x[i] = vel[i] + k3v[i] * dt;
            }
            sys.Acceleration(t + dt, tmpPos, k4v);

            // ── 가중 결합 (1, 2, 2, 1) / 6 ──
            double h6 = dt / 6.0;
            for (int i = 0; i < n; i++)
            {
                pos[i] += (k1x[i] + (k2x[i] + k3x[i]) * 2.0 + k4x[i]) * h6;
                vel[i] += (k1v[i] + (k2v[i] + k3v[i]) * 2.0 + k4v[i]) * h6;
            }
        }
    }
}