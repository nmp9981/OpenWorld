using System;
using UnityEngine;

public class PlanetInfo : MonoBehaviour
{
    public SolarInfo centerPlanet;

    public string planetName;
    public double Mass;
    public double Radius;

    public Vector3D position;
    public Vector3D universialForce;
    public Vector3D accel;
    public Vector3D velocity;
    public Vector3D dist;

    public Vector3D orbitNormalDir;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.position = new Vector3D(this.transform.position.x,0, this.transform.position.z);
        dist = centerPlanet.position - this.position;
        orbitNormalDir = new Vector3D(0, 1, 0);
        velocity = SettingInitVelocity();
    }

    /// <summary>
    /// 초기 속도 설정
    /// </summary>
    Vector3D SettingInitVelocity()
    {
        universialForce = PhysicsFormula.Force_UniversalGravitation(dist, centerPlanet.Mass, Mass);
        accel = PhysicsFormula.Accel_From_Force(universialForce, Mass);
        double initVelocity = MathUtility.Sqrt(accel.Magnitude() * dist.Magnitude());
        Vector3D initVelocityDir = Vector3D.Cross(orbitNormalDir,dist).Normalized();
        return initVelocityDir*initVelocity;
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

            Vector3D dist = planet.position - this.position;
            Vector3D force = PhysicsFormula.Force_UniversalGravitation(dist, planet.Mass, Mass);
            
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
    }
}
