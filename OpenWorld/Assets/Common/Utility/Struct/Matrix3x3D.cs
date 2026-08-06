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
        => (b==0)?NaN :new Matrix3x3D(a.m00 / b, a.m01 / b, a.m02 / b,
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
    public static readonly Matrix3x3D NaN = new Matrix3x3D(double.NaN, double.NaN, double.NaN,
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
    /// <summary>
    /// 대칭 행렬 판정: Aᵀ ≈ A
    /// eps 는 상대 오차 기준. 행렬 규모에 비례한 허용치를 사용한다.
    /// </summary>
    public bool IsSymmetric(double eps = 1e-12)
    {
        if (!IsFinite()) return false;

        double s = MathUtility.Max(MaxAbs(), 1.0) * eps;

        return MathUtility.Abs(m01 - m10) < s
            && MathUtility.Abs(m02 - m20) < s
            && MathUtility.Abs(m12 - m21) < s;
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
        if (MathUtility.Abs(det) < ConstUtility.Epcilon12) return NaN;
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
    public static Vector3D SymmetricEigenvalues(in Matrix3x3D a)
    {
        double p1 = a.m01 * a.m01 + a.m02 * a.m02 + a.m12 * a.m12;
        double q = a.Trace() / 3.0;

        // 이미 대각행렬 → p = 0 이므로 나눗셈 불가. 정렬만 해서 반환
        if (p1 <= ConstUtility.Epcilon12 * MathUtility.Max(q * q, 1.0))
        {
            double x = a.m00, y = a.m11, z = a.m22, t;
            if (x > y) { t = x; x = y; y = t; }
            if (y > z) { t = y; y = z; z = t; }
            if (x > y) { t = x; x = y; y = t; }
            return new Vector3D(x, y, z);
        }

        double d0 = a.m00 - q, d1 = a.m11 - q, d2 = a.m22 - q;
        double p2 = d0 * d0 + d1 * d1 + d2 * d2 + 2.0 * p1;   // ‖A - qE‖²_F  (상쇄 없음)
        double p = MathUtility.Sqrt(p2 / 6.0);

        Matrix3x3D b = (a - Identity * q) * (1.0 / p);
        double r = b.Determinamt() * 0.5;

        r = MathUtility.ClampValue(r, -1.0, 1.0);          // 반올림으로 |r|>1 → Acos NaN 방지
        double phi = MathUtility.ArkCos(r) / 3.0;       // φ ∈ [0, π/3]

        const double TWO_PI_3 = 2.0943951023931953;   // 2π/3

        double eMax = q + 2.0 * p * MathUtility.Cos(phi);
        double eMin = q + 2.0 * p * MathUtility.Cos(phi + TWO_PI_3);
        double eMid = 3.0 * q - eMax - eMin;          // 대각합 보존 — 세 번째 cos 보다 정확

        return new Vector3D(eMin, eMid, eMax);        // 오름차순
    }
    // 대칭 행렬 전용. 고유값 오름차순, 고유벡터는 정규직교(오른손 좌표계)
    public static bool SymmetricEigen(in Matrix3x3D a, out Vector3D eigenvalues,
                                      out Matrix3x3D eigenvectors)
    {
        eigenvalues = Vector3D.Zero;
        eigenvectors = Identity;

        if (!a.IsSymmetric()) return false;           // 전제 조건 강제
        if (!a.IsFinite()) return false;

        eigenvalues = SymmetricEigenvalues(a);

        // 양 끝(최소·최대)부터 — 중간 고유값은 중복근 가능성이 가장 높아 불안정
        Vector3D v0, v1, v2;
        bool okMin = NullVector(a - Identity * eigenvalues.x, out v0);
        bool okMax = NullVector(a - Identity * eigenvalues.z, out v2);

        if (okMin && okMax)
        {
            v1 = Vector3D.Cross(v2, v0).Normalized(); // 중간축은 외적으로 → 직교성 자동
            v2 = Vector3D.Cross(v0, v1);              // 재직교화
        }
        else if (okMin)                               // λ₂ = λ₃ 중복
        {
            Vector3D.BuildOrthonormalBasis(v0, out v1, out v2);
        }
        else if (okMax)                               // λ₁ = λ₂ 중복
        {
            Vector3D.BuildOrthonormalBasis(v2, out v0, out v1);
            Vector3D t = v0; v0 = v1; v1 = t;         // v2 가 최대축을 유지하도록 정리
        }
        else                                          // 삼중근 → 등방, 임의 기저
        {
            v0 = Vector3D.Right; v1 = Vector3D.Up; v2 = Vector3D.Forward;
        }

        if (Vector3D.Dot(Vector3D.Cross(v0, v1), v2) < 0.0) v2 = -v2;  // det = +1 강제

        eigenvectors = FromCols(v0, v1, v2);
        return true;
    }

    // (A - λE) 의 영공간 벡터. 행 두 개의 외적 중 가장 긴 것을 채택.
    private static bool NullVector(in Matrix3x3D m, out Vector3D v)
    {
        Vector3D c01 = Vector3D.Cross(m.Row0, m.Row1);
        Vector3D c02 = Vector3D.Cross(m.Row0, m.Row2);
        Vector3D c12 = Vector3D.Cross(m.Row1, m.Row2);

        double s01 = c01.SqrMagnitude(), s02 = c02.SqrMagnitude(), s12 = c12.SqrMagnitude();

        double best = s01; v = c01;
        if (s02 > best) { best = s02; v = c02; }
        if (s12 > best) { best = s12; v = c12; }

        double scale = MathUtility.Max(m.MaxAbs(), 1.0);
        if (best < ConstUtility.Epcilon12 * scale * scale) { v = Vector3D.Zero; return false; }

        v = v.Normalized();
        return true;
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
        // av = sinθ · ω̂  (반대칭부의 절반)
        Vector3D av = new Vector3D(r.m21 - r.m12, r.m02 - r.m20, r.m10 - r.m01) * 0.5;

        double s = av.Magnitude();          // sinθ ≥ 0
        double c = (r.Trace() - 1.0) * 0.5; // cosθ
        double theta = MathUtility.ArkTan2(s, c);   // [0, π], 조건수 양호

        // θ ≈ 0 : av ≈ θω̂ 이므로 그대로
        if (s < 1e-12 && c > 0.0) return av;

        // θ ≈ π : 반대칭부 소실 → (R+E)/2 = ω̂ω̂ᵀ 에서 축 추출
        if (c < 0.0 && s < 1e-6)
        {
            Matrix3x3D m = (r + Identity) * 0.5;
            Vector3D axis = m.Col0;
            double best = axis.SqrMagnitude();
            if (m.Col1.SqrMagnitude() > best) { axis = m.Col1; best = axis.SqrMagnitude(); }
            if (m.Col2.SqrMagnitude() > best) { axis = m.Col2; }
            axis = axis.Normalized();

            // 부호 모호성 해소: av가 아직 살아있으면 그걸로 판정
            if (Vector3D.Dot(axis, av) < 0.0) axis = -axis;
            return axis * theta;
        }
        return av * (theta / s);   // θ/sinθ
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

        Matrix3x3D x2 = x * x, x3 = x2 * x, x4 = x2 * x2, x6 = x3 * x3;
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
