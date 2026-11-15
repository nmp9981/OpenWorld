using System.Collections.Generic;
using System.IO;
using System.Transactions;
using UnityEngine;
using static IntegratorUtility;

public class DragTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Example_O();
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

    void Example_O()
    {
        Debug.Log("J2 off / J2 on");
        Debug.Log($"경사각 0 : {Model_O(true, 0.0f,false)/Model_O(true, 0.0f,true)}");
        Debug.Log($"경사각 51.6 : {Model_O(true, 51.6f, false) / Model_O(true, 51.6f, true)}");
        Debug.Log($"경사각 90 : {Model_O(true, 90.0f, false) / Model_O(true, 90.0f, true)}");
    }

    double Model_O(bool isUseAtmosphereSpin, double i_angle, bool isJ2)
    {
        //대기 자전 ON
        Perturbation.UseAtmosphereSpin = isUseAtmosphereSpin;

        //경사각 단위
        i_angle = i_angle * ConstUtility.Deg2Rad;

        //초기 설계
        double Mu = 398600.4418;
        double B = 0.022;
        double R = 6378.1363;
        Vector3D r = new Vector3D(R+400, 0, 0);
        Vector3D v = MathUtility.Sqrt(Mu / (R+400))* new Vector3D(0,MathUtility.Cos(i_angle), MathUtility.Sin(i_angle));

        //가속도
        Vector3D K = new Vector3D(0, 0, 1);
        AccelFuncTV aTV = (t, r, v) => Gravity.TwoBody(r, Mu)+ Perturbation.DragAccel(r, v, B) + Perturbation.J2Accel(r, K, Mu, 6378.1363, 0.00108262668);
        if(!isJ2)
        {
            aTV = (t, r, v) => Gravity.TwoBody(r, Mu)+ Perturbation.DragAccel(r, v, B);
        }

        //적분 간격
        double dt = 2.5;
        int steps = (int)(86400 / dt);   // 1일
        double t = 0;

        //항력이 한일
        double energyW = 0;

        //에너지
        double J2 = 0.00108262668;
        double rMag = r.Magnitude();
        double J2Energy = isJ2?(Mu* J2* R*R / (2*rMag*rMag*rMag) * (3*r.z*r.z/ (rMag*rMag) - 1)):0;
        double initialEnergy = 0.5 * v.Magnitude() * v.Magnitude() - Mu / r.Magnitude()+J2Energy;
        double totalE = 0;

        //장반경
        double a0 = -Mu / (2 * initialEnergy);

        //일률
        double P0 = 0;
        double P1 = 0;

        //승교점 리스트
        List<(double, double)> RAANList = new List<(double, double)>();
        
        //이전 z좌표
        double prevZ = r.z;

        //고도합계
        double heightSum = 0;

        //적분
        for (int i = 0; i < steps * 7; i++)
        {
            P0 = Vector3D.Dot(Perturbation.DragAccel(r, v, B), v);

            IntegratorUtility.RK4(ref r, ref v, ref t, dt, aTV);

            P1 = Vector3D.Dot(Perturbation.DragAccel(r, v, B), v);

            energyW += 0.5 * (P0 + P1) * dt;

            heightSum += r.Magnitude() - R;

            if ((i + 1) % steps == 0)//하루 지남
            {
                rMag = r.Magnitude();
                J2Energy = isJ2 ? (Mu * J2 * R * R / (2 * rMag * rMag * rMag) * (3 * r.z * r.z / (rMag * rMag) - 1)) : 0;
                totalE = (Vector3D.Dot(v, v) * 0.5 - Mu / r.Magnitude())+J2Energy;
                double a = -Mu / (2 * totalE);
                Debug.Log($"a0 = {a0}, a = {a}, delta_a = {(a - a0)*1000}");
                Debug.Log($"고도 평균 {heightSum/steps}");
                if (i + 1 == steps)
                {
                    //return (a - a0) * 1000;
                }
                heightSum = 0;
            }
        }

        //해석식과 비교
        rMag = r.Magnitude();
        J2Energy = isJ2 ? (Mu * J2 * R * R / (2 * rMag * rMag * rMag) * (3 * r.z * r.z / (rMag * rMag) - 1)) : 0;
        double finalEnergy = 0.5 * v.Magnitude() * v.Magnitude() - Mu / r.Magnitude()+J2Energy;
        double deltaE = finalEnergy - initialEnergy;
        double rateEnergy = (deltaE - energyW) / deltaE;
        Debug.Log($"Initial Energy: {deltaE}, Final Energy: {energyW}, Rate of Energy Loss: {rateEnergy}");
        return 0;
    }
}
