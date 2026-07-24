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

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterScript : MonoBehaviour
{
    int N;          // 격자 구간 수 (점은 N+1개: 0 ~ N)
    double dx;      // 격자 간격 = L / N
    double gravity = 1;//중력 가속도

    WaterState curWaterState;//물 상태
    WaterDerivedState waterDerivedState;

    double fx, fy, dt;

    //텍스처
    Texture2D tex;

    //Mesh
    Mesh mesh;
    Vector3[] vertices;
    double heightScale = 1.0;   // 물결 과장 배율
    Color[] colors;
    Vector2[] uvs;

    private void Awake()
    {
        LiquidInit();
        CreateMesh();
        curWaterState.h[N / 2, N / 2] += 0.5;
    }

    private void Update()
    {
        InputMouses();
        RenderMesh();
    }
  
    // Update is called once per frame
    void FixedUpdate()
    {
        LiquidStep(dt);
    }

    /// <summary>
    /// 액체 초기화
    /// </summary>
    void LiquidInit()
    {
        N = 64;                 // 격자 크기
        dx = 1;

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
    /// 높이 추가
    /// </summary>
    /// <param name="ci"></param>
    /// <param name="cj"></param>
    /// <param name="amount"></param>
    void AddHeight(int ci, int cj, double amount)
    {
        //경계 처리
        if (ci < 1 || ci >= N || cj < 1 || cj >= N) return;

        //주변셀에 가중치
        for (int di = -1; di <= 1; di++)
            for (int dj = -1; dj <= 1; dj++)
            {
                int ni = ci + di;
                int nj = cj + dj;
                //경계 조건
                if (ni < 0 || ni > N || nj < 0 || nj > N) continue;
                //가중치
                double w = (di == 0 && dj == 0) ? 1 : 0.5;
                curWaterState.h[ni, nj] += (amount*w);
            }
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
                //전방 차분
                double diffFluxX = curWaterState.x_flux[i + 1, j] - curWaterState.x_flux[i, j];
                double diffFluxY = curWaterState.y_flux[i, j + 1] - curWaterState.y_flux[i, j];
                waterDerivedState.dh[i, j] = (-diffFluxX - diffFluxY) / dx;

                //갱신
                curWaterState.h[i, j] = curWaterState.h_prev[i, j] + dt * waterDerivedState.dh[i, j];
            }
        }
        //평활화
        for (int i = 1; i < N; i++)
            for (int j = 1; j < N; j++)
            {
                double avg = 0.25 * (curWaterState.h[i + 1, j] + curWaterState.h[i - 1, j]
                                  + curWaterState.h[i, j + 1] + curWaterState.h[i, j - 1]);
                curWaterState.h[i, j] = 0.97 * curWaterState.h[i, j] + 0.03 * avg;
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
                //후방 차분
                double diffFluxX = curWaterState.h_prev[i, j] - curWaterState.h_prev[i - 1, j];
                double diffFluxY = curWaterState.h_prev[i, j] - curWaterState.h_prev[i, j - 1];
                curWaterState.u[i, j] = curWaterState.u[i, j] - dt*gravity*diffFluxX/dx;
                curWaterState.v[i, j] = curWaterState.v[i, j] - dt * gravity * diffFluxY / dx;
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

    #region 입력
    void InputMouses()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane waterPlane = new Plane(Vector3.up, transform.position);  // 수면 평면
            //물체에 닿으면
            if (waterPlane.Raycast(ray, out float dist))
            {
                Vector3 p = ray.GetPoint(dist);
                //로컬->격자 인덱스
                Vector3 local = transform.InverseTransformPoint(p);
                int ci = MathUtility.RountToInt(local.x);
                int cj = MathUtility.RountToInt(local.z);
                AddHeight(ci, cj, 0.5);
            }
        }
    }
    #endregion

    #region 시각화
    /// <summary>
    /// 메쉬 생성
    /// </summary>
    void CreateMesh()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = mesh;

        int size = N + 1;
        vertices = new Vector3[size*size];
        colors = new Color[size * size];
        uvs = new Vector2[size * size];
        int[] triangles = new int[N*N*6];

        //점점 xz위치
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= N; j++)
                vertices[i * size + j] = new Vector3(i, 0, j);

        //삼각형 인덱스
        int t = 0;
        for(int i = 0; i < N; i++)
        {
            for(int j = 0; j < N; j++)
            {
                int aa = i * size + j;
                int bb = (i+1) * size + j;
                int cc = i * size + j + 1;
                int dd = (i+1) * size + j + 1;

                triangles[t++] = aa; triangles[t++] = cc; triangles[t++] = bb;
                triangles[t++] = bb; triangles[t++] = cc; triangles[t++] = dd;
            }
        }
        //uv채우기
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= N; j++)
                uvs[i * size + j] = new Vector2((float)i / N, (float)j / N);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    /// <summary>
    /// 메시 렌더링
    /// </summary>
    void RenderMesh() {
        //물결 과장 비율을 곱함
        int size = N + 1;
        for (int i = 0; i <= N; i++)
            for (int j = 0; j <= N; j++)
            {
                int idx = i * size + j;
                vertices[idx].y = (float)((curWaterState.h[i, j] - 1.0) * heightScale);

                //높이를 0~1로 정규화해 색에 저장 (R채널 사용)
                float hn = (float)MathUtility.ClampValue((curWaterState.h[i, j] - 1.0) * 3,0,1);
                colors[idx] = new Color(hn,0,0,1);
            }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();   // 음영 갱신 — 없으면 밋밋함
    }
    #endregion
}