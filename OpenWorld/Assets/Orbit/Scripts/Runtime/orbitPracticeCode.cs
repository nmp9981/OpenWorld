using Unity.VisualScripting;
using UnityEngine;
using static IntegratorUtility;

public class orbitPracticeCode : MonoBehaviour
{
    // ── 상수 (클래스 필드) ──
    const double Mu1 = 398600.4418;   // 지구
    const double Mu2 = 4902.800066;   // 달
    const double R12 = 384400.0;      // 지구–달 거리
    double pi2, pi1, omega;

    //L1, L4
    Vector3D L1, L4;

    private void Start()
    {
        TaskK_3();
    }

    void InitK()
    {
        pi2 = Mu2 / (Mu1 + Mu2);      // 0.01215
        pi1 = 1.0 - pi2;
        omega = MathUtility.Sqrt((Mu1 + Mu2) / (R12 * R12 * R12));

        // L1, L4 계산
        L4 = new Vector3D(R12*0.5-pi2*R12, R12 * MathUtility.Sqrt(3) * 0.5, 0);
        L1 = new Vector3D(FindL1(), 0, 0);//x축 위
    }

    // 위성 가속도: 질량중심은 관성계라 간접항 없음
    Vector3D AccelK(double t, Vector3D r)
    {
        Vector3D d1 = r - EarthPos(t);
        Vector3D d2 = r - MoonPos(t);
        double d1Mag = d1.Magnitude();
        double d2Mag = d2.Magnitude();
        return d1 * (-Mu1 / (d1Mag * d1Mag * d1Mag)) + d2 * (-Mu2 / (d2Mag * d2Mag * d2Mag));
    }

    Vector3D EarthPos(double t)
    {
        double theta = omega * t;
        return new Vector3D(-pi2 * R12 * MathUtility.Cos(theta), -pi2 * R12 * MathUtility.Sin(theta), 0);
    }

    Vector3D MoonPos(double t)
    {
        double theta = omega * t;
        return new Vector3D(pi1 * R12 * MathUtility.Cos(theta), pi1 * R12 * MathUtility.Sin(theta), 0);
    }
    double Jacobi(double t, Vector3D r, Vector3D v)
    {
        Vector3D omegaCrossR = new Vector3D(-omega * r.y, omega * r.x, 0);
        Vector3D vRot = v - omegaCrossR;
        double d1 = (r - EarthPos(t)).Magnitude();
        double d2 = (r - MoonPos(t)).Magnitude();
        return 0.5 * vRot.SqrMagnitude() - 0.5 * omega * omega * (r.x * r.x + r.y * r.y)
               - Mu1 / d1 - Mu2 / d2;
    }

    void TaskK()
    {
        InitK();
        AccelFuncT accel = AccelK;

        // 초기조건: 지구 중심에서 고도 400 km 원궤도 + 지구 자체의 관성 속도
        Vector3D earth0 = EarthPos(0);
        Vector3D earthVel0 = new Vector3D(-omega * earth0.y, omega * earth0.x, 0);
        Vector3D r = earth0 + new Vector3D(6778, 0, 0);
        Vector3D v = earthVel0 + new Vector3D(0, MathUtility.Sqrt(Mu1 / 6778), 0);

        double dt = 10.0;
        int steps = (int)(86400 / dt);   // 1일
        double c0 = Jacobi(0, r, v);
        double maxRel = 0;

        double rTol = 1e-10;
        double aTol = 1e-10;

        double t = 0, h = 10.0, tEnd = 86400;
        
        while (t < tEnd)
        {
            if (t + h > tEnd) h = tEnd - t;
            bool ok;
            double hused;

            IntegratorUtility.RK45(ref r, ref v, ref h, t, accel, rTol, aTol, out ok, out hused);

            if (ok)
            {
                t += hused;
                double c = Jacobi(t, r, v);
                double rel = MathUtility.Abs((c - c0) / c0);
                if (rel > maxRel) maxRel = rel;
            }
            else
            {
                Debug.Log($"적분 실패: t={t}, h={h}");
                break;
            }
        }
       
        Debug.Log($"C0 = {c0}, 최대 상대 변화 = {maxRel:E3}");
    }
    void TaskK_Verlet()
    {
        InitK();
        AccelFuncT accel = AccelK;

        // 초기조건: 지구 중심에서 고도 400 km 원궤도 + 지구 자체의 관성 속도
        Vector3D earth0 = EarthPos(0);
        Vector3D earthVel0 = new Vector3D(-omega * earth0.y, omega * earth0.x, 0);
        Vector3D r = earth0 + new Vector3D(6778, 0, 0);
        Vector3D v = earthVel0 + 1.1* new Vector3D(0, MathUtility.Sqrt(Mu1 / 6778), 0);

        double dt = 10.0;
        int steps = (int)(86400 / dt);   // 1일
        double c0 = Jacobi(0, r, v);
        double maxRel = 0;
        double t = 0;

        for (int i = 0; i < steps*6; i++)
        {
            IntegratorUtility.VelocityVerlet(ref r, ref v, dt, t, accel);
            t += dt;
            double c = Jacobi(t, r, v);
            double rel = MathUtility.Abs((c - c0) / c0);
            if (rel > maxRel) maxRel = rel;

            if((i+1)%steps == 0)
                Debug.Log($"C0 = {c0},{i}일에서 최대 상태 변화 = {maxRel:E3}");
        }
        Debug.Log($"C0 = {c0}, 최대 상대 변화 = {maxRel:E3}");
    }

    void TaskK_3()
    {
        InitK();
        AccelFuncT accel = AccelK;

        // 초기조건: 지구 중심에서 고도 400 km 원궤도 + 지구 자체의 관성 속도
        Vector3D earth0 = EarthPos(0);
        Vector3D earthVel0 = new Vector3D(-omega * earth0.y, omega * earth0.x, 0);
        Vector3D r = L1 + new Vector3D(1, 0, 0);
        Vector3D v = new Vector3D(-omega * r.y, omega * r.x, 0);   // v = Ω × r

        double dt = 10.0;
        int steps = (int)(86400 / dt);   // 1일
        double c0 = Jacobi(0, r, v);
        double maxRel = 0;

        double rTol = 1e-12;
        double aTol = 1e-12;

        double t = 0, h = 10.0, tEnd = 86400*730;
        double nextOut = 21600.0;   // 6시간마다 출력

        while (t < tEnd)
        {
            if (t + h > tEnd) h = tEnd - t;
            bool ok;
            double hused;

            IntegratorUtility.RK45(ref r, ref v, ref h, t, accel, rTol, aTol, out ok, out hused);

            if (ok)
            {
                t += hused;
                double c = Jacobi(t, r, v);
                double rel = MathUtility.Abs((c - c0) / c0);
                if (rel > maxRel) maxRel = rel;

                if (t >= nextOut)
                {
                    //L점까지의 거리
                    Vector3D rRo = ToRotating(t, r);
                    double dist = (rRo - L1).Magnitude();
                    nextOut += 21600.0;
                    Debug.Log($"{(int)t/86400}일 최대 취이 차이 = {dist:E3}");
                }
            }
            else
            {
                Debug.Log($"적분 실패: t={t}, h={h}");
                break;
            }
        }

        Debug.Log($"C0 = {c0}, 최대 상대 변화 = {maxRel:E3}");
    }

    ///3번 추가
    double LagrangeForce(double x)
    {
        double x1 = -pi2 * R12, x2 = pi1 * R12;
        double d1 = x - x1, d2 = x - x2;
        return omega * omega * x
               - Mu1 * d1 / (MathUtility.Abs(d1) * d1 * d1)
               - Mu2 * d2 / (MathUtility.Abs(d2) * d2 * d2);
    }

    double FindL1()
    {
        double lo = -pi2 * R12 + 1000.0;   // 지구 바로 옆
        double hi = pi1 * R12 - 1000.0;   // 달 바로 옆
        for (int k = 0; k < 100; k++)
        {
            double mid = 0.5 * (lo + hi);
            if (LagrangeForce(mid) < 0) lo = mid;
            else hi = mid;
        }
        return 0.5 * (lo + hi);
    }
    /// <summary>
    /// 관성계 좌표 r을 회전좌표로 변환
    /// </summary>
    /// <param name="t"></param>
    /// <param name="r"></param>
    /// <returns></returns>
    Vector3D ToRotating(double t, Vector3D r)
    {
        double c = MathUtility.Cos(omega * t), s = MathUtility.Sin(omega * t);
        return new Vector3D(c * r.x + s * r.y, -s * r.x + c * r.y, r.z);
    }
}
