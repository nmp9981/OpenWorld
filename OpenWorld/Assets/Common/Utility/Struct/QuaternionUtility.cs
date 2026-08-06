using UnityEngine;


/// <summary>
/// 쿼터니언 정의
/// </summary>
[System.Serializable]
public struct CustomQuaternion
{
    //성분(스칼라 + 벡터)
    public double scala;
    public Vector3D vec;

    //생성자
    public CustomQuaternion(double scala, Vector3D vec)
    {
        this.scala = scala;
        this.vec = vec;
    }

    //연산자
    public static CustomQuaternion operator +(CustomQuaternion a, CustomQuaternion b)
        => new CustomQuaternion(a.scala + b.scala, a.vec+ b.vec);
    public static CustomQuaternion operator -(CustomQuaternion a, CustomQuaternion b)
        => new CustomQuaternion(a.scala - b.scala, a.vec - b.vec);
    public static CustomQuaternion operator -(CustomQuaternion a)
        => new CustomQuaternion(-a.scala, -a.vec);
    public static CustomQuaternion operator *(CustomQuaternion a, CustomQuaternion b)
        => new CustomQuaternion(
            a.scala * b.scala - Vector3D.Dot(a.vec, b.vec),
            a.scala * b.vec + b.scala * a.vec + Vector3D.Cross(a.vec, b.vec)
            );
    public static CustomQuaternion operator *(CustomQuaternion q, double d)
        => new CustomQuaternion(q.scala * d, q.vec * d);
    public static CustomQuaternion operator *(double d,CustomQuaternion q)
        => new CustomQuaternion(q.scala * d, q.vec * d);
    //로컬 공간의 축을 월드 공간으로 회전
    public static Vector3D operator *(CustomQuaternion q, Vector3D v)
    {
        // 벡터를 quaternion으로 변환
        CustomQuaternion qv = new CustomQuaternion(0.0, v);
        // 회전: q * v * q^-1
        CustomQuaternion result = q * qv * q.Conjugate;
        return result.vec;
    }
    public static CustomQuaternion operator /(CustomQuaternion q, double d)
        => new CustomQuaternion(q.scala / d, q.vec / d);

    //크기
    public double SqrMagnitude => scala * scala + Vector3D.Dot(vec, vec);
    public double Magnitude => MathUtility.Sqrt(scala * scala + Vector3D.Dot(vec, vec));
    public CustomQuaternion Normalized => new CustomQuaternion(scala / Magnitude, vec / Magnitude);

    //켤레
    public CustomQuaternion Conjugate => new CustomQuaternion(scala, new Vector3D(-vec.x, -vec.y, -vec.z));

    //단위 쿼터니언
    public static CustomQuaternion Identity=>new CustomQuaternion(1.0, new Vector3D(0, 0, 0));

    /// <summary>
    /// 선형 보간
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static CustomQuaternion Lerp(CustomQuaternion a, CustomQuaternion b, double t)
    {
        CustomQuaternion result = new CustomQuaternion(
            a.scala * (1.0 - t) + b.scala * t,
            a.vec * (1.0 - t) + b.vec * t
        );
        return result.Normalized;
    }
    /// <summary>
    /// 구면 선형 보간. 최단 경로로 보간한다.
    /// </summary>
    public static CustomQuaternion Slerp(CustomQuaternion a, CustomQuaternion b, double t)
    {
        a = a.Normalized;
        b = b.Normalized;

        double cosOmega = Dot(a, b);

        // double cover: 먼 길 방지
        if (cosOmega < 0.0)
        {
            b = -b;
            cosOmega = -cosOmega;
        }

        const double SLERP_THRESHOLD = 0.9995;

        if (cosOmega > SLERP_THRESHOLD)
        {
            // 거의 동일한 회전 -> Nlerp 폴백
            CustomQuaternion r = a + (b - a) * t;
            return r.Normalized;
        }

        cosOmega = MathUtility.ClampValue(cosOmega, -1.0, 1.0);
        double omega = MathUtility.ArkCos(cosOmega);
        double sinOmega = MathUtility.Sin(omega);

        double wa = MathUtility.Sin((1.0 - t) * omega) / sinOmega;
        double wb = MathUtility.Sin(t * omega) / sinOmega;

        return a * wa + b * wb;
    }

    //내적
    public static double Dot(CustomQuaternion a, CustomQuaternion b)
    => a.scala * b.scala + Vector3D.Dot(a.vec, b.vec);

    /// <summary>
    /// b를 a와 같은 반구로 정렬. 회전 자체는 변하지 않는다(double cover).
    /// </summary>
    public static CustomQuaternion EnsureShortestPath(CustomQuaternion a, CustomQuaternion b)
        => Dot(a, b) < 0.0 ? -b : b;

    /// <summary>
    /// 두 회전 사이 최단 각도 (라디안, [0, PI])
    /// </summary>
    public static double Angle(CustomQuaternion a, CustomQuaternion b)
    {
        CustomQuaternion d = a.Conjugate * b;      // 단위 전제: 역원 = 켤레
        double vLen = d.vec.Magnitude();
        double wAbs = MathUtility.Abs(d.scala);    // 절댓값 = 최단 경로
        return 2.0 * MathUtility.ArkTan2(vLen, wAbs);
    }

    /// <summary>
    /// 빠른 근사판. 정밀도가 필요 없는 broad-phase 용도.
    /// </summary>
    public static double AngleFast(CustomQuaternion a, CustomQuaternion b)
    {
        double d = MathUtility.Abs(Dot(a, b));
        return 2.0 * MathUtility.ArkCos(MathUtility.ClampValue(d, -1.0, 1.0));
    }
}


public class QuaternionUtility
{
    //3*3행렬을 쿼터니언으로 변환
    public static CustomQuaternion Mat3ToQuaternion(Matrix3x3D m)
    {
        //대각합
        double trace = m.Trace();

        if (trace > 0.0)
        {
            double s = MathUtility.Sqrt(1.0 + trace) * 0.5;   // s = w
            double inv = 0.25 / s;
            return new CustomQuaternion(s, new Vector3D(
                (m.m21 - m.m12) * inv,
                (m.m02 - m.m20) * inv,
                (m.m10 - m.m01) * inv));
        }
        else if (m.m00 >= m.m11 && m.m00 >= m.m22)
        {
            double s = MathUtility.Sqrt(1.0 + m.m00 - m.m11 - m.m22) * 0.5;   // s = x
            double inv = 0.25 / s;
            return new CustomQuaternion((m.m21 - m.m12) * inv, new Vector3D(
                s,
                (m.m01 + m.m10) * inv,
                (m.m02 + m.m20) * inv));
        }
        else if (m.m11 >= m.m22)
        {
            double s = MathUtility.Sqrt(1.0 - m.m00 + m.m11 - m.m22) * 0.5;   // s = y
            double inv = 0.25 / s;
            return new CustomQuaternion((m.m02 - m.m20) * inv, new Vector3D(
                (m.m01 + m.m10) * inv,
                s,
                (m.m12 + m.m21) * inv));
        }
        else
        {
            double s = MathUtility.Sqrt(1.0 - m.m00 - m.m11 + m.m22) * 0.5;   // s = z
            double inv = 0.25 / s;
            return new CustomQuaternion((m.m10 - m.m01) * inv, new Vector3D(
                (m.m02 + m.m20) * inv,
                (m.m12 + m.m21) * inv,
                s));
        }
    }

    /// <summary>
    /// 쿼터니언 -> 3x3 회전행렬. 단위 쿼터니언이 아니면 내부에서 정규화한다.
    /// </summary>
    public static Matrix3x3D QuaternionToMat3(CustomQuaternion q)
    {
        double sq = q.SqrMagnitude;
        if (sq < ConstUtility.Epcilon12) return Matrix3x3D.Identity;

        // 비단위 쿼터니언 대응: s = 2/|q|^2 로 스케일 흡수
        double s = 2.0 / sq;

        double w = q.scala, x = q.vec.x, y = q.vec.y, z = q.vec.z;

        double xs = x * s, ys = y * s, zs = z * s;
        double wx = w * xs, wy = w * ys, wz = w * zs;
        double xx = x * xs, xy = x * ys, xz = x * zs;
        double yy = y * ys, yz = y * zs, zz = z * zs;

        return new Matrix3x3D(
            1.0 - (yy + zz), xy - wz, xz + wy,
            xy + wz, 1.0 - (xx + zz), yz - wx,
            xz - wy, yz + wx, 1.0 - (xx + yy));
    }

    //역수
    public static CustomQuaternion Inverse(CustomQuaternion q)
    {
        return q.Conjugate / q.SqrMagnitude;
    }

    /// <summary>
    /// 오일러 각 -> 쿼티니언
    /// </summary>
    /// <param name="eulerAngle"></param>
    /// <returns></returns>
    public static CustomQuaternion Euler(double x, double y, double z)
    {
        double radX = x * ConstUtility.Deg2Rad;
        double radY = y * ConstUtility.Deg2Rad;
        double radZ = z * ConstUtility.Deg2Rad;

        CustomQuaternion qx = AxisAngle(new Vector3D(1, 0, 0), radX);
        CustomQuaternion qy = AxisAngle(new Vector3D(0, 1, 0), radY);
        CustomQuaternion qz = AxisAngle(new Vector3D(0, 0, 1), radZ);

        // Unity style rotation order
        CustomQuaternion q = qy * (qx * qz);

        return q.Normalized;
    }

    /// <summary>
    /// 축 각도 구하기
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static CustomQuaternion AxisAngle(Vector3D axis, double angle)
    {
        axis.Normalized();

        double half = angle * 0.5;

        double s = MathUtility.Sin(half);
        double c = MathUtility.Cos(half);

        CustomQuaternion q;

        q.vec.x = axis.x * s;
        q.vec.y = axis.y * s;
        q.vec.z = axis.z * s;
        q.scala = c;

        return q;
    }

    /// <summary>
    /// 회전 적분
    /// </summary>
    /// <param name="rot"></param>
    /// <param name="corr"></param>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static CustomQuaternion IntegrateRotation(CustomQuaternion rot, Vector3D corr, double dt)
    {
        CustomQuaternion omegaQ = new CustomQuaternion(0.0, corr);
        CustomQuaternion dq = rot * omegaQ;
        CustomQuaternion nextRot = rot + dq * (0.5 * dt);
        return nextRot.Normalized;
    }
    /// <summary>
    /// q 회전 중에서 주어진 axis 방향으로 “얼마나 회전했는가” (signed angle)
    /// </summary>
    /// <param name="q"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public static double TwistAngle(CustomQuaternion q, Vector3D axis)
    {
        // 반드시 정규화
        Vector3D n = axis.Normalized();

        // quaternion vector part
        Vector3D v = q.vec;

        // twist 성분 추출 (projection)
        Vector3D vTwist = n * Vector3D.Dot(v, n);

        // twist quaternion
        CustomQuaternion qTwist = new CustomQuaternion(q.scala, vTwist);
        qTwist = qTwist.Normalized;

        // angle = 2 * acos(w)
        double angle = 2.0* MathUtility.ArkCos(MathUtility.ClampValue(qTwist.scala, -1, 1));

        // 부호 결정 (축 방향)
        double sign = Vector3D.Dot(vTwist, n) >= 0.0f ? 1.0 : -1.0;
        return angle * sign;
    }

    /// <summary>
    /// 축 기준 theta만큼 회전한 쿼터니언
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public static CustomQuaternion AngleAxis(double angle, Vector3D axis)
    {
        axis = axis.Normalized();
        double half = angle * 0.5;

        double sinHalf = MathUtility.Sin(half);
        double cosHalf = MathUtility.Cos(half);

        Vector3D axisRotVec = new Vector3D(axis.x * sinHalf, axis.y * sinHalf, axis.z * sinHalf);
        return new CustomQuaternion { scala = cosHalf, vec = axisRotVec };
    }

}
