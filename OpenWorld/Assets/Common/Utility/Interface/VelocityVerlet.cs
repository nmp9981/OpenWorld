namespace MathD.Integration
{
    /// <summary>
    /// Velocity Verlet (kick-drift-kick).
    /// 2차 정확도, symplectic, 시간 가역. 분리 가능 시스템의 실용 표준.
    /// 가속도를 재사용하여 스텝당 힘 계산 1회.
    /// </summary>
    public class VelocityVerlet : ISymplecticIntegrator
    {
        private Vector3D[] accel;
        private bool primed;        // 첫 스텝에서만 가속도 계산

        public int Order => 2;
        public bool IsSymplectic => true;
        public bool IsTimeReversible => true;
        public int ForceEvaluationsPerStep => 1;   // 재사용 덕분

        public void Prepare(int bodyCount)
        {
            if (accel == null || accel.Length != bodyCount)
                accel = new Vector3D[bodyCount];
            primed = false;
        }

        public void Step(ISeparableSystem sys, double t, double dt,
                         Vector3D[] pos, Vector3D[] vel)
        {
            int n = pos.Length;
            if (accel == null || accel.Length != n) Prepare(n);

            // 첫 호출에만 a(x_n) 계산. 이후는 직전 스텝 마지막 값을 재사용.
            if (!primed)
            {
                sys.Acceleration(t, pos, accel);
                primed = true;
            }

            double half = dt * 0.5;

            for (int i = 0; i < n; i++)
            {
                vel[i] += accel[i] * half;   // kick
                pos[i] += vel[i] * dt;       // drift
            }

            sys.Acceleration(t + dt, pos, accel);   // a(x_{n+1})

            for (int i = 0; i < n; i++)
                vel[i] += accel[i] * half;   // kick
        }
    }
}