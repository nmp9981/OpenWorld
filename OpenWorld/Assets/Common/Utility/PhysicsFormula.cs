using System;
using System.Diagnostics;
using System.Resources;
using System.Transactions;
using Unity.Collections;
using Unity.VisualScripting;

public static class PhysicsFormula
{
    /// <summary>
    /// 만유인력 힘
    /// </summary>
    /// <returns></returns>
    public static Vector3D Force_UniversalGravitation(Vector3D r, double M, double m)
    {
        if (r.Magnitude() <= ConstUtility.Epcilon12) return Vector3D.Zero;

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

    /// <summary>
    /// 밀도 구하기
    /// </summary>
    /// <param name="H"></param>
    /// <returns></returns>
    public static double Rho(double height)
    {
        double p0=1.0;
        double h0=1.0;
        double H = 1.0;

        if (height < 250)//예외
        {
            throw new ArgumentOutOfRangeException("범위 초과");
        }
        else if (height >= 250 && height < 300)
        {
            p0 = 7.248e-11;
            h0 = 250;
            H = 45.546;
        }
        else if (height >= 300 && height < 350)
        {
            p0 = 2.418e-11;
            h0 = 300;
            H = 53.628;
        }
        else if (height >= 350 && height < 400)
        {
            p0 = 9.518e-12;
            h0 = 350;
            H = 53.298;
        }
        else if (height >= 400 && height < 450)
        {
            p0 = 3.725e-12;
            h0 = 400;
            H = 58.515;
        }
        else if (height >= 450 && height < 500)
        {
            p0 = 1.585e-12;
            h0 = 450;
            H = 60.828;
        }
        else if (height >= 500 && height < 600)
        {
            p0 = 6.967e-13;
            h0 = 500;
            H = 63.822;
        }
        else if (height >= 600 && height < 700)
        {
            p0 = 1.454e-13;
            h0 = 600;
            H = 71.835;
        }
        else if (height >= 700 && height < 800)
        {
            p0 = 3.614e-14;
            h0 = 700;
            H = 88.667;
        }
        else if (height >= 800)
        {
            p0 = 1.170e-14;
            h0 = 800;
            H = 124.64;
        }

        double deltaH = (height - h0) / H;
        return p0*MathUtility.Exp(-deltaH);
    }
}
