using System;
using UnityEngine;

public class PlanetInfo : MonoBehaviour
{
    public SolarInfo centerPlanet;

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

    // Update is called once per frame
    void FixedUpdate()
    {
        dist = centerPlanet.position - this.position;
        universialForce = PhysicsFormula.Force_UniversalGravitation(dist, centerPlanet.Mass, Mass);
        accel = PhysicsFormula.Accel_From_Force(universialForce,Mass);
        velocity += MathUtility.Integrate(accel, Time.fixedTimeAsDouble);
        position += MathUtility.Integrate(velocity, Time.fixedTimeAsDouble);
        SetPosition(position);
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
    /// 위치 설정
    /// </summary>
    private void SetPosition(Vector3D position)
    {
        Debug.Log("위치 "+position.x+" "+position.y+" "+position.z);

        this.gameObject.transform.position = new Vector3((float)position.x,(float)position.y,(float) position.z);
    }
}
