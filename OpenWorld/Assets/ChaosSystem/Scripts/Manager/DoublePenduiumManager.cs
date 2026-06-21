using Unity.Collections;
using UnityEngine;

//진자 상태
public struct PenduiumState
{
    public double angle1;
    public double angleVelocity1;
    public double angle2;
    public double angleVelocity2;
}
//진자 도함수
public struct PenduiumDerived
{
    public double d_angle1;
    public double d_angleVelocity1;
    public double d_angle2;
    public double d_angleVelocity2;
}

public class DoublePenduiumManager : MonoBehaviour
{
    double l1, l2;
    double m1, m2;
    
    PenduiumState currentState;

    int maxPointIndex = 3;
    int pointIdx = 0;
    Vector3 pivot;

    [SerializeField] private LineRenderer lineRenderer;


    double e0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //초기 상태 설정
        l1 = 1;l2 = 1;
        m1 = 1;m2 = 1;
        pivot = Vector3.zero;//고정점
        SettingInitCase();
        SettingLineRenderer();

        e0 = Energy(currentState);//초기 에너지
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        int subStep = 8;
        double dt = Time.fixedDeltaTime / subStep;
        for (int i = 0; i < subStep; i++)
        {
            currentState = RK4_theta(currentState, dt);
        }
        double drift =MathUtility.Abs(Energy(currentState) - e0) / MathUtility.Abs(e0);
        //Debug.Log(drift);
        SetPosition(currentState);
        //TestCos();
    }

    /// <summary>
    /// 라인 렌더러 세팅
    /// </summary>
    void SettingLineRenderer()
    {
        lineRenderer.positionCount = maxPointIndex;
        lineRenderer.startWidth = 0.05f; // 점의 크기 (시작)
        lineRenderer.endWidth = 0.05f;   // 점의 크기 (끝)
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;

        lineRenderer.SetPosition(pointIdx, this.gameObject.transform.position);
        pointIdx++;
    }

    /// <summary>
    /// 초기 조건 세팅
    /// </summary>
    void SettingInitCase()
    {
        currentState.angle1 = ConstUtility.PI / 2;   // 첫 진자를 수평으로 (90도)
        currentState.angle2 = ConstUtility.PI / 2+0.01;   // 둘째 진자도 수평으로
        currentState.angleVelocity1 = 0;     // 정지 상태에서 놓기
        currentState.angleVelocity2 = 0;
    }

    /// <summary>
    /// RK4 적분
    /// </summary>
    /// <param name="y"></param>
    /// <param name="dt"></param>
    public PenduiumState RK4_theta(PenduiumState y, double dt)
    {
        PenduiumDerived k1 = Cal_DoublePendulum_AnaleAccel_theta(y);
        PenduiumDerived k2 = Cal_DoublePendulum_AnaleAccel_theta(AddScaled(y, k1, dt*0.5));   
        PenduiumDerived k3 = Cal_DoublePendulum_AnaleAccel_theta(AddScaled(y, k2, dt*0.5));
        PenduiumDerived k4 = Cal_DoublePendulum_AnaleAccel_theta(AddScaled(y, k3, dt));
       
        PenduiumState yNext = new PenduiumState();
        yNext.angle1 = y.angle1 + (dt / 6) * (k1.d_angle1 + 2 * k2.d_angle1 + 2 * k3.d_angle1 + k4.d_angle1);
        yNext.angleVelocity1 = y.angleVelocity1 + (dt / 6) * (k1.d_angleVelocity1 + 2 * k2.d_angleVelocity1 + 2 * k3.d_angleVelocity1 + k4.d_angleVelocity1);
        yNext.angle2 = y.angle2 + (dt / 6) * (k1.d_angle2 + 2 * k2.d_angle2 + 2 * k3.d_angle2 + k4.d_angle2);
        yNext.angleVelocity2 = y.angleVelocity2 + (dt / 6) * (k1.d_angleVelocity2 + 2 * k2.d_angleVelocity2 + 2 * k3.d_angleVelocity2 + k4.d_angleVelocity2);

        double coss = MathUtility.Cos(2 * (yNext.angle1-yNext.angle2));
        return yNext;
    }

    /// <summary>
    /// RK4 계산 함수 F
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public PenduiumDerived Cal_DoublePendulum_AnaleAccel_theta(PenduiumState y)
    {
        return PhysicsFormula.DoublePendulum_AnaleAccel(l1,l2,m1,m2,y);
    }

    /// <summary>
    /// 헬퍼 함수
    /// 상태 + 도함수 * h → 새 상태
    /// </summary>
    /// <param name="y"></param>
    /// <param name="k"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    PenduiumState AddScaled(PenduiumState y, PenduiumDerived k, double dt)
    {
        // 4성분 각각: y.angleX + dt * k.d_angleX
        y.angle1 = y.angle1 + dt * k.d_angle1;
        y.angleVelocity1 = y.angleVelocity1 + dt * k.d_angleVelocity1;
        y.angle2 = y.angle2 + dt * k.d_angle2;
        y.angleVelocity2 = y.angleVelocity2 + dt* k.d_angleVelocity2;

        return y;
    }

    /// <summary>
    /// 위치 설정
    /// </summary>
    public void SetPosition(PenduiumState state)
    {
        Vector3 pos1 = new Vector3((float)(l1*MathUtility.Sin(state.angle1)), (float)(-l1 * MathUtility.Cos(state.angle1)), 0);
        Vector3 pos2 = new Vector3((float)(l1 * MathUtility.Sin(state.angle1)+ l2 * MathUtility.Sin(state.angle2))
            , (float)(-l1 * MathUtility.Cos(state.angle1) - l2 * MathUtility.Cos(state.angle2)), 0);
        // (x, y, z) 좌표에 점 그리기
        lineRenderer.SetPosition(0, pivot);
        lineRenderer.SetPosition(1, pos1);
        
        lineRenderer.SetPosition(2, pos2);

    }
    double Energy(PenduiumState s)
    {
        double T = 0.5 * (m1 + m2) * l1 * l1 * s.angleVelocity1 * s.angleVelocity1
                 + 0.5 * m2 * l2 * l2 * s.angleVelocity2 * s.angleVelocity2
                 + m2 * l1 * l2 * s.angleVelocity1 * s.angleVelocity2 * MathUtility.Cos(s.angle1 - s.angle2);
        double V = -(m1 + m2) * ConstUtility.gravity * l1 * MathUtility.Cos(s.angle1)
                 - m2 * ConstUtility.gravity * l2 * MathUtility.Cos(s.angle2);
        return T + V;
    }
    void TestCos()
    {
        double[] testX = { 0, 0.5, 1.0, 1.57, 2.0, 3.0, 3.14, -2.0, -3.0, 5.0, 10.0, 50.0 };
        foreach (double x in testX)
        {
            double mine = MathUtility.Cos(x);
            double real = System.Math.Cos(x);
            double err = System.Math.Abs(mine - real);
            Debug.Log($"x={x}: 내값={mine:F8}, 표준={real:F8}, 오차={err:E3}");
        }
    }
}
