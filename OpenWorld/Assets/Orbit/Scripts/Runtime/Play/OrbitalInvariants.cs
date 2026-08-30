/// <summary>
/// 보존량 3종
/// </summary>
public static class OrbitalInvariants
{

    /// <summary>
    /// 보존량 H
    /// </summary>
    /// <returns></returns>
    public static Vector3D SpecificAngularMomentum(StateVector state)
    {
        return Vector3D.Cross(state.Position, state.Velocity);
    }

    /// <summary>
    /// 에너지 
    /// </summary>
    /// <param name="central"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public static double SpecificMechanicalEnergy(CentralBody central, StateVector state)
    {
        double kineticEnergy = 0.5 * state.Velocity.SqrMagnitude();
        double potentialEnergy = -central.Mu / state.Position.Magnitude();
        return kineticEnergy + potentialEnergy;
    }

    /// <summary>
    ///궤도 이심률 계산
    /// </summary>
    /// <returns></returns>
    public static Vector3D EccentricityVector(CentralBody central, StateVector state)
    {
        double velocitySq = state.Velocity.SqrMagnitude();
        double r = state.Position.Magnitude();
        //BAC
        Vector3D BAC = (velocitySq - central.Mu / r) * state.Position;
        //CAB
        Vector3D CAB = Vector3D.Dot(state.Position, state.Velocity) * state.Velocity;

        //이심률
        Vector3D eccentricityVector = (BAC - CAB) / central.Mu;
        return eccentricityVector;
    }
}
