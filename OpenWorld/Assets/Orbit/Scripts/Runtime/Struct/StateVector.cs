public readonly struct StateVector {
    public readonly Vector3D Position;   // km
    public readonly Vector3D Velocity;   // km/s

    public StateVector(Vector3D position, Vector3D velocity)
    {
        Position = position;
        Velocity = velocity;
    }
}
