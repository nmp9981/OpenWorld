using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 유체 상태
/// </summary>
public struct LiquidState2D
{
    public double[,] u;   // (i,j) 셀에서 유체가 x방향으로 흐르는 속도
    public double[,] v;   // (i,j) 셀에서 유체가 y방향으로 흐르는 속도
    public double[,] p;   // 압력
    public double[,] div;  //발산(투영 단계 임시 버퍼)
    public double[,] u_prev, v_prev;// 이전 스텝 값(확산·이류의 소스)
}
/// <summary>
/// 유체 도함수
/// </summary>
public struct LiquidDerived2D
{
    public double[,] du;   // u의 시간도함수 = v
    public double[,] dv;   // v의 시간도함수 = c²·(공간 2차미분) 
    public double[,] dp;   // 압력 도함수
}


public class LiquidManager : MonoBehaviour
{
    int N;          // 격자 구간 수 (점은 N+1개: 0 ~ N)
    double L;       // 줄 길이
    double h;      // 격자 간격 = L / N
    double dx;      // 격자 간격 = L / N
    double dy;
    double c;       // 파동 속도
    double nu = 1;  // 동점성계수

    double sensitivity = 500;

    private Vector3D curForce;
    private Vector3D outForce;

    LiquidState2D curLiquidState;
 
    // Update is called once per frame
    void FixedUpdate()
    {
        LiquidStep();
    }

    /// <summary>
    /// 스텝
    /// </summary>
    void LiquidStep()
    {
        double dt = Time.fixedDeltaTime;

        //외력
        double fx= Input.GetAxis("Mouse X") * sensitivity*dt;
        double fy= Input.GetAxis("Mouse Y") * sensitivity*dt;
        AddExternalForce(fx,fy);

        // 확산: 소스 준비
        Copy(curLiquidState.u_prev, curLiquidState.u);
        Copy(curLiquidState.v_prev, curLiquidState.v);
        Spread(dt);
        
        //이류 전 투영
        Projection(dt);

        //이류 : 소스 준비
        Copy(curLiquidState.u_prev, curLiquidState.u);
        Copy(curLiquidState.v_prev, curLiquidState.v);
        //x,y방향 각각
        Advect(1,dt, curLiquidState.u, curLiquidState.u_prev, curLiquidState.u_prev, curLiquidState.v_prev);
        Advect(2, dt, curLiquidState.v, curLiquidState.v_prev, curLiquidState.u_prev, curLiquidState.v_prev);

        //이류 후 투영
        Projection(dt);
    }

    /// <summary>
    /// 외력 추가
    /// </summary>
    void AddExternalForce(double fx, double fy)
    {
        for (int i = 0; i <= N; i++)
        {
            for (int j = 0; j <= N; j++)
            {
                curLiquidState.u[i, j] += fx;
                curLiquidState.u[i, j] += fy;
            }
        }
    }

    /// <summary>
    /// 확산
    /// </summary>
    void Spread(double dt)
    {
        double a = nu*dt / (h*h);
        LinSolve(curLiquidState.u,curLiquidState.u_prev,a,1+4*a,30,1);
        LinSolve(curLiquidState.v, curLiquidState.v_prev, a, 1 + 4 * a, 30,2);
    }

    /// <summary>
    /// 투영
    /// </summary>
    /// <param name="dt"></param>
    void Projection(double dt)
    {
        int repeatCount = 30;//반복

        //발산 계산
        for (int i = 1; i < N; i++)
        {
            for (int j = 1; j < N; j++)
            {
                //발산 계산
                curLiquidState.div[i, j]
                    = (curLiquidState.u[i + 1, j] - curLiquidState.u[i - 1, j] + curLiquidState.v[i, j + 1] - curLiquidState.v[i, j - 1]) / (2 * h);
                //압력 초기화
                curLiquidState.p[i, j] = 0;
            }
        }

        //포아송 풀기
        for (int k = 0;k < repeatCount; k++)
        {
            for (int i = 1; i < N; i++)
            {
                for (int j = 1; j < N; j++)
                {
                    //점 4개 합
                    double p = curLiquidState.p[i + 1, j] + curLiquidState.p[i - 1, j] + curLiquidState.p[i, j + 1] + curLiquidState.p[i, j - 1];
                    //투영 단계의 이산화
                    curLiquidState.p[i, j] = (p - h * h * curLiquidState.div[i, j]) * 0.25;
                }
            }
            DecreteBoundaryCondition(curLiquidState.p, 0);
        }
        
        //기울기 빼기
        for (int i = 1; i < N; i++)
        {
            for (int j = 1; j < N; j++)
            {
                curLiquidState.u[i, j] -= ((curLiquidState.p[i + 1, j] - curLiquidState.p[i - 1, j]) / (2 * h));
                curLiquidState.v[i, j] -= ((curLiquidState.p[i, j + 1] - curLiquidState.p[i, j - 1]) / (2 * h));
            }
        }
        // 기울기 빼기 루프 끝난 뒤
        DecreteBoundaryCondition(curLiquidState.u, 1);
        DecreteBoundaryCondition(curLiquidState.v, 2);
    }

    /// <summary>
    /// 이류 이산화
    /// flag: 경계 플래그, d: 결과, d0: 이전 값(소스), velU/velV: 이류시키는 속도장
    /// </summary>
    void Advect(int flag, double dt,double[,] d, double[,] d0, double[,] velU, double[,] velV)
    {
        for (int i = 1; i < N; i++)
        {
            for (int j = 1; j < N; j++)
            {
                //역 추적점
                double prevX = i - (dt * velU[i,j]/h);
                double prevY = j - (dt * velV[i, j] / h);

                //클램프
                if (prevX < 0.5) prevX = 0.5;
                if(prevX> N - 0.5) prevX = N-0.5;
                if (prevY < 0.5) prevY = 0.5;
                if (prevY > N - 0.5) prevY = N - 0.5;

                //정수/소수부
                int i0 = (int)prevX;
                int i1 = i0+1;
                int j0 = (int)prevY;
                int j1 = j0+1;

                double s1 = prevX - i0; double s0 = 1 - s1;
                double t1 = prevY - j0; double t0 = 1 - t1;

                //이중 선형 보간
                d[i, j] = s0 * (t0 * d0[i0, j0] + t1 * d0[i0, j1]) + s1 * (t0 * d0[i1, j0] + t1 * d0[i1, j1]);
            }
        }
        //경계 조건
        DecreteBoundaryCondition(d, flag);
    }

    /// <summary>
    /// 경계 조건 이산화
    /// </summary>
    void DecreteBoundaryCondition(double[,] x, int b)
    {
        //모서리
        for (int j = 0; j <= N; j++)
        {
            x[0, j] = (b==1)?-x[1, j]:x[1,j];
        }
        for (int j = 0; j <= N; j++)
        {
            x[N, j] = (b == 1) ? -x[N - 1, j]:x[N-1,j];
        }
        for (int i = 0; i <= N; i++)
        {
            x[i, 0] = (b==2)?-x[i, 1]:x[i,1];
        }
        for (int i = 0; i <= N; i++)
        {
            x[i, N] = (b==2)?-x[i, N - 1]:x[i,N-1];
        }

        //코너
        x[0, 0] = 0.5 * (x[1, 0] + x[0,1]);
        x[0, N] = 0.5 * (x[1, N] + x[0, N-1]);
        x[N, 0] = 0.5 * (x[N-1, 0] + x[N, 1]);
        x[N, N] = 0.5 * (x[N-1, N] + x[N, N-1]);
    }

    /// <summary>
    /// RK4 적분
    /// </summary>
    /// <param name="y"></param>
    /// <param name="dt"></param>
    public WaveState2D RK4_Wave2D(WaveState2D y, double dt)
    {
        WaveDerived2D k1 = Cal_WaveState(y);
        WaveDerived2D k2 = Cal_WaveState(AddScaled(y, k1, dt * 0.5));
        WaveDerived2D k3 = Cal_WaveState(AddScaled(y, k2, dt * 0.5));
        WaveDerived2D k4 = Cal_WaveState(AddScaled(y, k3, dt));

        //배열이니 할당을 해야함
        WaveState2D yNext = new WaveState2D();
        yNext.u = new double[N + 1, N + 1];
        yNext.v = new double[N + 1, N + 1];

        for (int i = 0; i <= N; i++)
        {
            for (int j = 0; j <= N; j++)
            {
                yNext.u[i, j] = y.u[i, j] + (dt / 6) * (k1.du[i, j] + 2 * k2.du[i, j] + 2 * k3.du[i, j] + k4.du[i, j]);
                yNext.v[i, j] = y.v[i, j] + (dt / 6) * (k1.dv[i, j] + 2 * k2.dv[i, j] + 2 * k3.dv[i, j] + k4.dv[i, j]);
            }
        }
        return yNext;
    }

    /// <summary>
    /// RK4 계산 함수 F
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public WaveDerived2D Cal_WaveState(WaveState2D y)
    {
        return Cal_Pos_Velocity(y);
    }

    /// <summary>
    /// 헬퍼 함수
    /// 상태 + 도함수 * h → 새 상태
    /// </summary>
    /// <param name="y"></param>
    /// <param name="k"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    WaveState2D AddScaled(WaveState2D y, WaveDerived2D k, double dt)
    {
        //배열을 새로 만들어서 할당
        //구조체는 문제없지만 그 안이 배열이 참조타입이다.
        WaveState2D result = new WaveState2D();
        result.u = new double[N + 1, N + 1];
        result.v = new double[N + 1, N + 1];
        for (int i = 0; i <= N; i++)
        {
            for (int j = 0; j <= N; j++)
            {
                result.u[i, j] = y.u[i, j] + dt * k.du[i, j];
                result.v[i, j] = y.v[i, j] + dt * k.dv[i, j];
            }
        }
        return result;
    }

    /// <summary>
    /// 속도, 위치 계산
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    WaveDerived2D Cal_Pos_Velocity(WaveState2D y)
    {
        WaveDerived2D result = new WaveDerived2D();
        result.du = new double[N + 1, N + 1];
        result.dv = new double[N + 1, N + 1];
        for (int i = 1; i < N; i++)
        {
            for (int j = 1; j < N; j++)
            {
                double h = dx * dx;
                double ch = (c * c) / h;

                result.du[i, j] = y.v[i, j];
                result.dv[i, j] = ch * (y.u[i + 1, j] - 4 * y.u[i, j] + y.u[i - 1, j] + y.u[i, j + 1] + y.u[i, j - 1]);
            }
        }
        //끝변
        for (int i = 0; i <= N; i++)
        {
            result.du[0, i] = 0; result.dv[0, i] = 0;
            result.du[N, i] = 0; result.dv[N, i] = 0;
            result.du[i, 0] = 0; result.dv[i, 0] = 0;
            result.du[i, N] = 0; result.dv[i, N] = 0;
        }
        return result;
    }

    /// <summary>
    /// 선형 솔버
    /// </summary>
    void LinSolve(double[,] x, double[,] x0, double a, double c, int iter, int flag)
    {
        for (int k = 0; k < iter; k++)
        {
            for (int i = 1; i < N; i++)
            {
                for (int j = 1; j < N; j++)
                {
                    double sumIJ = x[i + 1, j] + x[i - 1, j] + x[i, j + 1] + x[i, j - 1];
                    x[i, j] = (x0[i, j] + a * sumIJ)/c;
                }
            }
            //경계 조건 적용
            DecreteBoundaryCondition(x, flag);
        }
    }
    /// <summary>
    /// 배열 복사
    /// </summary>
    /// <param name="dst"></param>
    /// <param name="src"></param>
    void Copy(double[,] dst, double[,] src)
    {
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= N; j++)
                dst[i, j] = src[i, j];
    }
}
