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
    double reverseRoot2;

    WaveState2D currentWaveState;
    WaveDerived2D currentWaveDerived;

    int pointIdx = 0;
    int maxPointIndex;

    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

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
        //제약 조건
        if (c * fixedDt > dx * reverseRoot2)
        {
            fixedDt = (dx * reverseRoot2) / c;
        }
        //루프 시작
        for (int i = 0; i < subStep; i++)
        {
            currentWaveState = RK4_Wave2D(currentWaveState, fixedDt);
        }
        //시각화
        DrawWavePosition(currentWaveState);
        Debug.Log(currentWaveState.u[N/2,N/2]);
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
        reverseRoot2 = 0.7071068;

        currentWaveState.u = new double[N + 1,N+1];
        currentWaveState.v = new double[N + 1,N+1];
        currentWaveDerived.du = new double[N + 1,N+1];
        currentWaveDerived.dv = new double[N + 1,N+1];

        for (int i = 0; i <= N; i++)
        {
            for(int j = 0; j <= N; j++)
            {
                currentWaveState.u[i, j] = MathUtility.Sin(ConstUtility.PI * (i * dx) / L)* MathUtility.Sin(ConstUtility.PI * (j * dy) / L);
                currentWaveState.v[i, j] = 0;
            }
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
        yNext.u = new double[N + 1,N+1];
        yNext.v = new double[N + 1,N+1];

        for (int i = 0; i <= N; i++)
        {
            for(int j = 0; j <= N; j++)
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
        result.u = new double[N + 1,N+1];
        result.v = new double[N + 1,N+1];
        for (int i = 0; i <= N; i++)
        {
            for(int j = 0; j <= N; j++)
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
        result.du = new double[N + 1,N+1];
        result.dv = new double[N + 1,N+1];
        for (int i = 1; i < N; i++)
        {
            for(int j = 1;j < N; j++)
            {
                double h = dx * dx;
                double ch = (c * c) / h;

                result.du[i, j] = y.v[i, j];
                result.dv[i, j] = ch * (y.u[i + 1, j] - 4 * y.u[i, j] + y.u[i - 1, j] + y.u[i, j + 1] + y.u[i,j-1]);
            }
        }
        //끝변
        for(int i = 0; i <= N; i++)
        {
            result.du[0, i] = 0; result.dv[0, i] = 0;
            result.du[N, i] = 0; result.dv[N, i] = 0;
            result.du[i, 0] = 0; result.dv[i, 0] = 0;
            result.du[i, N] = 0; result.dv[i, N] = 0;
        }
        return result;
    }

    #region 시각화
    /// <summary>
    /// 라인 렌더러 세팅
    /// </summary>
    void SettingLineRenderer()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // 실제 필요한 사각형 개수에 따라 배열 크기를 조절합니다.
        int quadCount = (N+1)*(N+1);
        vertices = new Vector3[quadCount];
        triangles = new int[N*N*6];

        //정점
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= N; j++)
                vertices[i * (N + 1) + j] = new Vector3(
                    (float)(i * dx), 0f, (float)(j * dy));   // 위치. 높이는 갱신때 u로

        //삼각형
        for (int i = 0; i < N; i++)
        {
            for(int j = 0; j < N; j++)
            {
                int bl = i * (N + 1) + j;          // 아래-왼쪽
                int br = (i + 1) * (N + 1) + j;    // 아래-오른쪽
                int tl = i * (N + 1) + (j + 1);    // 위-왼쪽
                int tr = (i + 1) * (N + 1) + (j + 1); // 위-오른쪽

                int tIndex = (i * N + j) * 6;

                // 삼각형 인덱스 설정 (하나의 사각형은 2개의 삼각형으로 구성)
                // 시계 방향(혹은 반시계 방향)으로 정점 인덱스를 연결해야 앞면이 보입니다.
                triangles[tIndex + 0] = bl;
                triangles[tIndex + 1] = tr;
                triangles[tIndex + 2] = br;

                triangles[tIndex + 3] = bl;
                triangles[tIndex + 4] = tl;
                triangles[tIndex + 5] = tr;
            }
        }
        // Mesh 데이터 할당
        mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;   // 한 번만 연결
    }

    /// <summary>
    /// 파동 위치 그리기
    /// </summary>
    void DrawWavePosition(WaveState2D current)
    {
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= N; j++)
                vertices[i * (N + 1) + j] = new Vector3(
                    (float)(i * dx),
                    (float)current.u[i, j],   // 높이 = 변위 u
                    (float)(j * dy));

        mesh.SetVertices(vertices);     // 정점만 갱신
        mesh.RecalculateNormals();      // 조명 위해 법선 재계산
        mesh.RecalculateBounds();       // 컬링 위해 경계 재계산
    }
    #endregion
}
