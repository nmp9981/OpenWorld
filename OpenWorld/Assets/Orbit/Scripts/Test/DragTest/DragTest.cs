using System.IO;
using System.Transactions;
using UnityEngine;
using static IntegratorUtility;

public class DragTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Example_N();
    }

    void Example_M()
    {
        Perturbation.UseAtmosphereSpin = false;
        double B = 0.022;
        Vector3D accel =  Perturbation.DragAccel(new Vector3D(0,0,6778.1363), new Vector3D(7.669, 0,0),B);
        Debug.Log($"Drag Accel: {accel.Magnitude()}");
        Debug.Log(accel.x);
    }

    void Example_N()
    {
        //대기 자전 OFF
        Perturbation.UseAtmosphereSpin = false;
        
        double Mu = 398600.4418;
        double B = 0.022;
        
        // 초기 설계
        Vector3D r = new Vector3D(6778.1363, 0, 0);
        Vector3D v = new Vector3D(0, MathUtility.Sqrt(Mu / 6778.1363), 0);

        //가속도
        AccelFuncTV aTV = (t, r, v) => Gravity.TwoBody(r, Mu) + Perturbation.DragAccel(r, v, B);

        //적분 간격
        double dt = 5.0;
        int steps = (int)(86400 / dt);   // 1일
        double t = 0;

        //항력이 한일
        double energyW = 0;

        //에너지
        double initialEnergy = 0.5 * v.Magnitude() * v.Magnitude() - Mu / r.Magnitude();
        double totalE = 0;

        //장반경
        double a0 = -Mu / (2 * initialEnergy);

        //일률
        double P0 = 0;
        double P1 = 0;

        //적분
        for (int i = 0; i < steps * 10; i++)
        {
            P0 = Vector3D.Dot(Perturbation.DragAccel(r, v, B), v);

            IntegratorUtility.RK4(ref r, ref v, ref t, dt, aTV);
            
            P1= Vector3D.Dot(Perturbation.DragAccel(r, v, B), v);

            energyW += 0.5 * (P0 + P1) * dt;

            if((i+1)%steps == 0)//하루 지남
            {
                totalE = (Vector3D.Dot(v, v) * 0.5 - Mu / r.Magnitude());
                double a = -Mu / (2 * totalE);
                Debug.Log($"a0 = {a0}, a = {a}, delta_a = {a - a0}");

                if (i+1 == steps)
                {
                    Debug.Log($"{(a-a0)*1000} 368m와 비교");
                }
            }

        }
       
        //해석식과 비교
        double finalEnergy = 0.5 * v.Magnitude() * v.Magnitude() - Mu / r.Magnitude();
        double deltaE = finalEnergy - initialEnergy;
        double rateEnergy = (deltaE - energyW)/deltaE;
        Debug.Log($"Initial Energy: {deltaE}, Final Energy: {energyW}, Rate of Energy Loss: {rateEnergy}");
    }
}
