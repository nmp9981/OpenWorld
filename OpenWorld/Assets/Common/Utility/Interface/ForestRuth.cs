using MathD.Integration;
using UnityEngine;

public sealed class ForestRuth : ISymplecticIntegrator
{
    private const double W = 1.3512071919596576;
    private const double W1 = 1.0 - 2.0 * W;   // ≈ -1.702

    private readonly VelocityVerlet inner = new VelocityVerlet();

    public int Order => 4;
    public bool IsSymplectic => true;
    public bool IsTimeReversible => true;
    public int ForceEvaluationsPerStep => 6;   // Verlet 2회 × 3

    public void Prepare(int n) => inner.Prepare(n);

    public void Step(ISeparableSystem sys, double t, double dt,
                     Vector3D[] pos, Vector3D[] vel)
    {
        inner.Step(sys, t, W * dt, pos, vel);
        inner.Step(sys, t + W * dt, W1 * dt, pos, vel);
        inner.Step(sys, t + (W + W1) * dt, W * dt, pos, vel);
    }
}
