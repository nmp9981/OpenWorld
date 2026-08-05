using UnityEngine;

public struct Matrix3x3D
{
    public double m00, m01, m02;
    public double m10, m11, m12;
    public double m20, m21, m22;

    //행렬 생성자
    public Matrix3x3D(double m00, double m01, double m02,
        double m10, double m11, double m12,
        double m20, double m21, double m22)
    {
        this.m00 = m00;this.m01 = m01;this.m02 = m02;
        this.m10 = m10; this.m11 = m11; this.m12 = m12;
        this.m20 = m20; this.m21 = m21; this.m22 = m22;
    }

    //행벡터
    public Vector3D Row0 => new Vector3D(m00, m01, m02);
    public Vector3D Row1 => new Vector3D(m10, m11, m12);
    public Vector3D Row2 => new Vector3D(m20, m21, m22);

    //열벡터
    public Vector3D col0 => new Vector3D(m00, m10, m20);
    public Vector3D col1 => new Vector3D(m01, m11, m21);
    public Vector3D col2 => new Vector3D(m02, m12, m22);

    /// <summary>
    /// 벡터의 사칙연산
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Matrix3x3D operator +(Matrix3x3D a, Matrix3x3D b)
        => new Matrix3x3D(a.m00 + b.m00, a.m01 + b.m01, a.m02 + b.m02,
            a.m10 + b.m10, a.m11 + b.m11, a.m12 + b.m12,
            a.m20 + b.m20, a.m21 + b.m21, a.m22 + b.m22);
    public static Matrix3x3D operator -(Matrix3x3D a, Matrix3x3D b)
        => new Matrix3x3D(a.m00 - b.m00, a.m01 - b.m01, a.m02 - b.m02,
            a.m10 - b.m10, a.m11 - b.m11, a.m12 - b.m12,
            a.m20 - b.m20, a.m21 - b.m21, a.m22 - b.m22);
    public static Matrix3x3D operator -(Matrix3x3D a)
        => new Matrix3x3D(-a.m00, -a.m01, -a.m02,
            -a.m10, -a.m11, -a.m12,
            -a.m20, -a.m21, -a.m22);
    public static Matrix3x3D operator *(Matrix3x3D a, double b)
        => new Matrix3x3D(a.m00*b, a.m01*b, a.m02*b,
            a.m10*b, a.m11*b, a.m12*b,
            a.m20*b, a.m21*b, a.m22*b);
    public static Matrix3x3D operator *(double b, Matrix3x3D a)
        => new Matrix3x3D(a.m00 * b, a.m01 * b, a.m02 * b,
            a.m10 * b, a.m11 * b, a.m12 * b,
            a.m20 * b, a.m21 * b, a.m22 * b);
    public static Matrix3x3D operator *(Matrix3x3D a, Matrix3x3D b)
        => new Matrix3x3D(a.m00 * b.m00+a.m01*b.m10+a.m02*b.m20, a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21, a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22,
           a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20, a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21, a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22,
            a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20, a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21, a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22);
    public static Matrix3x3D operator /(Matrix3x3D a, double b)
        => (b==0)?Nan :new Matrix3x3D(a.m00 / b, a.m01 / b, a.m02 / b,
            a.m10 / b, a.m11 / b, a.m12 / b,
            a.m20 / b, a.m21 / b, a.m22 / b);

    //일치 여부
    public static bool operator ==(Matrix3x3D a, Matrix3x3D b)
        => a.m00 == b.m00 && a.m01 == b.m01 && a.m02 == b.m02
        && a.m10 == b.m10 && a.m11 == b.m11 && a.m12 == b.m12
        && a.m20 == b.m20 && a.m21 == b.m21 && a.m22 == b.m22;
    public static bool operator !=(Matrix3x3D a, Matrix3x3D b)
        => !(a == b);


    //영행렬, 단위행렬
    public static readonly Matrix3x3D Zero = default;
    public static readonly Matrix3x3D One = new Matrix3x3D(1,0,0,0,1,0,0,0,1);
    //Nan
    public static readonly Matrix3x3D Nan = new Matrix3x3D(double.NaN, double.NaN, double.NaN,
        double.NaN, double.NaN, double.NaN,
        double.NaN, double.NaN, double.NaN);

    //행렬식
    public double Determinamt() => Vector3D.ScalarTriple(Row0,Row1,Row2);

    //전치 행렬
    public Matrix3x3D Transpose() => new Matrix3x3D(m00, m10, m20,
        m01, m11, m21,
        m02, m12, m22);

    //역행렬
    public Matrix3x3D Inverse()
    {
        double det = Determinamt();
        if (det == 0) return Nan;
        double invDet = 1.0 / det;
        return new Matrix3x3D(
            (m11 * m22 - m12 * m21) * invDet,
            (m02 * m21 - m01 * m22) * invDet,
            (m01 * m12 - m02 * m11) * invDet,
            (m12 * m20 - m10 * m22) * invDet,
            (m00 * m22 - m02 * m20) * invDet,
            (m02 * m10 - m00 * m12) * invDet,
            (m10 * m21 - m11 * m20) * invDet,
            (m01 * m20 - m00 * m21) * invDet,
            (m00 * m11 - m01 * m10) * invDet
        );
    }
}
