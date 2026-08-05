using System.Security.Principal;
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
    public Vector3D Col0 => new Vector3D(m00, m10, m20);
    public Vector3D Col1 => new Vector3D(m01, m11, m21);
    public Vector3D Col2 => new Vector3D(m02, m12, m22);

    public static Matrix3x3D FromRows(Vector3D r0, Vector3D r1, Vector3D r2)
        => new Matrix3x3D(r0.x, r0.y, r0.z,
            r1.x, r1.y, r1.z,
            r2.x, r2.y, r2.z);
    public static Matrix3x3D FromCols(Vector3D c0, Vector3D c1, Vector3D c2)
        => new Matrix3x3D(c0.x, c1.x, c2.x,
            c0.y, c1.y, c2.y,
            c0.z, c1.z, c2.z);

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
    public static Vector3D operator *(Matrix3x3D a, Vector3D b)
        => new Vector3D(a.m00 * b.x + a.m01 * b.y + a.m02 * b.z,
            a.m10 * b.x + a.m11 * b.y + a.m12 * b.z,
            a.m20 * b.x + a.m21 * b.y + a.m22 * b.z);
    public static Vector3D operator *(Vector3D a, Matrix3x3D b)
        => new Vector3D(a.x * b.m00 + a.y * b.m10 + a.z * b.m20,
            a.x * b.m01 + a.y * b.m11 + a.z * b.m21,
            a.x * b.m02 + a.y * b.m12 + a.z * b.m22);
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
    public static bool Approximately(in Matrix3x3D a, in Matrix3x3D b, double eps = 1e-12)
    => MathUtility.Abs(a.m00 - b.m00) < eps && MathUtility.Abs(a.m01 - b.m01) < eps
    && MathUtility.Abs(a.m02 - b.m02) < eps && MathUtility.Abs(a.m10 - b.m10) < eps
    && MathUtility.Abs(a.m11 - b.m11) < eps && MathUtility.Abs(a.m12 - b.m12) < eps
    && MathUtility.Abs(a.m20 - b.m20) < eps && MathUtility.Abs(a.m21 - b.m21) < eps
    && MathUtility.Abs(a.m22 - b.m22) < eps;

    //Nan, Infinity 검사
    public bool IsNaN()
    => double.IsNaN(m00) || double.IsNaN(m01) || double.IsNaN(m02)
    || double.IsNaN(m10) || double.IsNaN(m11) || double.IsNaN(m12)
    || double.IsNaN(m20) || double.IsNaN(m21) || double.IsNaN(m22);

    public bool IsFinite()
        => IsFiniteD(m00) && IsFiniteD(m01) && IsFiniteD(m02)
        && IsFiniteD(m10) && IsFiniteD(m11) && IsFiniteD(m12)
        && IsFiniteD(m20) && IsFiniteD(m21) && IsFiniteD(m22);
    private static bool IsFiniteD(double d) => !double.IsNaN(d) && !double.IsInfinity(d);

    //성분 중 최대 절댓값
    public double MaxAbs()
    {
        double r = MathUtility.Abs(m00);
        double t;
        t = MathUtility.Abs(m01); if (t > r) r = t;
        t = MathUtility.Abs(m02); if (t > r) r = t;
        t = MathUtility.Abs(m10); if (t > r) r = t;
        t = MathUtility.Abs(m11); if (t > r) r = t;
        t = MathUtility.Abs(m12); if (t > r) r = t;
        t = MathUtility.Abs(m20); if (t > r) r = t;
        t = MathUtility.Abs(m21); if (t > r) r = t;
        t = MathUtility.Abs(m22); if (t > r) r = t;
        return r;
    }

    //영행렬, 단위행렬
    public static readonly Matrix3x3D Zero = default;
    public static readonly Matrix3x3D Identity = new Matrix3x3D(1,0,0,0,1,0,0,0,1);
    //Nan
    public static readonly Matrix3x3D Nan = new Matrix3x3D(double.NaN, double.NaN, double.NaN,
        double.NaN, double.NaN, double.NaN,
        double.NaN, double.NaN, double.NaN);

    //대각합
    public double Trace() => m00 + m11 + m22;

    //행렬식
    public double Determinamt() => Vector3D.ScalarTriple(Row0,Row1,Row2);

    //전치 행렬
    public Matrix3x3D Transpose() => new Matrix3x3D(m00, m10, m20,
        m01, m11, m21,
        m02, m12, m22);

    //A^T=-A 판정
    public bool IsSkewSymmetric(double eps = 1e-12)
    {
        double s = MathUtility.Max(MaxAbs(), 1.0) * eps;
        return MathUtility.Abs(m00) < s
            && MathUtility.Abs(m11) < s
            && MathUtility.Abs(m22) < s
            && MathUtility.Abs(m01 + m10) < s
            && MathUtility.Abs(m02 + m20) < s
            && MathUtility.Abs(m12 + m21) < s;
    }
    // [ω]× → ω  (반대칭 가정)
    public Vector3D FromSkewSymmetric() => new Vector3D(m21, m02, m10);

    //여인수 행렬
    public Matrix3x3D Adjugate() => new Matrix3x3D(
    m11 * m22 - m12 * m21, m02 * m21 - m01 * m22, m01 * m12 - m02 * m11,
    m12 * m20 - m10 * m22, m00 * m22 - m02 * m20, m02 * m10 - m00 * m12,
    m10 * m21 - m11 * m20, m01 * m20 - m00 * m21, m00 * m11 - m01 * m10);

    //역행렬
    public Matrix3x3D Inverse()
    {
        double det = Determinamt();
        if (MathUtility.Abs(det) < ConstUtility.Epcilon12) return Nan;
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

    /// <summary>
    /// 강체 시뮬
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    //반대칭 행렬
    public static Matrix3x3D SkewSymmetric(Vector3D v)
    {
        return new Matrix3x3D(
            0, -v.z, v.y,
            v.z, 0, -v.x,
            -v.y, v.x, 0
        );
    }
    //반대칭 행렬 연산
    public static Matrix3x3D SkewSymmetric(Vector3D v, double scale)
    {
        return new Matrix3x3D(
            0, -v.z * scale, v.y * scale,
            v.z * scale, 0, -v.x * scale,
            -v.y * scale, v.x * scale, 0
        );
    }
    //텐서곱
    public static Matrix3x3D Outer(Vector3D a, Vector3D b)
    {
        return new Matrix3x3D(
            a.x * b.x, a.x * b.y, a.x * b.z,
            a.y * b.x, a.y * b.y, a.y * b.z,
            a.z * b.x, a.z * b.y, a.z * b.z
        );
    }
    // I_world = R I_body Rᵀ
    public static Matrix3x3D Similarity(in Matrix3x3D r, in Matrix3x3D i)
        => r * i * r.Transpose();

    //2차 주소행렬식의 합
    public double SecondIncariant() => (m00 * m11 - m01 * m10) + (m11 * m22 - m12 * m21) + (m00 * m22 - m02 * m20);
    
    // 대칭 행렬 전용. 고유값 오름차순, 고유벡터는 정규직교
    public static bool SymmetricEigen(in Matrix3x3D a, out Vector3D eigenvalues,
                                  out Matrix3x3D eigenvectors)
    {
        double traceA = a.Trace();
        double secondA = a.SecondIncariant();
        double detA = a.Determinamt();
    }
    // QR — 한 번에 끝남
    public Matrix3x3D GramSchmidt()
    {
        Vector3D c0 = Col0.Normalized();
        Vector3D c1 = (Col1 - c0 * Vector3D.Dot(Col1, c0)).Normalized();
        Vector3D c2 = Vector3D.Cross(c0, c1);   // 3차원에서는 외적으로 바로
        return FromCols(c0, c1, c2);
    }
    // 극분해 — 반복법 (Higham 방법)
    public Matrix3x3D PolarDecomposition(out Matrix3x3D p, int maxIter = 32)
    {
        Matrix3x3D u = this;
        for (int i = 0; i < maxIter; i++)
        {
            Matrix3x3D inv = u.Inverse();
            if (inv.IsNaN()) break;
            Matrix3x3D next = (u + inv.Transpose()) * 0.5;
            if (Approximately(next, u, ConstUtility.Epcilon12)) { u = next; break; }
            u = next;
        }
        p = u.Transpose() * this;      // P = UᵀA, 대칭 양정치
        return u;
    }

    //정수 거듭제곱
    public Matrix3x3D Pow(long n)
    {
        if (n == 0) return Identity;
        if (n < 0) return Inverse().Pow(-n);
        
        Matrix3x3D half = Pow(n / 2);
        if (n % 2 == 0) return half * half;
        else return half * half * this;
    }
    // 회전벡터(축×각) → 회전행렬
    public static Matrix3x3D ExpSkew(Vector3D w)
    {
        double theta2 = w.SqrMagnitude();

        // θ → 0: sinθ/θ → 1, (1-cosθ)/θ² → 1/2 (테일러 2차항까지)
        if (theta2 < 1e-16)
        {
            Matrix3x3D k0 = SkewSymmetric(w);
            return Identity + k0 + k0 * k0 * 0.5;
        }

        double theta = MathUtility.Sqrt(theta2);
        Matrix3x3D k = SkewSymmetric(w * (1.0 / theta));   // 단위축으로 정규화
        return Identity
             + k * MathUtility.Sin(theta)
             + (k * k) * (1.0 - MathUtility.Cos(theta));
    }
    // 회전행렬 → 회전벡터
    public static Vector3D LogRotation(in Matrix3x3D r)
    {
        double c = (r.Trace() - 1.0) * 0.5;
        c = MathUtility.ClampValue(c, -1.0, 1.0);          // Acos NaN 방지
        double theta = MathUtility.ArkCos(c);

        if (theta < 1e-8)                              // 항등에 가까움
            return new Vector3D(r.m21 - r.m12, r.m02 - r.m20, r.m10 - r.m01) * 0.5;

        if (theta > 3.14159265)                        // θ ≈ π — 반대칭부가 소실됨
        {
            // (R + E)/2 = ω̂ω̂ᵀ 의 최대 열에서 축을 추출
            Matrix3x3D s = (r + Identity) * 0.5;
            Vector3D axis = s.Col0;
            double best = axis.SqrMagnitude();
            if (s.Col1.SqrMagnitude() > best) { axis = s.Col1; best = axis.SqrMagnitude(); }
            if (s.Col2.SqrMagnitude() > best) { axis = s.Col2; }
            return axis.Normalized() * theta;          // 부호 모호성 존재
        }

        double k = theta / (2.0 * MathUtility.Sin(theta));
        return new Vector3D(r.m21 - r.m12, r.m02 - r.m20, r.m10 - r.m01) * k;
    }
    public static Matrix3x3D Exp(in Matrix3x3D a)
    {
        if (a.IsSkewSymmetric())
            return ExpSkew(new Vector3D(a.m21, a.m02, a.m10));   // 정확한 경로

        // 1) scaling: ‖A/2ˢ‖ ≤ 1/2
        double norm = a.MaxAbs() * 3.0;                 // ‖·‖∞ 상계
        int s = 0;
        while (norm > 0.5 && s < 60) { norm *= 0.5; s++; }
        Matrix3x3D x = a * MathUtility.Pow(0.5, s);

        // 2) Padé [6/6] 근사: e^X ≈ D⁻¹N
        // Padé [6/6] 계수
        const double c0 = 1.0;
        const double c1 = 1.0 / 2.0;
        const double c2 = 5.0 / 44.0;
        const double c3 = 1.0 / 66.0;
        const double c4 = 1.0 / 792.0;
        const double c5 = 1.0 / 15840.0;
        const double c6 = 1.0 / 665280.0;

        Matrix3x3D x2 = x * x, x3 = x2 * x, x4 = x2 * x2, x5 = x4 * x, x6 = x3 * x3;
        // 짝수항: c0·E + c2·X² + c4·X⁴ + c6·X⁶
        Matrix3x3D even = Identity * c0 + x2 * c2 + x4 * c4 + x6 * c6;

        // 홀수항: c1·X + c3·X³ + c5·X⁵  =  (c1·E + c3·X² + c5·X⁴)·X
        Matrix3x3D odd = (Identity * c1 + x2 * c3 + x4 * c5) * x;

        // N = even + odd,  D = even - odd
        Matrix3x3D n = even + odd;
        Matrix3x3D d = even - odd;
        Matrix3x3D result = d.Inverse() * n;

        // 3) squaring
        for (int i = 0; i < s; i++) result = result * result;
        return result;
    }
}
