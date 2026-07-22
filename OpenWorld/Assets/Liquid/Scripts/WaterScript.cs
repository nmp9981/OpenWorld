using System.Security.Cryptography;
using UnityEngine;

/// <summary>
/// 유체 상태
/// </summary>
public struct WaterState
{
    public double[,] u;   // (i,j) 셀에서 유체가 x방향으로 흐르는 속도
    public double[,] v;   // (i,j) 셀에서 유체가 y방향으로 흐르는 속도
    public double[,] h;   // (i,j) 셀에서의 높이
    public double[,] p;   // 압력
    public double[,] x_flux, y_flux;   // 유량
    public double[,] div;  //발산(투영 단계 임시 버퍼)
    public double[,] h_prev;   // 이전 스텝값
    public double[,] u_prev, v_prev;// 이전 스텝 값(확산·이류의 소스)
}

public struct WaterDerivedState
{
    public double[,] dh;   // (i,j) 셀에서의 높이
}

public class WaterScript : MonoBehaviour
{
    int N;          // 격자 구간 수 (점은 N+1개: 0 ~ N)
    double L;       // 줄 길이
    double dx;      // 격자 간격 = L / N
    double sensitivity = 500;//감도
    double gravity = 9.81;//중력 가속도

    WaterState curWaterState;//물 상태
    WaterDerivedState waterDerivedState;

    double fx, fy, dt;

    //밀도장
    double[,] dens, dens_prev;
    double diff = 0.0001;              // 밀도 확산율 (파라미터)
    double a;    // 확산계수
    double c;              // 분모
    //텍스처
    Texture2D tex;

    private void Awake()
    {
        LiquidInit();
    }

    private void Update()
    {
        InputMouse();
        Render();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        LiquidStep(dt);
        DensityStep(dt);
    }

    /// <summary>
    /// 액체 초기화
    /// </summary>
    void LiquidInit()
    {
        N = 64;                 // 격자 크기
        L = 1.0;
        dx = L / N;

        //액체 상태
        curWaterState.u = new double[N + 1, N + 1];
        curWaterState.v = new double[N + 1, N + 1];
        curWaterState.h = new double[N + 1, N + 1];
        curWaterState.p = new double[N + 1, N + 1];
        curWaterState.div = new double[N + 1, N + 1];
        curWaterState.h_prev = new double[N + 1, N + 1];
        curWaterState.x_flux = new double[N + 1, N + 1];
        curWaterState.y_flux = new double[N + 1, N + 1];
        curWaterState.u_prev = new double[N + 1, N + 1];
        curWaterState.v_prev = new double[N + 1, N + 1];
        waterDerivedState.dh = new double[N + 1, N + 1];

        //밀도장
        dens = new double[N + 1, N + 1];
        dens_prev = new double[N + 1, N + 1];

        //텍스터 생성
        tex = new Texture2D(N + 1, N + 1);
        tex.filterMode = FilterMode.Bilinear;

        //시간 간격
        dt = Time.fixedDeltaTime;

        //초기화
        double height0 = 1;
        for (int i = 0; i <= N; i++)
        {
            for(int j = 0; j <= N; j++)
            {
                curWaterState.h[i, j] = height0;
            }
        }
    }

    /// <summary>
    /// 스텝
    /// </summary>
    void LiquidStep(double dt)
    {
        //마우스 화면 좌표를 격자 인덱스로 변환
        Vector3 mp = Input.mousePosition;
        int size = Mathf.Min(Screen.width, Screen.height);
        int offsetX = (Screen.width - size) / 2;
        int ci = (int)((mp.x - offsetX) / size * N);
        int cj = (int)((Screen.height - mp.y) / size * N);   // 렌더 상하반전과 맞춤
        if (Input.GetMouseButton(0))
        {
            AddDensity(ci, cj, 3);
            //AddExternalForce(ci, cj, fx, fy);
            AddHeight(ci, cj, 0.02);
        }

        //높이, 속도 최대 최소, 기본 수심
        double hMax = 0;
        double uMax = 0;
        for (int i = 0; i <= N; i++)
        {
            for (int j = 0; j <= N; j++)
            {
                double su = MathUtility.Abs(curWaterState.u[i, j]);
                double sv = MathUtility.Abs(curWaterState.v[i, j]);
                if (curWaterState.h[i, j] > hMax) hMax = curWaterState.h[i,j];
                if(su>uMax) uMax = su;
                if (sv > uMax) uMax = sv;
            }
        }

        //소스 준비
        Copy(curWaterState.h_prev, curWaterState.h);
        Copy(curWaterState.u_prev, curWaterState.u);
        Copy(curWaterState.v_prev, curWaterState.v);

        //제약 조건
        double maxDt = 0.4*dx / (MathUtility.Sqrt(gravity * hMax) + uMax+1e-6);
        if (dt > maxDt) dt = maxDt;
        
        //유량 계산
        DecreteHeight(dt);
        //경계 조건
        DecreteBoundaryCondition(curWaterState.h, 0);

        //속도 갱신
        DecretePressure(dt);
        //경계 조건
        DecreteBoundaryCondition(curWaterState.u, 1);
        DecreteBoundaryCondition(curWaterState.v, 2);

        //이류 : 소스 준비
        Copy(curWaterState.u_prev, curWaterState.u);
        Copy(curWaterState.v_prev, curWaterState.v);
        //x,y방향 각각
        Advect(1, dt, curWaterState.u, curWaterState.u_prev, curWaterState.u_prev, curWaterState.v_prev);
        Advect(2, dt, curWaterState.v, curWaterState.v_prev, curWaterState.u_prev, curWaterState.v_prev);
    }

    /// <summary>
    /// 밀도 스텝
    /// </summary>
    void DensityStep(double dt)
    {
        Copy(dens_prev, dens);
        a = diff * dt / (dx * dx);
        c = 1 + 4 * a;
        LinSolve(dens, dens_prev, a, c, 20, 0);
        Copy(dens_prev, dens);
        Advect(0, dt, dens, dens_prev, curWaterState.u, curWaterState.v);
        Damping();
    }

    /// <summary>
    /// 입력
    /// </summary>
    void InputMouse()
    {
        fx = Input.GetAxis("Mouse X") * sensitivity * dt;
        fy = Input.GetAxis("Mouse Y") * sensitivity * dt;
    }

    /// <summary>
    /// 외력 추가
    /// </summary>
    void AddExternalForce(int ci, int cj, double fx, double fy)
    {
        // 커서가 있는 셀 (ci, cj) 주변에만 주입
        if (ci < 1 || ci >= N || cj < 1 || cj >= N) return;
        curWaterState.u[ci, cj] += fx;
        curWaterState.v[ci, cj] += fy;
    }
    /// <summary>
    /// 밀도 추가
    /// </summary>
    void AddDensity(int ci, int cj, double amount)
    {
        // 커서가 있는 셀 (ci, cj) 주변에만 주입
        if (ci < 1 || ci >= N || cj < 1 || cj >= N) return;
        dens[ci, cj] += amount;
    }
    /// <summary>
    /// 높이 추가
    /// </summary>
    /// <param name="ci"></param>
    /// <param name="cj"></param>
    /// <param name="amount"></param>
    void AddHeight(int ci, int cj, double amount)
    {
        if (ci < 1 || ci >= N || cj < 1 || cj >= N) return;
        curWaterState.h[ci, cj] += amount;
    }

    /// <summary>
    /// 높이 이산화
    /// </summary>
    void DecreteHeight(double dt)
    {
        //유량 계산
        for (int i = 1; i < N; i++)
        {
            for (int j = 1; j < N; j++)
            {
                //유량 계산
                curWaterState.x_flux[i, j] = curWaterState.h_prev[i, j] * curWaterState.u[i, j];
                curWaterState.y_flux[i, j] = curWaterState.h_prev[i, j] * curWaterState.v[i, j];
            }
        }
        //미분 후 높이 갱신
        for (int i = 1; i < N; i++)
        {
            for (int j = 1; j < N; j++)
            {
                //중심 차분
                double diffFluxX = curWaterState.x_flux[i + 1, j] - curWaterState.x_flux[i - 1, j];
                double diffFluxY = curWaterState.y_flux[i, j + 1] - curWaterState.y_flux[i, j - 1];
                waterDerivedState.dh[i, j] = (-diffFluxX - diffFluxY) / (2 * dx);

                //갱신
                curWaterState.h[i, j] = curWaterState.h_prev[i, j] + dt * waterDerivedState.dh[i, j];
            }
        }
    }
    /// <summary>
    /// 압력 이산화
    /// 높이 기울기 중심 차분
    /// </summary>
    void DecretePressure(double dt)
    {
        for (int i = 1; i < N; i++)
        {
            for (int j = 1; j < N; j++)
            {
                //중심 차분
                double diffFluxX = curWaterState.h_prev[i + 1, j] - curWaterState.h_prev[i - 1, j];
                double diffFluxY = curWaterState.h_prev[i, j + 1] - curWaterState.h_prev[i, j - 1];
                curWaterState.u[i, j] = curWaterState.u[i, j] - dt*gravity*diffFluxX/(2*dx);
                curWaterState.v[i, j] = curWaterState.v[i, j] - dt * gravity * diffFluxY / (2 * dx);
            }
        }
    }

    /// <summary>
    /// 이류 이산화
    /// flag: 경계 플래그, d: 결과, d0: 이전 값(소스), velU/velV: 이류시키는 속도장
    /// </summary>
    void Advect(int flag, double dt, double[,] d, double[,] d0, double[,] velU, double[,] velV)
    {
        for (int i = 1; i < N; i++)
        {
            for (int j = 1; j < N; j++)
            {
                //역 추적점
                double prevX = i - (dt * velU[i, j] / dx);
                double prevY = j - (dt * velV[i, j] / dx);

                //클램프
                if (prevX < 0.5) prevX = 0.5;
                if (prevX > N - 0.5) prevX = N - 0.5;
                if (prevY < 0.5) prevY = 0.5;
                if (prevY > N - 0.5) prevY = N - 0.5;

                //정수/소수부
                int i0 = (int)prevX;
                int i1 = i0 + 1;
                int j0 = (int)prevY;
                int j1 = j0 + 1;

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
    /// 감쇠
    /// </summary>
    void Damping()
    {
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= N; j++)
                dens[i, j] *= 0.995;
    }

    /// <summary>
    /// 경계 조건 이산화
    /// </summary>
    void DecreteBoundaryCondition(double[,] x, int b)
    {
        //모서리
        for (int j = 0; j <= N; j++)
        {
            x[0, j] = (b == 1) ? -x[1, j] : x[1, j];
        }
        for (int j = 0; j <= N; j++)
        {
            x[N, j] = (b == 1) ? -x[N - 1, j] : x[N - 1, j];
        }
        for (int i = 0; i <= N; i++)
        {
            x[i, 0] = (b == 2) ? -x[i, 1] : x[i, 1];
        }
        for (int i = 0; i <= N; i++)
        {
            x[i, N] = (b == 2) ? -x[i, N - 1] : x[i, N - 1];
        }

        //코너
        x[0, 0] = 0.5 * (x[1, 0] + x[0, 1]);
        x[0, N] = 0.5 * (x[1, N] + x[0, N - 1]);
        x[N, 0] = 0.5 * (x[N - 1, 0] + x[N, 1]);
        x[N, N] = 0.5 * (x[N - 1, N] + x[N, N - 1]);
    }
    #region 솔버
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
                    x[i, j] = (x0[i, j] + a * sumIJ) / c;
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
    #endregion

    #region 시각화
    /// <summary>
    /// 밀도를 흑백으로 표현
    /// </summary>
    void Render()
    {
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= N; j++)
            {
                float d = (float)((curWaterState.h[i, j] - 1) * 5 + 0.5);
                d = (float)MathUtility.ClampValue(d, 0, 1);
                Color col = new Color(
    d * 0.4f,          // R: 적게
    d * 0.7f,          // G: 중간
    0.3f + d * 0.7f    // B: 기본으로 깔고 밀도만큼 밝게
);
                tex.SetPixel(i, j, col);
            }
        tex.Apply();
    }
    void OnGUI()
    {
        int size = (int)MathUtility.Min((double)Screen.width, (double)Screen.height);  // 화면 높이(1080)에 맞춤
        int x = (Screen.width - size) / 2;                  // 가로 중앙 정렬
        GUI.DrawTexture(new Rect(x, 0, size, size), tex);
    }
    #endregion
}
