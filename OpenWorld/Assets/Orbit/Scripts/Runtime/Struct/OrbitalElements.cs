/// <summary>
/// 궤도 요소 구조체
/// </summary>
public struct OrbitalElements
{
    public double p, e, i, raan, argp, nu;
    public OrbitGeometry Geometry;
}

/// <summary>
/// 궤도 형상
/// </summary>

public enum OrbitGeometry { General, CircularInclined, EllipticalEquatorial, CircularEquatorial }
