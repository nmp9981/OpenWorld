namespace MathD.Integration
{
    // ═══════════════════════════════════════════════════════════
    //  시스템 인터페이스
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 가속도가 위치에만 의존하는 시스템 (분리 가능 Hamiltonian).
    /// symplectic 적분기의 전제. 속도 의존 힘이 있으면 IODESystem 사용.
    /// </summary>
    public interface ISeparableSystem
    {
        int BodyCount { get; }

        /// <summary>
        /// positions에서의 가속도를 accelOut에 채운다.
        /// positions를 변경하지 말 것. accelOut은 전부 덮어쓸 것.
        /// </summary>
        void Acceleration(double t, Vector3D[] positions, Vector3D[] accelOut);
    }

    /// <summary>일반 1계 ODE. 2계는 y=[x,v]로 낮춰서 사용.</summary>
    public interface IODESystem
    {
        int Dimension { get; }
        void Derivative(double t, double[] y, double[] dydt);
    }

    /// <summary>보존량 계산. 검증 하네스가 사용.</summary>
    public interface IConservativeSystem
    {
        double KineticEnergy(Vector3D[] vel);
        double PotentialEnergy(Vector3D[] pos);
        Vector3D LinearMomentum(Vector3D[] vel);
        Vector3D AngularMomentum(Vector3D[] pos, Vector3D[] vel);
    }


    // ═══════════════════════════════════════════════════════════
    //  적분기 인터페이스 — 상태는 호출자 소유, in-place 갱신
    // ═══════════════════════════════════════════════════════════

    public interface ISymplecticIntegrator
    {
        /// <summary>전역 오차 차수. O(dt^Order)</summary>
        int Order { get; }

        /// <summary>true면 장기 에너지가 유계로 진동 (드리프트 없음).</summary>
        bool IsSymplectic { get; }

        /// <summary>true면 dt→-dt로 정확히 되돌아옴.</summary>
        bool IsTimeReversible { get; }

        /// <summary>한 스텝당 Acceleration 호출 횟수. 성능 비교용.</summary>
        int ForceEvaluationsPerStep { get; }

        /// <summary>내부 버퍼 준비. 루프 진입 전 1회. bodyCount가 바뀌면 재호출.</summary>
        void Prepare(int bodyCount);

        /// <summary>t에서 t+dt로 한 스텝. pos/vel은 in-place 갱신.</summary>
        void Step(ISeparableSystem sys, double t, double dt,
                  Vector3D[] pos, Vector3D[] vel);
    }

    public interface IODEIntegrator
    {
        int Order { get; }
        bool IsSymplectic { get; }
        int DerivativeEvaluationsPerStep { get; }

        void Prepare(int dimension);
        void Step(IODESystem sys, double t, double dt, double[] y);
    }

    /// <summary>적응 스텝. 이심률 큰 궤도, 강성 문제용.</summary>
    public interface IAdaptiveIntegrator : IODEIntegrator
    {
        double AbsoluteTolerance { get; set; }
        double RelativeTolerance { get; set; }
        double MinStep { get; set; }
        double MaxStep { get; set; }

        /// <summary>
        /// 오차 추정에 따라 스텝을 시도. 
        /// 성공하면 t와 y가 갱신되고 dt에 다음 권장 스텝이 담긴다.
        /// 실패하면 t, y는 불변이고 dt만 축소된다 — 재호출할 것.
        /// </summary>
        bool TryStep(IODESystem sys, ref double t, ref double dt, double[] y);
    }
}