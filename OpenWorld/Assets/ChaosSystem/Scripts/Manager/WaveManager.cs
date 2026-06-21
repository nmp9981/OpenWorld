using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 파동 상태
/// </summary>
public struct WaveState
{
    public double[] u;   // 변위 (각 격자점의 위아래 위치)
    public double[] v;   // 속도 (각 격자점의 시간 변화율 ∂u/∂t)
}
/// <summary>
/// 파동 도함수
/// </summary>
public struct WaveDerived
{
    public double[] du;   // u의 시간도함수 = v
    public double[] dv;   // v의 시간도함수 = c²·(공간 2차미분)
}

public class WaveManager : MonoBehaviour
{
    int N;          // 격자 구간 수 (점은 N+1개: 0 ~ N)
    double L;       // 줄 길이
    double dx;      // 격자 간격 = L / N
    double c;       // 파동 속도
    double dt;      // 시간 스텝 (CFL로 dx에 묶임)

    WaveState currentWaveState;
    WaveDerived currentWaveDerived;

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
            currentWaveState = RK4_Wave(currentWaveState, fixedDt);
        }
        DrawWavePosition(currentWaveState);
    }

    /// <summary>
    /// 파라미터 세팅
    /// </summary>
    void SettingParemeter()
    {
        N = 100;L = 1;c = 1;
        dx = L / N;
        dt = 0.5 * dx / c;

        currentWaveState.u = new double[N + 1];
        currentWaveState.v = new double[N + 1];
        currentWaveDerived.du = new double[N + 1];
        currentWaveDerived.dv = new double[N + 1];

        for (int i = 0; i <= N; i++)
        {
            currentWaveState.u[i] = MathUtility.Sin(ConstUtility.PI * (i * dx) / L);
            currentWaveState.v[i] = 0;
        }
    }

    /// <summary>
    /// RK4 적분
    /// </summary>
    /// <param name="y"></param>
    /// <param name="dt"></param>
    public WaveState RK4_Wave(WaveState y, double dt)
    {
        WaveDerived k1 = Cal_WaveState(y);
        WaveDerived k2 = Cal_WaveState(AddScaled(y, k1, dt * 0.5));
        WaveDerived k3 = Cal_WaveState(AddScaled(y, k2, dt * 0.5));
        WaveDerived k4 = Cal_WaveState(AddScaled(y, k3, dt));

        //배열이니 할당을 해야함
        WaveState yNext = new WaveState();
        yNext.u = new double[N + 1];
        yNext.v = new double[N + 1];

        for (int i = 0; i <= N; i++)
        {
            yNext.u[i] = y.u[i]+ (dt / 6) * (k1.du[i] + 2 * k2.du[i] + 2 * k3.du[i] + k4.du[i]);
            yNext.v[i] = y.v[i]+ (dt / 6)*(k1.dv[i] + 2 * k2.dv[i] + 2 * k3.dv[i] + k4.dv[i]);
        }
        return yNext;
    }

    /// <summary>
    /// RK4 계산 함수 F
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public WaveDerived Cal_WaveState(WaveState y)
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
    WaveState AddScaled(WaveState y, WaveDerived k, double dt)
    {
        //배열을 새로 만들어서 할당
        //구조체는 문제없지만 그 안이 배열이 참조타입이다.
        WaveState result = new WaveState();
        result.u = new double[N + 1];
        result.v = new double[N + 1];
        for (int i = 0; i <= N; i++)
        {
            result.u[i] = y.u[i] + dt * k.du[i];
            result.v[i] = y.v[i] + dt*k.dv[i];
        }
        return result;
    }

    /// <summary>
    /// 속도, 위치 계산
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    WaveDerived Cal_Pos_Velocity(WaveState y)
    {
        WaveDerived result = new WaveDerived();
        result.du = new double[N + 1];
        result.dv = new double[N + 1];
        for (int i = 1; i < N; i++)
        {
            double cx = c * c / (dx*dx);
            result.dv[i] = cx*(y.u[i+1]-2*y.u[i]+y.u[i-1]);
            result.du[i] = y.v[i];
        }
        //끝점
        result.du[0] = 0; result.dv[0] = 0;
        result.du[N] = 0; result.dv[N] = 0;

        return result;
    }

    #region 시각화
    /// <summary>
    /// 라인 렌더러 세팅
    /// </summary>
    void SettingLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = N+1;
        lineRenderer.startWidth = 0.05f; // 점의 크기 (시작)
        lineRenderer.endWidth = 0.05f;   // 점의 크기 (끝)
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;

        lineRenderer.SetPosition(pointIdx, this.gameObject.transform.position);
    }

    /// <summary>
    /// 파동 위치 그리기
    /// </summary>
    void DrawWavePosition(WaveState current)
    {
        for (int i = 0; i <= N; i++)
        {
            float x = (float)(i * dx);
            float y = (float)current.u[i];
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
    #endregion
}
