using UnityEngine;

public class SolarInfo : MonoBehaviour
{
    public double Mass;
    public double Radius;

    public Vector3D position;

    private void Awake()
    {
        position = new Vector3D(0, 0, 0);
    }
}
