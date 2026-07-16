using UnityEngine;

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

public class LiquidManager : MonoBehaviour
{
    int N;          // 격자 구간 수 (점은 N+1개: 0 ~ N)
    double L;       // 줄 길이
    double h;      // 격자 간격 = L / N
    double nu = 0.0001;  // 동점성계수
    double sensitivity = 500;//감도

    LiquidState2D curLiquidState;//물 상태

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
        h = L / N;

        //액체 상태
        curLiquidState.u = new double[N + 1, N + 1];
        curLiquidState.v = new double[N + 1, N + 1];
        curLiquidState.p = new double[N + 1, N + 1];
        curLiquidState.div = new double[N + 1, N + 1];
        curLiquidState.u_prev = new double[N + 1, N + 1];
        curLiquidState.v_prev = new double[N + 1, N + 1];

        //밀도장
        dens = new double[N + 1, N + 1];
        dens_prev = new double[N + 1, N + 1];

        //텍스터 생성
        tex = new Texture2D(N + 1, N + 1);
        tex.filterMode = FilterMode.Bilinear;

        //시간 간격
        dt = Time.fixedDeltaTime;
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
        if (Input.GetMouseButton(0)){
            AddDensity(ci, cj, 3);
            AddExternalForce(ci, cj, fx, fy);
        }

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
    /// 밀도 스텝
    /// </summary>
    void DensityStep(double dt)
    {
        Copy(dens_prev, dens);
        a = diff * dt / (h * h);
        c = 1 + 4 * a;
        LinSolve(dens, dens_prev,a,c, 20, 0);
        Copy(dens_prev, dens);
        Advect(0,dt,dens,dens_prev, curLiquidState.u, curLiquidState.v);
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
    void AddExternalForce(int ci, int cj,double fx, double fy)
    {
        // 커서가 있는 셀 (ci, cj) 주변에만 주입
        if (ci < 1 || ci >= N || cj < 1 || cj >= N) return;
        curLiquidState.u[ci, cj] += fx;
        curLiquidState.v[ci, cj] += fy;
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

    /// <summary>
    /// 감쇠
    /// </summary>
    void Damping()
    {
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= N; j++)
                dens[i, j] *= 0.995;
    }
    #region 시각화
    /// <summary>
    /// 밀도를 흑백으로 표현
    /// </summary>
    void Render()
    {
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= N; j++)
            {
                float d = (float)MathUtility.ClampValue(dens[i, j],0,1);
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
        int size = (int) MathUtility.Min((double)Screen.width, (double)Screen.height);  // 화면 높이(1080)에 맞춤
        int x = (Screen.width - size) / 2;                  // 가로 중앙 정렬
        GUI.DrawTexture(new Rect(x, 0, size, size), tex);
    }
    #endregion
}
