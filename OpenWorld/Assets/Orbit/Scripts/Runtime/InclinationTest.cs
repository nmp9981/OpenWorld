using UnityEngine;
using System.IO;
using static IntegratorUtility;

public class InclinationTest : MonoBehaviour
{
    double T0;
    const double Arcsec2Rad = ConstUtility.Deg2Rad / 3600.0;
    const double JD0 = 2461041.5;   // 2026-01-01 0시 TT
    const double MuE = 398600.4418, MuSun = 1.32712440018e11, MuMoon = 4902.800066;

    bool UseMoon = true;
    bool UseSun = true;

    string path = "D:\\Math\\Data\\Memo.txt";

    // 위성 가속도: 질량중심은 관성계라 간접항 없음
    Vector3D AccelL(double t, Vector3D r)
    {
        double jd = JD0 + t / 86400.0;

        Vector3D earth = Gravity.TwoBody(r, MuE); 
        Vector3D j2= Perturbation.J2Accel(r, new Vector3D(0, 0, 1), MuE, 6378.137, 1.08262668e-3);

        Vector3D a = earth + j2;
        if (UseMoon) a += Perturbation.ThirdBodyAccel(r, MoonPos(jd), MuMoon);

        //세차행렬 -> 좌표변환
        double ζ = (2306.2181* T0 + 0.30188* T0*T0 +0.017998 *T0*T0*T0)*Arcsec2Rad;
        double z = (2306.2181 *T0 + 1.09468 * T0 * T0 + 0.018203 * T0 * T0 * T0) * Arcsec2Rad;
        double θ = (2004.3109* T0 - 0.42665 * T0 * T0 - 0.041833 * T0 * T0 * T0) * Arcsec2Rad;
        Matrix3x3D precession = Matrix3x3D.R3Frame(-z) * Matrix3x3D.R2Frame(θ) * Matrix3x3D.R3Frame(-ζ);
        if (UseSun) a += Perturbation.ThirdBodyAccel(r, precession*SunPos(jd), MuSun);

        return a;
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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        T0 = (JD0 - 2451545.0) / 36525.0;

        AccelFuncT accel = AccelL;
        // GEO위성
        Vector3D r = new Vector3D(42164, 0, 0);
        Vector3D v =new Vector3D(0, MathUtility.Sqrt(MuE / 42164), 0);
        double prevZ = 0;

        double dt = 300.0;
        int steps = (int)(86400 / dt);   // 1일
        double t = 0;

        CentralBody earth = new CentralBody(MuE, 6378.137, 1.08262668e-3);

        for (int i = 0; i < steps * 365; i++)
        {
            IntegratorUtility.RK4(ref r, ref v, t,dt, accel);
            t += dt;
           
            if (prevZ<0 && r.z>0)//승교점이 0이면 경사각만 출력
            {
                StateVector state = new StateVector(r, v);
                OrbitalElements element = OrbitConverter.ToElements(earth, state);

                double angle = element.i * ConstUtility.Rad2Deg;
                Debug.Log($"경사각 = {angle}");

                // 두 번째 인자(append)가 true면 이어쓰기, false면 덮어쓰기
                using (StreamWriter writer = new StreamWriter(path, append: true))
                {
                    writer.WriteLine(angle);
                }
            }
            prevZ = r.z;
        }
    }
}
