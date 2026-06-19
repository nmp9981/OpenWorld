using System;
using System.Resources;
using System.Transactions;
using Unity.Collections;

public static class PhysicsFormula
{
    /// <summary>
    /// 만유인력 힘
    /// </summary>
    /// <returns></returns>
    public static Vector3D Force_UniversalGravitation(Vector3D r, double M, double m)
    {
        if (r.Magnitude() <= ConstUtility.Epcilon12) return Vector3D.ZeroVector();

        double up = M * m * ConstUtility.G;//10^-11을 추후 곱해야함
        double rSize = r.Magnitude();
        double down = rSize*rSize+ConstUtility.Epcilon12;
        double forceSize = up / down;
        Vector3D result = r.Normalized() * forceSize;
        return result;
    }

    /// <summary>
    /// 힘으로부터 가속도 구하기
    /// F=ma
    /// </summary>
    /// <param name="force"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    public static Vector3D Accel_From_Force(Vector3D force,double m)
    {
        return force / m;
    }

    /// <summary>
    /// 운동량 계산
    /// </summary>
    /// <param name="m"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector3D Cal_Momentum(double m,Vector3D v)
    {
        return v * m;
    }

    /// <summary>
    /// 각 가속도 계산
    /// </summary>
    /// <param name="l1"></param>
    /// <param name="l2"></param>
    /// <param name="m1"></param>
    /// <param name="m2"></param>
    /// <param name="theta1"></param>
    /// <param name="theta2"></param>
    /// <param name="w1"></param>
    /// <param name="w2"></param>
    /// <returns></returns>
    public static PenduiumDerived DoublePendulum_AnaleAccel(double l1, double l2,double m1, double m2,PenduiumState state)
    {
        double deltaTheta = state.angle1 - state.angle2;
        
        //각 가속도1
        //분모
        double down = l1 * (2 * m1 + m2 - m2 * MathUtility.Cos(2 * deltaTheta));
        //분자
        double upper1 = -ConstUtility.gravity * (2 * m1 + m2) * MathUtility.Sin(state.angle1);
        double upper2 = -m2 * ConstUtility.gravity * MathUtility.Sin(state.angle1 - 2* state.angle2);
        double upper3 = -2 * MathUtility.Sin(deltaTheta) * m2 * (state.angleVelocity2* state.angleVelocity2 * l2
            + state.angleVelocity1 * state.angleVelocity1 * l1*MathUtility.Cos(deltaTheta));
       
        //각 가속도2
        //분모
        double down2 = l2 * (2 * m1 + m2 - m2 * MathUtility.Cos(2 * deltaTheta));
        //분자
        double upper21 = (m1 + m2) * state.angleVelocity1 * state.angleVelocity1 * l1;
        double upper22 = ConstUtility.gravity * (m1 + m2) * MathUtility.Cos(state.angle1);
        double upper23 = state.angleVelocity2 * state.angleVelocity2 * l2 * m2 * MathUtility.Cos(deltaTheta);
        double upperAccel2 = 2 * MathUtility.Sin(deltaTheta) * (upper21 + upper22 + upper23);

        PenduiumDerived newDerived = new PenduiumDerived();
        newDerived.d_angleVelocity1 = (upper1 + upper2 + upper3) / down;
        newDerived.d_angle1 = state.angleVelocity1;
        newDerived.d_angleVelocity2 = upperAccel2 / down2;
        newDerived.d_angle2 = state.angleVelocity2;

        return newDerived;
    }
}
