public struct Vector3D
{
    public double x, y, z;

    public Vector3D(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    /// <summary>
    /// 벡터의 사칙연산
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Vector3D operator +(Vector3D a, Vector3D b)
        => new Vector3D(a.x+b.x,a.y+b.y,a.z+b.z);
    public static Vector3D operator -(Vector3D a, Vector3D b)
        => new Vector3D(a.x - b.x, a.y - b.y, a.z - b.z);
    public static Vector3D operator *(Vector3D a, double b)
        => new Vector3D(a.x*b, a.y *b, a.z *b);
    public static Vector3D operator /(Vector3D a, double b)
        => new Vector3D(a.x / b, a.y / b, a.z / b);
    
    /// <summary>
    /// 크기
    /// </summary>
    /// <returns></returns>
    public double Magnitude()
    {
        double square = x * x + y * y + z * z;
        return MathUtility.Sqrt(square);
    }

    /// <summary>
    /// 정규화
    /// </summary>
    /// <returns></returns>
    public Vector3D Normalized()
    {
        double mag = Magnitude();

        if(mag < ConstUtility.Epcilon12) return new Vector3D(x,y,z);

        Vector3D norm = new Vector3D(x/mag, y/mag, z/mag);
        return norm;
    }

    /// <summary>
    /// 내적
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public double Dot(Vector3D a, Vector3D b)
    {
        return a.x*b.x+a.y*b.y+a.z*b.z;
    }

    /// <summary>
    /// 외적
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public Vector3D Cross(Vector3D a, Vector3D b)
    {
        double xi = a.y * b.z - a.z * b.y;
        double yj = a.x * b.z - a.z * b.x;
        double zk = a.x * b.y - a.y * b.x;

        return new Vector3D(xi, -yj, zk);
    }
}