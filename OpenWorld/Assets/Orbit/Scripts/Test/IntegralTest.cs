using MathD.Integration;
using System.Text;
using System;
using UnityEngine;
using System.IO;

public class IntegralTest : MonoBehaviour
{
    // ── 문제 정의: 2체 케플러 ──
    static double Mu;
   
    // ═══════════════════════════════════════════════════════════════
    //  가장 단순한 적분기 테스트
    //
    //  아이디어: 케플러 해석해 없이도 참값을 얻을 수 있다.
    //    - μ=1, a=1 로 놓으면 주기 T = 2π (정준 단위)
    //    - 궤도는 닫혀 있으므로, 정확히 1주기 적분하면
    //      "출발점으로 돌아와야" 한다.
    //    - 따라서 |끝위치 - 시작위치| 가 그대로 전역 오차다.
    //
    //  측정 두 가지:
    //    (1) 수렴 차수: dt를 반으로 줄일 때 오차가 2^차수 배로 줄어드는가
    //    (2) 에너지: 100주기 동안 드리프트인가, 진동인가
    // ═══════════════════════════════════════════════════════════════

    /// <summary>중심천체(원점, μ=1) 주위를 도는 물체 1개. 가장 단순한 케플러 시스템.</summary>
    public sealed class KeplerSystem : ISeparableSystem, IConservativeSystem
    {
        public int BodyCount => 1;

        public void Acceleration(double t, Vector3D[] pos, Vector3D[] accelOut)
        {
            double r = pos[0].Magnitude();
            accelOut[0] = pos[0] * (-1.0 / (r * r * r));   // a = -μ r / |r|³,  μ=1
        }

        public double KineticEnergy(Vector3D[] vel) => 0.5 * vel[0].SqrMagnitude();
        public double PotentialEnergy(Vector3D[] pos) => -1.0 / pos[0].Magnitude();
        public Vector3D LinearMomentum(Vector3D[] vel) => vel[0];
        public Vector3D AngularMomentum(Vector3D[] pos, Vector3D[] vel) => Vector3D.Cross(pos[0], vel[0]);
    }

    public static class SimpleIntegratorTest
    {
        static readonly double T = 2.0 * Math.PI;   // a=1, μ=1 → 주기 2π

        // 이심률 e인 궤도의 초기 조건: 근점에서 출발
        //   r = a(1-e) = 1-e,  v = sqrt(μ(1+e)/(a(1-e))) (접선 방향)
        static void InitialState(double e, out Vector3D pos, out Vector3D vel)
        {
            pos = new Vector3D(1.0 - e, 0, 0);
            vel = new Vector3D(0, Math.Sqrt((1.0 + e) / (1.0 - e)), 0);
        }

        public static void Main()
        {
            var sys = new KeplerSystem();
            var integrators = new ISymplecticIntegrator[]
            {
            new ExplicitEuler(), new SymplecticEuler(), new VelocityVerlet(), new RungeKutta4()
            };

            Console.WriteLine("─── 테스트 1: 수렴 차수 (1주기 후 출발점과의 거리) ───");
            Console.WriteLine("dt 반감 → 오차비의 log2 = 관측 차수. Order 속성과 맞으면 통과.\n");

            foreach (var ig in integrators)
            {
                Console.WriteLine($"[{ig.GetType().Name}]  이론 차수 = {ig.Order}");
                double prevErr = 0;
                foreach (int steps in new[] { 500, 1000, 2000, 4000, 8000 })
                {
                    double err = ReturnError(ig, sys, e: 0.3, orbits: 1, stepsPerOrbit: steps);
                    string order = prevErr == 0 ? "  -  " : Math.Log(prevErr / err, 2).ToString("F2");
                    Console.WriteLine($"  T/{steps,-5}  오차 = {err:E2}   관측 차수 = {order}");
                    prevErr = err;
                }
                Console.WriteLine();
            }

            Console.WriteLine("─── 테스트 2: 에너지 거동 (100주기, e=0.3) ───");
            Console.WriteLine("symplectic → 진동(최대치가 안 자람) / 비symplectic → 한 방향 드리프트\n");

            foreach (var ig in integrators)
                EnergyBehavior(ig, sys, e: 0.3, orbits: 100, stepsPerOrbit: 2000);
        }

        /// <summary>orbits 주기 적분 후 출발점과의 거리 = 전역 위치 오차</summary>
        static double ReturnError(ISymplecticIntegrator ig, KeplerSystem sys,
                                  double e, int orbits, int stepsPerOrbit)
        {
            InitialState(e, out var p0, out var v0);
            var pos = new[] { p0 };
            var vel = new[] { v0 };

            ig.Prepare(1);
            double dt = T / stepsPerOrbit;
            long n = (long)stepsPerOrbit * orbits;
            for (long i = 0; i < n; i++)
                ig.Step(sys, i * dt, dt, pos, vel);

            return (pos[0] - p0).Magnitude();
        }

        /// <summary>10주기마다 상대 에너지 오차를 출력 — 커지는지, 왔다갔다 하는지 눈으로 확인</summary>
        static void EnergyBehavior(ISymplecticIntegrator ig, KeplerSystem sys,
                                   double e, int orbits, int stepsPerOrbit)
        {
            InitialState(e, out var p0, out var v0);
            var pos = new[] { p0 };
            var vel = new[] { v0 };

            ig.Prepare(1);
            double dt = T / stepsPerOrbit;
            double E0 = sys.KineticEnergy(vel) + sys.PotentialEnergy(pos);

            Console.Write($"[{ig.GetType().Name,-16}] ΔE/E: ");
            for (int orbit = 1; orbit <= orbits; orbit++)
            {
                for (int i = 0; i < stepsPerOrbit; i++)
                    ig.Step(sys, 0, dt, pos, vel);   // 힘이 t 무관이므로 t는 아무 값이나 OK

                if (orbit % 10 == 0)
                {
                    double E = sys.KineticEnergy(vel) + sys.PotentialEnergy(pos);
                    Console.Write($"{(E - E0) / Math.Abs(E0),10:E1} ");
                }
            }
            Console.WriteLine();
        }
    }
}
