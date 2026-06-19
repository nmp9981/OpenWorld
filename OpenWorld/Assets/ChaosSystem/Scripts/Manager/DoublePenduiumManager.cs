using Unity.Collections;
using UnityEngine;

//진자 상태
public struct PenduiumState
{
    public double angle1;
    public double angleVelocity1;
    public double angle2;
    public double angleVelocity2;
}
//진자 도함수
public struct PenduiumDerived
{
    public double d_angle1;
    public double d_angleVelocity1;
    public double d_angle2;
    public double d_angleVelocity2;
}

public class DoublePenduiumManager : MonoBehaviour
{
    double l1, l2;
    double m1, m2;
    double theta1, theta2;
    double w1, w2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
    }


    /// <summary>
    /// RK4 적분
    /// </summary>
    /// <param name="y"></param>
    /// <param name="dt"></param>
    public PenduiumState RK4_theta(PenduiumState y, double dt)
    {
        PenduiumDerived k1 = Cal_DoublePendulum_AnaleAccel_theta(y);
        PenduiumDerived k2 = Cal_DoublePendulum_AnaleAccel_theta(AddScaled(y, k1, dt));   
        PenduiumDerived k3 = Cal_DoublePendulum_AnaleAccel_theta(AddScaled(y, k2, dt));
        PenduiumDerived k4 = Cal_DoublePendulum_AnaleAccel_theta(AddScaled(y, k3, dt));
       
        PenduiumState yNext = new PenduiumState();
        yNext.angle1 = y.angle1 + (dt / 6) * (k1.d_angle1 + 2 * k2.d_angle1 + 2 * k3.d_angle1 + k4.d_angle1);
        yNext.angleVelocity1 = y.angleVelocity1 + (dt / 6) * (k1.d_angleVelocity1 + 2 * k2.d_angleVelocity1 + 2 * k3.d_angleVelocity1 + k4.d_angleVelocity1);
        yNext.angle2 = y.angle2 + (dt / 6) * (k1.d_angle2 + 2 * k2.d_angle2 + 2 * k3.d_angle2 + k4.d_angle2);
        yNext.angleVelocity2 = y.angleVelocity2 + (dt / 6) * (k1.d_angleVelocity2 + 2 * k2.d_angleVelocity2 + 2 * k3.d_angleVelocity2 + k4.d_angleVelocity2);

        return yNext;
    }

    /// <summary>
    /// RK4 계산 함수 F
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public PenduiumDerived Cal_DoublePendulum_AnaleAccel_theta(PenduiumState y)
    {
        return PhysicsFormula.DoublePendulum_AnaleAccel(l1,l2,m1,m2,y);
    }

    /// <summary>
    /// 헬퍼 함수
    /// 상태 + 도함수 * h → 새 상태
    /// </summary>
    /// <param name="y"></param>
    /// <param name="k"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    PenduiumState AddScaled(PenduiumState y, PenduiumDerived k, double dt)
    {
        // 4성분 각각: y.angleX + dt * k.d_angleX
        y.angle1 = y.angle1 + dt * 0.5 * k.d_angle1;
        y.angleVelocity1 = y.angleVelocity1 + dt * 0.5 * k.d_angleVelocity1;
        y.angle2 = y.angle2 + dt * 0.5 * k.d_angle2;
        y.angleVelocity2 = y.angleVelocity2 + dt * 0.5 * k.d_angleVelocity2;

        return y;
    }
}
