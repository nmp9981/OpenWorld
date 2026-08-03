[System.Serializable]
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
    public static Vector3D operator -(Vector3D a)
        => new Vector3D(-a.x, -a.y, -a.z);
    public static Vector3D operator -(Vector3D a, Vector3D b)
        => new Vector3D(a.x - b.x, a.y - b.y, a.z - b.z);
    public static Vector3D operator *(Vector3D a, double b)
        => new Vector3D(a.x*b, a.y *b, a.z *b);
    public static Vector3D operator *(double b, Vector3D a)
        => new Vector3D(a.x * b, a.y * b, a.z * b);
    public static Vector3D operator /(Vector3D a, double b)
        => new Vector3D(a.x / b, a.y / b, a.z / b);

    //일치 여부
    public static bool operator ==(Vector3D a, Vector3D b)
        => a.x==b.x && a.y==b.y && a.z==b.z;
    public static bool operator !=(Vector3D a, Vector3D b)
        => !(a==b);


    //제로벡터, 원벡터
    public static readonly Vector3D Zero = new Vector3D(0, 0, 0);
    public static readonly Vector3D One = new Vector3D(1,1,1);
    //단위 방향 벡터
    public static readonly Vector3D Right = new Vector3D(1, 0, 0);
    public static readonly Vector3D Left = new Vector3D(-1, 0, 0);
    public static readonly Vector3D Up = new Vector3D(0, 1, 0);
    public static readonly Vector3D Down = new Vector3D(0, -1, 0);
    public static readonly Vector3D Forward = new Vector3D(0, 0, 1);
    public static readonly Vector3D Back = new Vector3D(0, 0, -1);

    public double SqrMagnitude() => x * x + y * y + z * z;// 크기 제곱
    public double Distance(Vector3D a, Vector3D b) => (a - b).Magnitude();// 두 벡터 사이 거리
    public double SqrDistznce(Vector3D a, Vector3D b) => (a - b).SqrMagnitude();// 두 벡터 사이 거리 제곱

    //보간
    public static Vector3D Lerp(Vector3D a, Vector3D b, double t)=> a + (b - a) * t;

    /// <summary>
    /// 크기
    /// </summary>
    /// <returns></returns>
    public double Magnitude()
    {
        double square = x * x + y * y + z * z;
        return MathUtility.Sqrt(square);
    }
   
    #region 정규화
    /// <summary>
    /// 정규화
    /// </summary>
    /// <returns></returns>
    public Vector3D Normalized()
    {
        double mag = Magnitude();

        if(mag < ConstUtility.Epcilon12) return Zero;

        Vector3D norm = new Vector3D(x/mag, y/mag, z/mag);
        return norm;
    }

    /// <summary>
    /// 정규화 시도
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool TryNormalize(out Vector3D result)  // 실패를 감지해야 할 때
    {
        double mag = Magnitude();
        if (mag < ConstUtility.Epcilon12) { result = Zero; return false; }
        result = new Vector3D(x / mag, y / mag, z / mag);
        return true;
    }
    #endregion 정규화

    /// <summary>
    /// 내적
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static double Dot(Vector3D a, Vector3D b)
    {
        return a.x*b.x+a.y*b.y+a.z*b.z;
    }

    /// <summary>
    /// 외적
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Vector3D Cross(Vector3D a, Vector3D b)
    {
        double xi = a.y * b.z - a.z * b.y;
        double yj = -(a.x * b.z - a.z * b.x);
        double zk = a.x * b.y - a.y * b.x;

        return new Vector3D(xi, yj, zk);
    }
    #region 물리용 연산

    #endregion
}