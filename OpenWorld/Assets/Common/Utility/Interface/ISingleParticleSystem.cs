namespace MathD.Integration
{
    /// <summary>단일 입자 상태.</summary>
    public struct ParticleState
    {
        public Vector3D position;
        public Vector3D velocity;

        public ParticleState(Vector3D p, Vector3D v) { position = p; velocity = v; }
    }

    /// <summary>단일 입자 시스템. 배열 인터페이스보다 구현이 간단하다.</summary>
    public interface ISingleParticleSystem
    {
        Vector3D Acceleration(double t, Vector3D position);
    }

    /// <summary>
    /// ISingleParticleSystem을 ISeparableSystem으로 변환.
    /// 길이 1 배열로 감싸므로 오버헤드는 무시할 수준.
    /// </summary>
    public sealed class SingleParticleAdapter : ISeparableSystem
    {
        private readonly ISingleParticleSystem inner;
        public SingleParticleAdapter(ISingleParticleSystem s) { inner = s; }

        public int BodyCount => 1;

        public void Acceleration(double t, Vector3D[] positions, Vector3D[] accelOut)
            => accelOut[0] = inner.Acceleration(t, positions[0]);
    }

    /// <summary>구조체 상태를 배열 적분기로 전진시키는 헬퍼.</summary>
    public sealed class ParticleIntegrator
    {
        private readonly ISymplecticIntegrator integrator;
        private readonly ISeparableSystem system;
        private readonly Vector3D[] pos = new Vector3D[1];
        private readonly Vector3D[] vel = new Vector3D[1];

        public ParticleIntegrator(ISymplecticIntegrator integ, ISingleParticleSystem sys)
        {
            integrator = integ;
            system = new SingleParticleAdapter(sys);
            integrator.Prepare(1);
        }

        public ParticleState Step(ParticleState state, double t, double dt)
        {
            pos[0] = state.position;
            vel[0] = state.velocity;
            integrator.Step(system, t, dt, pos, vel);
            return new ParticleState(pos[0], vel[0]);
        }
    }
}