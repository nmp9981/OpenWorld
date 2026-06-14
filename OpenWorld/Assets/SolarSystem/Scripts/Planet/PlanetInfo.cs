using System;
using UnityEngine;

public class PlanetInfo : MonoBehaviour
{
    public SolarInfo centerPlanet;
    public bool isStar;

    public string planetName;
    public double Mass;
    public double Radius;
    public double eccentricity;//이심율

    public Vector3D position;
    public Vector3D universialForce;
    public Vector3D accel;
    public Vector3D velocity;
    public Vector3D dist;

    public Vector3D orbitNormalDir;

    //적분기용 변수
    public Vector3D accelOld;
    public Vector3D accelNew;

    //라인렌더러
    private int pointIdx=0;
    private int maxPointIndex = 9999;
    [SerializeField] private LineRenderer lineRenderer;

    void Start()
    {
        SettingLineRenderer();
        SettingPlanetInfo();
    }

    /// <summary>
    /// 라인 렌더러 세팅
    /// </summary>
    void SettingLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = maxPointIndex; // 점의 개수 1개로 설정
        lineRenderer.startWidth = 1f; // 점의 크기 (시작)
        lineRenderer.endWidth = 1f;   // 점의 크기 (끝)
    }

    /// <summary>
    /// 행성 초기 정보 세팅
    /// </summary>
    public void SettingPlanetInfo()
    {
        this.position = new Vector3D(this.transform.position.x, 0, this.transform.position.z);
        dist = centerPlanet.position - this.position;
        orbitNormalDir = new Vector3D(0, 1, 0);
        velocity = isStar ? Vector3D.ZeroVector() : SettingInitVelocity();
    }

    /// <summary>
    /// 초기 속도 설정
    /// </summary>
    public Vector3D SettingInitVelocity()
    {
        universialForce = PhysicsFormula.Force_UniversalGravitation(dist, centerPlanet.Mass, Mass);
        accel = PhysicsFormula.Accel_From_Force(universialForce, Mass);
        double initVelocity = MathUtility.Sqrt(accel.Magnitude() * dist.Magnitude());
        Vector3D initVelocityDir = Vector3D.Cross(orbitNormalDir,dist).Normalized();
        return initVelocityDir*initVelocity*eccentricity;
    }

    /// <summary>
    /// 총 가속도
    /// </summary>
    public Vector3D TotalAccel()
    {
        Vector3D totalAccel = Vector3D.ZeroVector();

        foreach(var planet in PlanerManager.Instance._planetList)
        {
            //자기자신은 패스
            if (planet==this) continue;

            Vector3D distBetween = planet.position - this.position;
            Vector3D force = PhysicsFormula.Force_UniversalGravitation(distBetween, planet.Mass, Mass);
            
            totalAccel += PhysicsFormula.Accel_From_Force(force, Mass);
        }
        return totalAccel;
    }

    /// <summary>
    /// 위치 설정
    /// </summary>
    public void SetPosition(Vector3D position)
    {
        this.gameObject.transform.position = new Vector3((float)position.x,(float)position.y,(float) position.z);
        // (x, y, z) 좌표에 점 그리기
        lineRenderer.SetPosition(pointIdx%maxPointIndex, this.gameObject.transform.position);
        pointIdx += 1;
    }
}
