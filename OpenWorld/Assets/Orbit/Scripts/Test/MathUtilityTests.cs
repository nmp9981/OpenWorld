using System.IO;
using System.Text;
using UnityEngine;

public class MathUtilityTests : MonoBehaviour
{
    public enum Method { ExplicitEuler, SymplecticEuler, VelocityVerlet, RK4 }

    [Header("설정")]
    public Method method = Method.VelocityVerlet;
    [Range(0f, 0.9f)] public float eccentricity = 0.3f;
    public double dt = 2.0 * ConstUtility.PI / 2000.0;   // 주기당 2000스텝
    public int stepsPerFixedUpdate = 10;
    public float renderScale = 5f;

    [Header("관측값 (읽기 전용)")]
    public double elapsedOrbits;
    public double energyErrorNow, energyErrorMax;
    public double posErrorNow, posErrorMax;      // ← 새로 추가: 해석해 대비
    public double velErrorNow;

    Vector3D pos, vel;
    double E0;
    long stepCount;
    Orbit orbit;

    //파일 작성용
    CentralBody body;
    StreamWriter writer;
    

    // ── 시스템 정의: μ=1 케플러. 유틸리티에 델리게이트로 전달 ──
    static Vector3D Gravity(Vector3D r)
    {
        double d = r.Magnitude();
        return r * (-1.0 / (d * d * d));
    }

    static double Energy(Vector3D r, Vector3D v)
        => 0.5 * v.SqrMagnitude() - 1.0 / r.Magnitude();

    void Start()
    {
        double e = eccentricity;
        
        body = new CentralBody (1.0,1.0);     // 실제 생성 방식에 맞게
        var oe = new OrbitalElements { p = 1.0 - e * e, e = e, i = 0, raan = 0, argp = 0, nu = 0 };
        orbit = new Orbit(body, oe, epoch: 0.0);

        // 적분기 초기값을 손계산이 아니라 같은 Orbit에서 뽑음
        var s0 = orbit.StateAt(0.0);
        pos = s0.Position; vel = s0.Velocity;

        // 사전 점검: 손계산 값과 1e-15 수준에서 같아야 함
        Debug.Log($"r0={pos.x}, {pos.y}, {pos.z}   expected=({1.0 - e}, 0, 0)");
        Debug.Log($"v0={vel.x}, {vel.y}, {vel.z}   expected=(0, {MathUtility.Sqrt((1.0 + e) / (1.0 - e))}, 0)");

        E0 = Energy(pos, vel);
        energyErrorMax = 0;
        posErrorMax = 0;
        stepCount = 0;

        // 헤더: orbit, 그다음 방법별 posErr, velErr, energyErr, a, e
        string path = Path.Combine(Application.persistentDataPath, $"{method}_e{eccentricity}.csv");
        writer = new StreamWriter(path);
        writer.WriteLine("orbit,posErr,velErr,energyErr,a,e");
        Debug.Log(path);
    }

    void FixedUpdate()
    {
        if (writer == null) return;

        for (int i = 0; i < stepsPerFixedUpdate; i++)
        {
            switch (method)
            {
                case Method.ExplicitEuler: IntegratorUtility.ExplicitEuler(ref pos, ref vel, dt, Gravity); break;
                case Method.SymplecticEuler: IntegratorUtility.SymplecticEuler(ref pos, ref vel, dt, Gravity); break;
                case Method.VelocityVerlet: IntegratorUtility.VelocityVerlet(ref pos, ref vel, dt, Gravity); break;
                case Method.RK4: IntegratorUtility.RK4(ref pos, ref vel, dt, Gravity); break;
            }
            stepCount++;

            // 주기 경계에 정확히 도달한 스텝에서만 기록 (stepsPerOrbit의 배수)
            if (stepCount % 2000 == 0)          // 주기당 2000스텝 기준. dt 바꾸면 같이 수정
            {
                double t = stepCount * dt;
                var ex = orbit.StateAt(t);
                var el = OrbitConverter.ToElements(body, new StateVector(pos, vel));
                double a = el.p / (1.0 - el.e * el.e);

                writer.WriteLine($"{stepCount / 2000},{(pos - ex.Position).Magnitude()},{(vel - ex.Velocity).Magnitude()}," +
                                 $"{(Energy(pos, vel) - E0) / MathUtility.Abs(E0)},{a},{el.e}");
                writer.Flush();
            }
        }
        
        double simT = stepCount * dt;
        var exact = orbit.StateAt(simT);

        elapsedOrbits = simT / orbit.Period;
        energyErrorNow = (Energy(pos, vel) - E0) / MathUtility.Abs(E0);
        energyErrorMax = MathUtility.Max(energyErrorMax, MathUtility.Abs(energyErrorNow));
        posErrorNow = (pos - exact.Position).Magnitude();
        velErrorNow = (vel - exact.Velocity).Magnitude();
        posErrorMax = MathUtility.Max(posErrorMax, posErrorNow);

        transform.position = new Vector3((float)pos.x, (float)pos.y, (float)pos.z) * renderScale;
    }

    void OnDestroy() => writer?.Close();
}
