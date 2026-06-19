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
        PenduiumState k2_m = new PenduiumState();
        k2_m.angle1 = y.angle1 +dt*0.5* k1.d_angle1;
        k2_m.angleVelocity1 = y.angleVelocity1 + dt * 0.5 * k1.d_angleVelocity1;
        k2_m.angle2 = y.angle2 + dt * 0.5 * k1.d_angle2;
        k2_m.angleVelocity2 = y.angleVelocity2 + dt * 0.5 * k1.d_angleVelocity2;

        PenduiumDerived k2 = Cal_DoublePendulum_AnaleAccel_theta(k2_m);
        PenduiumState k3_m = new PenduiumState();
        k3_m.angle1 = y.angle1 + dt * 0.5 * k2.d_angle1;
        k3_m.angleVelocity1 = y.angleVelocity1 + dt * 0.5 * k2.d_angleVelocity1;
        k3_m.angle2 = y.angle2 + dt * 0.5 * k2.d_angle2;
        k3_m.angleVelocity2 = y.angleVelocity2 + dt * 0.5 * k2.d_angleVelocity2;

        PenduiumDerived k3 = Cal_DoublePendulum_AnaleAccel_theta(k3_m);
        PenduiumState k4_m = new PenduiumState();
        k4_m.angle1 = y.angle1 + dt * k3.d_angle1;
        k4_m.angleVelocity1 = y.angleVelocity1 + dt *k3.d_angleVelocity1;
        k4_m.angle2 = y.angle2 + dt *k3.d_angle2;
        k4_m.angleVelocity2 = y.angleVelocity2 + dt * k3.d_angleVelocity2;

        PenduiumDerived k4 = Cal_DoublePendulum_AnaleAccel_theta(k4_m);
       
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
}
