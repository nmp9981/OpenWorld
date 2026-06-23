using UnityEngine;

/// <summary>
/// 파동 상태
/// </summary>
public struct WaveState2D
{
    public double[,] u;   // 변위 (각 격자점의 위아래 위치)
    public double[,] v;   // 속도 (각 격자점의 시간 변화율 ∂u/∂t)
}
/// <summary>
/// 파동 도함수
/// </summary>
public struct WaveDerived2D
{
    public double[,] du;   // u의 시간도함수 = v
    public double[,] dv;   // v의 시간도함수 = c²·(공간 2차미분) 
}


public class Wave2DManager : MonoBehaviour
{
    int N;          // 격자 구간 수 (점은 N+1개: 0 ~ N)
    double L;       // 줄 길이
    double dx;      // 격자 간격 = L / N
    double dy;
    double c;       // 파동 속도
    double dt;      // 시간 스텝 (CFL로 dx에 묶임)

    WaveState2D currentWaveState;
    WaveDerived2D currentWaveDerived;

    int pointIdx = 0;
    int maxPointIndex;

    [SerializeField] private LineRenderer lineRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SettingParemeter();
        SettingLineRenderer();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        int subStep = 8;
        double fixedDt = Time.fixedDeltaTime / subStep;
        for (int i = 0; i < subStep; i++)
        {
            currentWaveState = RK4_Wave2D(currentWaveState, fixedDt);
        }
        DrawWavePosition(currentWaveState);
    }

    /// <summary>
    /// 파라미터 세팅
    /// </summary>
    void SettingParemeter()
    {
        N = 100; L = 3; c = 5;
        dx = L / N;
        dy = L / N;
        dt = 0.5 * dx / c;

        currentWaveState.u = new double[N + 1,2];
        currentWaveState.v = new double[N + 1,2];
        currentWaveDerived.du = new double[N + 1,2];
        currentWaveDerived.dv = new double[N + 1,2];

        for (int i = 0; i <= N; i++)
        {
            currentWaveState.u[i,0] = MathUtility.Sin(ConstUtility.PI * (i * dx) / L);
            currentWaveState.u[i, 1] = MathUtility.Sin(ConstUtility.PI * (i * dy) / L);
            currentWaveState.v[i,0] = 0;
            currentWaveState.v[i, 1] = 0;
        }
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
        yNext.u = new double[N + 1,2];
        yNext.v = new double[N + 1,2];

        for (int i = 0; i <= N; i++)
        {
            for(int j = 0; j <= 2; j++)
            {
                yNext.u[i,j] = y.u[i,j] + (dt / 6) * (k1.du[i,j] + 2 * k2.du[i,j] + 2 * k3.du[i,j] + k4.du[i,j]);
                yNext.v[i,j] = y.v[i,j] + (dt / 6) * (k1.dv[i,j] + 2 * k2.dv[i,j] + 2 * k3.dv[i,j] + k4.dv[i,j]);
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
        result.u = new double[N + 1,2];
        result.v = new double[N + 1,2];
        for (int i = 0; i <= N; i++)
        {
            for(int j = 0; j < 2; j++)
            {
                result.u[i,j] = y.u[i,j] + dt * k.du[i,j];
                result.v[i,j] = y.v[i,j] + dt * k.dv[i,j];
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
        result.du = new double[N + 1,2];
        result.dv = new double[N + 1,2];
        for (int i = 1; i < N; i++)
        {
            for(int j = 0;j < 2; j++)
            {
                double h = dx * dx;
                double ch = (c * c) / h;

                result.du[i, j] = ch * (y.u[i + 1, j] - 4 * y.u[i, j] - y.u[i - 1, j] + y.u[i, j + 1] - y.u[i,j-1]);
                result.du[i, j] = y.v[i,j];
            }
        }
        //끝점
        result.du[0,0] = 0; result.dv[0,0] = 0;
        result.du[N,0] = 0; result.dv[N,0] = 0;
        result.du[0,1] = 0; result.dv[0,1] = 0;
        result.du[N,1] = 0; result.dv[N,1] = 0;

        return result;
    }

    #region 시각화
    /// <summary>
    /// 라인 렌더러 세팅
    /// </summary>
    void SettingLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = N + 1;
        lineRenderer.startWidth = 0.05f; // 점의 크기 (시작)
        lineRenderer.endWidth = 0.05f;   // 점의 크기 (끝)
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;

        lineRenderer.SetPosition(pointIdx, this.gameObject.transform.position);
    }

    /// <summary>
    /// 파동 위치 그리기
    /// </summary>
    void DrawWavePosition(WaveState2D current)
    {
        for (int i = 0; i <= N; i++)
        {
            float x = (float)(i * dx);
            float y = (float)current.u[i, 0];
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));

            y = (float)(i*dy);
            x = (float)current.u[i,1];
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
    #endregion
}
