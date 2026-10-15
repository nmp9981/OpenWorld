using NUnit.Framework;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.VFX;
using static IntegratorUtility;
using static UnityEditor.PlayerSettings;

public class orbutTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TaskF();
    }

    /// <summary>
    /// 왕복 테스트
    /// </summary>
    void Connect_State_Element()
    {
        StateVector state0 = new StateVector(new Vector3D(-6045.0, -3490.0, 2500.0),
            new Vector3D(-3.457, 6.618, 2.533));

        OrbitalElements orbit = StateVector_To_OrbitElement(state0, 398600.4418);

        OrbitalElements newOrbit = new OrbitalElements
        {
            p = 16056.11967,
            e = 1.4,
            i = 30.0 * ConstUtility.Deg2Rad,
            raan = 40.0 * ConstUtility.Deg2Rad,
            argp = 60.0 * ConstUtility.Deg2Rad,
            nu = 30.0 * ConstUtility.Deg2Rad,
            Geometry = OrbitGeometry.General
        };
        StateVector state1 = OrbitElemental_To_StateVector(newOrbit, 398600.4418);
    }

    /// <summary>
    /// 적분기 연결
    /// </summary>
    void ConnectIntegralTest()
    {
        double Mu = 1.0;
        AccelFunc twoBody = r => Gravity.TwoBody(r, Mu);

        var r0 = new Vector3D(1, 0, 0);
        var v0 = new Vector3D(0, 1, 0);
        double E0 = 0.5 * v0.SqrMagnitude() - Mu / r0.Magnitude();

        int N = 100;
        double T = 2 * ConstUtility.PI;
        double dt = T / N;
        double maxDiff = 0;

        for (int s = 1; s <= 10 * N; s++)
        {
            VelocityVerlet(ref r0, ref v0, dt, twoBody);
            double energy = 0.5 * v0.SqrMagnitude() - Mu / r0.Magnitude();
            double diff = energy - E0;
            maxDiff = MathUtility.Max(maxDiff, MathUtility.Abs(diff));

            if (s % N == 0)
            {
                Debug.Log($"Energy : {maxDiff:E12}");
                maxDiff = 0;
            }
        }
    }
    /// <summary>
    /// 상태벡터 -> 궤도요소
    /// </summary>
    OrbitalElements StateVector_To_OrbitElement(StateVector state, double mu)
    {
        double r = state.Position.Magnitude();
        double v = state.Velocity.Magnitude();

        double vr = Vector3D.Dot(state.Position, state.Velocity) / r;

        Vector3D hVec = Vector3D.Cross(state.Position, state.Velocity);
        double h = hVec.Magnitude();

        double incl_Angle = MathUtility.ArkCos(hVec.z / h);

        Vector3D nVec = Vector3D.Cross(Vector3D.Forward, hVec);
        double n = nVec.Magnitude();

        double RAAN = MathUtility.ArkCos(nVec.x / n);
        if (nVec.y>=0) RAAN = MathUtility.ArkCos(nVec.x / n);
        else RAAN = 2 * ConstUtility.PI - MathUtility.ArkCos(nVec.x / n);

        Vector3D eVec = ((state.Position * (v * v - mu / r)) - (state.Velocity *r* vr)) / mu;
        double e = eVec.Magnitude();

        double w = MathUtility.ArkCos(Vector3D.Dot(nVec, eVec) / (n * e));
        if (eVec.z < 0) w = 2 * ConstUtility.PI - w;

        double trueAnomaly = MathUtility.ArkCos(Vector3D.Dot(eVec, state.Position) / (e * r));
        if (vr < 0) trueAnomaly = 2 * ConstUtility.PI - trueAnomaly;

        OrbitalElements newOb = new OrbitalElements
        {
            p = h * h / mu,
            e = e,
            i = incl_Angle,
            raan = RAAN,
            argp = w,
            nu = trueAnomaly,
            Geometry = OrbitGeometry.General
        };
        return newOb;
    }
    /// <summary>
    /// 궤도요소 -> 상태벡터
    /// </summary>
    /// <param name="orbit"></param>
    /// <returns></returns>
    StateVector OrbitElemental_To_StateVector(OrbitalElements orbit, double mu)
    {
        double r = orbit.p / (1 + orbit.e * MathUtility.Cos(orbit.nu));
        Vector3D r_x = new Vector3D(r*MathUtility.Cos(orbit.nu), r*MathUtility.Sin(orbit.nu), 0);

        double pp = MathUtility.Sqrt(1 / orbit.p * mu);
        Vector3D v_x =pp* new Vector3D(-MathUtility.Sin(orbit.nu), orbit.e+MathUtility.Cos(orbit.nu),0);

        Matrix3x3D Qxx = Matrix3x3D.R3(orbit.raan) * Matrix3x3D.R1(orbit.i) * Matrix3x3D.R3(orbit.argp);

        Vector3D rx = Qxx*r_x;
        Vector3D vx = Qxx * v_x;

        StateVector newState = new StateVector(rx, vx);
        return newState;
    }

    /// <summary>
    /// 예제 4-9
    /// </summary>
    void Example4_9()
    {
        double mu = 398600.4418;
        StateVector state0 = new StateVector(new Vector3D(-3670.0, -3870.0, 4400.0),
            new Vector3D(4.7, -7.4, 1.0));
        OrbitalElements elements = StateVector_To_OrbitElement(state0, mu);

        Debug.Log($"p : {elements.p}");//h=58927 km^2/s
        Debug.Log($"e : {elements.e}");
        Debug.Log($"i : {elements.i * ConstUtility.Rad2Deg}");
        Debug.Log($"raan : {elements.raan * ConstUtility.Rad2Deg}");
        Debug.Log($"argp : {elements.argp * ConstUtility.Rad2Deg}");
        Debug.Log($"nu : {elements.nu * ConstUtility.Rad2Deg}");

        //장반경
        double a = elements.p / (1 - elements.e * elements.e);
        Debug.Log($"a : {a}");
        //주기
        double T = 2 * ConstUtility.PI * MathUtility.Sqrt(a * a * a / mu);
        Debug.Log($"T : {T}");
        //초당 각도
        double meanMotion = 2 * ConstUtility.PI / T;
        Debug.Log($"meanMotion : {meanMotion}");
        //E0
        double E0 = Anomaly.TrueToEccentric(elements.nu, elements.e);
        Debug.Log($"E0 : {E0}");
        //t1
        double t1 = (E0 - elements.e * MathUtility.Sin(E0)) / meanMotion;
        Debug.Log($"t1 : {t1}");
        //tf
        double tf = t1 + 345600;
        Debug.Log($"tf : {tf}");
        //첫 궤도np
        double np = tf / T;
        double t32 = (np-MathUtility.Floor(np)) * T;
        double M32 = meanMotion * t32;
        Debug.Log($"M32 : {M32}");
        double E32 = Anomaly.MeanToEccentric(M32, elements.e)+2*ConstUtility.PI;
        Debug.Log($"E32 : {E32}");
        
        double nu32 = Anomaly.EccentricToTrue(E32, elements.e);
        Debug.Log($"nu32 : {nu32}");

        //위성
        double r = elements.p / (1 + elements.e * MathUtility.Cos(nu32));
        Vector3D rx =new Vector3D( r * MathUtility.Cos(nu32),r*MathUtility.Sin(nu32),0);
        Vector3D vx = new Vector3D(-MathUtility.Sqrt(mu/elements.p) * MathUtility.Sin(nu32), MathUtility.Sqrt(mu / elements.p) * (elements.e + MathUtility.Cos(nu32)), 0);
        Debug.Log($"rx {rx.x} {rx.y} {rx.z}");
        Debug.Log($"vx {vx.x} {vx.y} {vx.z}");

        //승교점 적경 변화량
        double deltaRAAN = -1.5*MathUtility.Cos(elements.i)*(MathUtility.Sqrt(mu)*0.00108263*6378*6378)
            /((1-elements.e*elements.e)*(1 - elements.e * elements.e)*MathUtility.Pow(a,3.5));
        Debug.Log($"deltaRAAN : {deltaRAAN}");
        double RAAN = (elements.raan + deltaRAAN * 345600);
        Debug.Log($"RAAN : {RAAN}");
        //근지점편각
        double deltaArgp = 0.75 * (4 - 5 * MathUtility.Sin(elements.i) * MathUtility.Sin(elements.i)) *
            (MathUtility.Sqrt(mu) * 0.00108263 * 6378 * 6378) / ((1 - elements.e * elements.e) * (1 - elements.e * elements.e) * MathUtility.Pow(a, 3.5));
        Debug.Log($"deltaArgp : {deltaArgp}");
        double Argp = (elements.argp + deltaArgp * 345600);
        Debug.Log($"Argp : {Argp}");
        Matrix3x3D Qxx = Matrix3x3D.R3Frame(Argp) * Matrix3x3D.R1Frame(elements.i) * Matrix3x3D.R3Frame(RAAN);
        Matrix3x3D QxxT = Qxx.Transpose();

        Vector3D rxNew = QxxT * rx;
        Vector3D vxNew = QxxT * vx;

        Debug.Log($"rxNew : {rxNew.x} {rxNew.y} {rxNew.z}");
        Debug.Log($"vxNew : {vxNew.x} {vxNew.y} {vxNew.z}");

        Example4_92(rxNew, vxNew);
    }
    /// <summary>
    /// 예제 4-9
    /// </summary>
    void Example4_92(Vector3D rx, Vector3D vx)
    {
        double rTol = 1e-10, aTol = 1e-10;
        double Mu = 398600.4418;
        Vector3D K = new Vector3D(0, 0, 1);
        AccelFunc twoBody = r => Gravity.TwoBody(r, Mu) + Perturbation.J2Accel(r, K, Mu, 6378.1363, 0.00108262668);

        StateVector state0 = new StateVector(new Vector3D(-3670.0, -3870.0, 4400.0),
            new Vector3D(4.7, -7.4, 1.0));
        var r0 = state0.Position;
        var v0 = state0.Velocity;
        
        double dt = 0.05;
        double tEnd = 345600;//96시간
        int step = 0;

        double t = 0;
        
        while (t < tEnd)
        {
            if (t + dt > tEnd) dt = tEnd - t;
            double hUsed = dt;
            RK45(ref r0, ref v0,ref dt, twoBody,rTol, aTol, out bool ok);

            if(ok)
            {
                t += hUsed;
                step++;
            }
        }
        Debug.Log($"step {step}");
        Debug.Log($"rxNew 차이 : {r0.x-rx.x} {r0.y-rx.y} {r0.z - rx.z}");
        Debug.Log($"vxNew 차이 : {v0.x-vx.x} {v0.y-vx.y} {v0.z-vx.z}");

        //RSW
        Vector3D rHat = r0.Normalized();
        Vector3D hVec = Vector3D.Cross(r0, v0);
        Vector3D wHat = hVec.Normalized();
        Vector3D sHat = Vector3D.Cross(wHat, rHat);

        double deltaR = Vector3D.Dot(r0-rx, rHat);
        double deltaS = Vector3D.Dot(r0 - rx, sHat);
        double deltaW = Vector3D.Dot(r0 - rx,wHat);
        double rsw = MathUtility.Sqrt(deltaR*deltaR + deltaS*deltaS+deltaW*deltaW);
        Debug.Log($"RSW 값 {deltaR} {deltaS} {deltaW}");
        Debug.Log($"RSW {rsw} {(r0 - rx).Magnitude()}");
    }
    /// <summary>
    /// 과제 F
    /// </summary>
    void TaskF()
    {
        //해석식
        double i = 51.6 *ConstUtility.Deg2Rad;double e = 0;double a = 6778;double Mu = 398600.4418;
        double deltaRAAN = -1.5 * MathUtility.Cos(i) * (MathUtility.Sqrt(Mu) * 0.00108263 * 6378.1363 * 6378.1363)
            / ((1 - e*e) * (1 - e*e) * MathUtility.Pow(a, 3.5));
        Debug.Log($"deltaRAAN : {deltaRAAN}");

        //적분기
        double rTol = 1e-10, aTol = 1e-10;
        Vector3D K = new Vector3D(0, 0, 1);
        AccelFunc twoBody = r => Gravity.TwoBody(r, Mu) + Perturbation.J2Accel(r, K, Mu, 6378.1363, 0.00108262668);

        OrbitalElements newElements = new OrbitalElements()
        {
            p = 6778,
            e = 0,
            i = 51.6 * ConstUtility.Deg2Rad,
            raan = 0,
            argp = 0,
            nu = 0,
            Geometry = OrbitGeometry.General
        };
        StateVector state0 = OrbitElemental_To_StateVector(newElements,Mu);
        var r0 = state0.Position;
        var v0 = state0.Velocity;

        double dt = 0.05;
        double tEnd = 345600;//96시간
        int step = 0;
        double stepRAAN = 0;

        double t = 0;
        double avgDeltaRAAN = 0;
        double preRAAN = 0;
        double prevZ = r0.z;
        double prevTime = 0;

        List<(double,double)> RAANList = new List<(double,double)>();

        while (t < tEnd)
        {
            if (t + dt > tEnd) dt = tEnd - t;
            double hUsed = dt;
            RK45(ref r0, ref v0, ref dt, twoBody, rTol, aTol, out bool ok);

            if (ok)
            {
                double vr = Vector3D.Dot(r0,v0) / r0.Magnitude();

                Vector3D hVec = Vector3D.Cross(r0, v0);
                double h = hVec.Magnitude();

                double incl_Angle = MathUtility.ArkCos(hVec.z / h);

                Vector3D nVec = Vector3D.Cross(Vector3D.Forward, hVec);
                double n = nVec.Magnitude();

                double RAAN = MathUtility.ArkCos(nVec.x / n);
                if (nVec.y >= 0) RAAN = MathUtility.ArkCos(nVec.x / n);
                else RAAN = 2 * ConstUtility.PI - MathUtility.ArkCos(nVec.x / n);

                if (r0.z >= 0 && prevZ<0)
                {
                    RAANList.Add((t, RAAN));
                }
                preRAAN = RAAN;
                prevZ = r0.z;
                prevTime = t;

                t += hUsed;
                step++;
            }
        }
        for (int k = 1; k < RAANList.Count; k++)
        {
            Debug.Log(k + "번째 RAAN : " + RAANList[k].Item2);
            double delta = RAANList[k].Item2 - RAANList[k - 1].Item2;
            avgDeltaRAAN += delta;
            stepRAAN+= (RAANList[k].Item1 - RAANList[k - 1].Item1);
        }

        Debug.Log($"최종 평균 deltaRAAN {avgDeltaRAAN/stepRAAN}");
    }
}
