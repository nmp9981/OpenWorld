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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dist = centerPlanet.position - this.position;
        velocity = SettingInitVelocity();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        universialForce = PhysicsFormula.Force_UniversalGravitation(dist, centerPlanet.Mass, Mass);
        accel = PhysicsFormula.Accel_From_Force(universialForce,Mass);
        velocity += MathUtility.Integrate(accel, Time.fixedTimeAsDouble);
        position += MathUtility.Integrate(velocity, Time.fixedTimeAsDouble);
    }

    /// <summary>
    /// 초기 속도 설정
    /// </summary>
    Vector3D SettingInitVelocity()
    {
        universialForce = PhysicsFormula.Force_UniversalGravitation(dist, centerPlanet.Mass, Mass);
        accel = PhysicsFormula.Accel_From_Force(universialForce, Mass);

        double initVelocity = MathUtility.Sqrt(accel.Magnitude() * dist.Magnitude());
        Vector3D initVelocityDir = accel.Normalized();
        return initVelocityDir*initVelocity;
    }
}
