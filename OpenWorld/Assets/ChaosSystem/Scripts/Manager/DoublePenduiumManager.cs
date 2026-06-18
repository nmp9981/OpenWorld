using Unity.Collections;
using UnityEngine;

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
    public double RK4_theta1(double y, double dt)
    {
        double k1 = Cal_DoublePendulum_AnaleAccel_theta1(y);
        double k2 = Cal_DoublePendulum_AnaleAccel_theta1(y + dt * 0.5 * k1);
        double k3 = Cal_DoublePendulum_AnaleAccel_theta1(y + dt * 0.5 * k2);
        double k4 = Cal_DoublePendulum_AnaleAccel_theta1(y + dt * k3);
        double yNext = y + (dt / 6) * (k1 + 2 * k2 + 2 * k3 + k4);

        return yNext;
    }
    /// <summary>
    /// RK4 적분
    /// </summary>
    /// <param name="y"></param>
    /// <param name="dt"></param>
    public double RK4_theta2(double y, double dt)
    {
        double k1 = Cal_DoublePendulum_AnaleAccel_theta2(y);
        double k2 = Cal_DoublePendulum_AnaleAccel_theta2(y + dt * 0.5 * k1);
        double k3 = Cal_DoublePendulum_AnaleAccel_theta2(y + dt * 0.5 * k2);
        double k4 = Cal_DoublePendulum_AnaleAccel_theta2(y + dt * k3);
        double yNext = y + (dt / 6) * (k1 + 2 * k2 + 2 * k3 + k4);

        return yNext;
    }

    /// <summary>
    /// RK4 계산 함수 F
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public double Cal_DoublePendulum_AnaleAccel_theta1(double y)
    {
        return PhysicsFormula.DoublePendulum_AnaleAccel1(l1,l2,m1,m2,theta1,theta2,w1,w2);
    }
    /// <summary>
    /// RK4 계산 함수 F
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public double Cal_DoublePendulum_AnaleAccel_theta2(double y)
    {
        return PhysicsFormula.DoublePendulum_AnaleAccel2(l1, l2, m1, m2, theta1, theta2, w1, w2);
    }
}
