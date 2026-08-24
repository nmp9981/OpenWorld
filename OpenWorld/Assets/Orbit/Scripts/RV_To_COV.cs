using UnityEngine;

public readonly struct CentralBody
{
    public readonly double Mu;          // km³/s²
    public readonly double Radius;      // 2단계에서 측지좌표에 필요
    public readonly double J2;          // 3단계에서 채움
    public readonly double RotationRate;// 2단계 ECEF 변환
}

public readonly struct StateVector
{
    public readonly Vector3D Position;   // km
    public readonly Vector3D Velocity;   // km/s
}

public enum OrbitGeometry { General, CircularInclined, EllipticalEquatorial, CircularEquatorial }

public class RV_To_COV : MonoBehaviour
{
   /// <summary>
   /// 보존량 H
   /// </summary>
   /// <returns></returns>
    Vector3D SpecificAngularMomentum(StateVector state)
    {
        return Vector3D.Cross(state.Position, state.Velocity);
    }

    /// <summary>
    /// 에너지 
    /// </summary>
    /// <param name="central"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    double SpecificMechanicalEnergy(CentralBody central, StateVector state)
    {
        double kineticEnergy = 0.5 * state.Velocity.SqrMagnitude();
        double potentialEnergy = -central.Mu / state.Position.Magnitude();
        return kineticEnergy + potentialEnergy;
    }

    /// <summary>
    ///궤도 이심률 계산
    /// </summary>
    /// <returns></returns>
    Vector3D EccentricityVector(CentralBody central, StateVector state)
    {
        double velocitySq = state.Velocity.SqrMagnitude();
        double r = state.Position.Magnitude();
        //BAC
        Vector3D BAC = (velocitySq-central.Mu/r) * state.Position;
        //CAB
        Vector3D CAB = Vector3D.Dot(state.Position, state.Velocity) * state.Velocity;

        //이심률
        Vector3D eccentricityVector = (BAC - CAB) / central.Mu;
        return eccentricityVector;
    }
}
