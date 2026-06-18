using System;
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
    /// 각 가속도1 계산
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
    public static double DoublePendulum_AnaleAccel1(double l1, double l2,double m1, double m2, double theta1, double theta2, double w1, double w2)
    {
        double deltaTheta = theta1 - theta2;
        
        //분모
        double down = l1 * (2 * m1 + m2 - m2 * MathUtility.Cos(2 * deltaTheta));
        //분자
        double upper1 = -ConstUtility.gravity * (2 * m1 + m2) * MathUtility.Sin(theta1);
        double upper2 = -m2 * ConstUtility.gravity * MathUtility.Sin(theta1-2*theta2);
        double upper3 = -2 * MathUtility.Sin(deltaTheta) * m2 * (w2*w2*l2+theta1*theta1*l1*MathUtility.Cos(deltaTheta));

        return (upper1 + upper2 + upper3) / down;
    }
    /// <summary>
    /// 각 가속도2 계산
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
    public static double DoublePendulum_AnaleAccel2(double l1, double l2, double m1, double m2, double theta1, double theta2, double w1, double w2)
    {
        double deltaTheta = theta1 - theta2;

        //분모
        double down = l2 * (2 * m1 + m2 - m2 * MathUtility.Cos(2 * deltaTheta));
        //분자
        double upper1 = (m1 + m2) * w1 * w1 * l1; ;
        double upper2 = ConstUtility.gravity*(m1+m2)*MathUtility.Cos(theta1);
        double upper3 = w2 * w2 * l2 * m2 * MathUtility.Cos(deltaTheta);
        double upper = 2 * MathUtility.Sin(deltaTheta)*(upper1+upper2+upper3);

        return upper / down;
    }
}
