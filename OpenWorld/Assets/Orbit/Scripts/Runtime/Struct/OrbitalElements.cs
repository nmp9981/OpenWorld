using System;

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

/// <summary>시간에 따라 전파 가능한 케플러 궤도. 타원 전용.</summary>
public readonly struct Orbit
{
    public readonly double P;      // 반통경 [km]
    public readonly double E;      // 이심률
    public readonly double I;      // 경사각 [rad]
    public readonly double Raan;   // 승교점 적경 [rad]
    public readonly double Argp;   // 근점 편각 [rad]
    public readonly double M0;     // 에포크에서의 평균근점이각 [rad]
    public readonly double Epoch;  // 에포크 시각 [s]
    public readonly CentralBody Body;

    public double SemiMajorAxis => P / ((1.0 - E) * (1.0 + E));
    public double MeanMotion => MathUtility.Sqrt(Body.Mu / (SemiMajorAxis * SemiMajorAxis * SemiMajorAxis));
    public double Period => 2.0 * ConstUtility.PI / MeanMotion;

    public Orbit(CentralBody body, OrbitalElements oe, double epoch = 0.0)
    {
        if (oe.e < 0.0 || oe.e >= 1.0)
            throw new ArgumentOutOfRangeException(nameof(oe.e), "타원 궤도만 지원 (0 <= e < 1)");
        if (oe.p <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(oe.p), "p > 0 이어야 함");

        Body = body;
        P = oe.p; E = oe.e; I = oe.i; Raan = oe.raan; Argp = oe.argp;
        Epoch = epoch;
        M0 = Anomaly.TrueToMean(oe.nu, oe.e);   // ν는 여기서 버려짐
    }

    /// <summary>시각 t에서의 평균근점이각.</summary>
    public double MeanAnomalyAt(double t) => M0 + MeanMotion * (t - Epoch);

    /// <summary>시각 t에서의 궤도요소 (ν가 갱신된 스냅샷).</summary>
    public OrbitalElements ElementsAt(double t) => new OrbitalElements
    {
        p = P,
        e = E,
        i = I,
        raan = Raan,
        argp = Argp,
        nu = Anomaly.MeanToTrue(MeanAnomalyAt(t), E),
        Geometry = OrbitGeometry.General
    };

    /// <summary>시각 t에서의 상태벡터.</summary>
    public StateVector StateAt(double t) => OrbitConverter.ToState(Body, ElementsAt(t));

    /// <summary>상태벡터로부터 궤도 생성.</summary>
    public static Orbit FromState(CentralBody body, StateVector sv, double epoch = 0.0)
        => new Orbit(body, OrbitConverter.ToElements(body, sv), epoch);
}