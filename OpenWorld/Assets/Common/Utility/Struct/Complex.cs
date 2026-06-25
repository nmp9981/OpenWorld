[System.Serializable]
public struct Complex
{
    public double x, y;

    public Complex(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// 복소수의 사칙연산
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Complex operator +(Complex a, Complex b)
        => new Complex(a.x + b.x, a.y + b.y);
    public static Complex operator -(Complex a, Complex b)
        => new Complex(a.x - b.x, a.y - b.y);
    public static Complex operator *(Complex a, double b)
        => new Complex(a.x * b, a.y * b);
    public static Complex operator *(Complex a, Complex b)
        => new Complex(a.x * b.x-a.y*b.y, a.y * b.x+a.x*b.y);
    public static Complex operator /(Complex a, double b)
        => new Complex(a.x / b, a.y / b);
    public static Complex operator /(Complex a, Complex b)
        => new Complex((a.x * b.x+a.y*b.y)/(b.x*b.x+b.y*b.y), (a.y * b.x - a.x * b.y) / (b.x * b.x + b.y * b.y));


    /// <summary>
    /// 켤레 복소수
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    public static Complex ComplexConjugate(Complex a)
    {
        return new Complex(a.x, -a.y);
    }

    /// <summary>
    /// 복소수 영
    /// </summary>
    /// <returns></returns>
    public static Complex ZeroComplex()
    {
        return new Complex(0, 0);
    }

    /// <summary>
    /// 크기
    /// </summary>
    /// <returns></returns>
    public double Magnitude()
    {
        double square = x * x + y * y;
        return MathUtility.Sqrt(square);
    }

    /// <summary>
    /// 정규화
    /// </summary>
    /// <returns></returns>
    public Complex Normalized()
    {
        double mag = Magnitude();

        if (mag < ConstUtility.Epcilon12) return new Complex(x, y);

        Complex norm = new Complex(x / mag, y / mag);
        return norm;
    }
}
