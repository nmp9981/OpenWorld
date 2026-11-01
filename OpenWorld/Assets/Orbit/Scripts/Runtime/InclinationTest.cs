using UnityEngine;
using static IntegratorUtility;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class InclinationTest : MonoBehaviour
{
    double Mu = 398600.4418;   // 지구
    double Mu1 = 1.32712440018e11;   // 태양
    double Mu2 = 4902.800066;   // 달
    double omega = 2 * ConstUtility.PI / 86400.0;   // 지구 자전각속도

    Vector3D EarthPos(double t)
    {
        double theta = 2 * ConstUtility.PI * t / 86400.0;
        return new Vector3D(-149597870.7 * MathUtility.Cos(theta), -149597870.7 * MathUtility.Sin(theta), 0);
    }

    // 위성 가속도: 질량중심은 관성계라 간접항 없음
    Vector3D AccelK(double t, Vector3D r)
    {
        Vector3D earth = Gravity.TwoBody(r, Mu); 
        Vector3D j2= Perturbation.J2Accel(r, new Vector3D(0, 0, 1), Mu, 6378.137, 1.08262668e-3);

        //시작 JD를 넣는다.
        Vector3D sun = SunPos(t);
        Vector3D moon = MoonPos(t);

        return earth + j2 + sun + moon;
    }

    /// <summary>
    /// J번 태양
    /// </summary>
    /// <param name="jd"></param>
    Vector3D SunPos(double jd)
    {
        double AU = 149597870.7;

        double T = (jd - 2451545.0) / 36525.0;

        // 1. Table 8.1 Earth 행 (각도 변화율 단위는 "/Cy → /3600)
        double a = (1.00000011 - 0.00000005 * T) * AU;
        double e = 0.01671022 - 0.00003804 * T;
        double varpi = 102.94719 + (1198.28 / 3600.0) * T;        // 근일점 경도 ϖ
        double L = 100.46435 + (129597740.63 / 3600.0) * T;   // 평균 경도

        // 2. 평균근점이각 M = L − ϖ, 0~360 정규화
        double M = (L - varpi) % 360.0;
        if (M < 0) M += 360.0;

        //3. 진근점이각
        double E = Anomaly.MeanToEccentric(M * ConstUtility.Deg2Rad, e);
        double nu = Anomaly.EccentricToTrue(E, e);

        //4. 일심 지구위치
        double r = a * (1.0 - e * MathUtility.Cos(E));
        double lon = nu + varpi * ConstUtility.Deg2Rad;   // 황경
        double xEcl = r * MathUtility.Cos(lon);
        double yEcl = r * MathUtility.Sin(lon);
        double zEcl = 0.0;

        //5. 지심 변환
        xEcl = -xEcl;
        yEcl = -yEcl;
        zEcl = -zEcl;

        //6. 황도 -> 적도 회전
        double eps = 23.439291 * ConstUtility.Deg2Rad; // 적경
        double x = xEcl;
        double y = yEcl * MathUtility.Cos(eps) - zEcl * MathUtility.Sin(eps);
        double z = yEcl * MathUtility.Sin(eps) + zEcl * MathUtility.Cos(eps);

        return new Vector3D(x, y, z);
    }

    /// <summary>
    /// J번 달
    /// </summary>
    /// <param name="jd"></param>
    Vector3D MoonPos(double jd)
    {
        double T = (jd - 2451545.0) / 36525.0;

        double R = 6378.1363;

        double lambda = 218.32 + 481267.881 * T + 6.29 * MathUtility.Sin((135.0 + 477198.87 * T) * ConstUtility.Deg2Rad) - 1.27 * MathUtility.Sin((259.3 - 413335.36 * T) * ConstUtility.Deg2Rad)
            + 0.66 * MathUtility.Sin((235.7 + 890534.22 * T) * ConstUtility.Deg2Rad) + 0.21 * MathUtility.Sin((269.9 + 954397.74 * T) * ConstUtility.Deg2Rad)
            - 0.19 * MathUtility.Sin((357.5 + 35999.05 * T) * ConstUtility.Deg2Rad) - 0.11 * MathUtility.Sin((186.5 + 966404.03 * T) * ConstUtility.Deg2Rad);

        lambda = lambda % 360.0;
        if (lambda < 0) lambda += 360.0;

        double beta = 5.13 * MathUtility.Sin((93.3 + 483202.02 * T) * ConstUtility.Deg2Rad) + 0.28 * MathUtility.Sin((228.2 + 960400.89 * T) * ConstUtility.Deg2Rad)
            - 0.28 * MathUtility.Sin((318.3 + 6003.15 * T) * ConstUtility.Deg2Rad) - 0.17 * MathUtility.Sin((217.6 - 407332.21 * T) * ConstUtility.Deg2Rad);

        double P = 0.9508 + 0.0518 * MathUtility.Cos((135.0 + 477198.87 * T) * ConstUtility.Deg2Rad) + 0.0095 * MathUtility.Cos((259.3 - 413335.36 * T) * ConstUtility.Deg2Rad)
            + 0.0078 * MathUtility.Cos((235.7 + 890534.22 * T) * ConstUtility.Deg2Rad) + 0.0028 * MathUtility.Cos((269.9 + 954397.74 * T) * ConstUtility.Deg2Rad);

        double r = R / MathUtility.Sin(P * ConstUtility.Deg2Rad);

        //구면 황도->벡터
        lambda = lambda * ConstUtility.Deg2Rad;
        beta = beta * ConstUtility.Deg2Rad;
        double xEcl = r * MathUtility.Cos(lambda) * MathUtility.Cos(beta);
        double yEcl = r * MathUtility.Sin(lambda) * MathUtility.Cos(beta);
        double zEcl = r * MathUtility.Sin(beta);

        //황도 -> 적도
        double eps = (23.439291 - 0.0130043 * T) * ConstUtility.Deg2Rad; // 적경
        double x = xEcl;
        double y = yEcl * MathUtility.Cos(eps) - zEcl * MathUtility.Sin(eps);
        double z = yEcl * MathUtility.Sin(eps) + zEcl * MathUtility.Cos(eps);

        return new Vector3D(x, y, z);
    }


    /// <summary>
    /// 자코비 계산
    /// </summary>
    /// <param name="t"></param>
    /// <param name="r"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    double Jacobi(double t, Vector3D r, Vector3D v)
    {
        Vector3D omegaCrossR = new Vector3D(-omega * r.y, omega * r.x, 0);
        Vector3D vRot = v - omegaCrossR;
        double d1 = (r - EarthPos(t)).Magnitude();
        double d2 = (r - MoonPos(t)).Magnitude();
        return 0.5 * vRot.SqrMagnitude() - 0.5 * omega * omega * (r.x * r.x + r.y * r.y)
               - Mu1 / d1 - Mu2 / d2;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AccelFuncT accel = AccelK;
        // 초기조건: 지구 중심에서 고도 400 km 원궤도 + 지구 자체의 관성 속도
        Vector3D earth0 = Vector3D.Zero;
        Vector3D earthVel0 = new Vector3D(-omega * earth0.y, omega * earth0.x, 0);
        Vector3D r = earth0 + new Vector3D(6778, 0, 0);
        Vector3D v = earthVel0 + 1.1 * new Vector3D(0, MathUtility.Sqrt(Mu1 / 6778), 0);

        double dt = 10.0;
        int steps = (int)(86400 / dt);   // 1일
        double c0 = Jacobi(0, r, v);
        double maxRel = 0;
        double t = 0;

        CentralBody earth = new CentralBody(Mu, 6378.137, 1.08262668e-3);

        for (int i = 0; i < steps * 365; i++)
        {
            IntegratorUtility.VelocityVerlet(ref r, ref v, dt, t, accel);
            t += dt;
            double c = Jacobi(t, r, v);
            double rel = MathUtility.Abs((c - c0) / c0);
            if (rel > maxRel) maxRel = rel;

            StateVector state = new StateVector(r, v);
            OrbitalElements element = OrbitConverter.ToElements(earth, state);

            if (element.raan==0)//승교점이 0이면 경사각만 출력
            {
                Debug.Log($"경사각 = {element.i}");
            }
        }
    }
}
