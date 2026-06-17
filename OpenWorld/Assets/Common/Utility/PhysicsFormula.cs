using Unity.Collections;

public static class PhysicsFormula
{
    /// <summary>
    /// 만유인력 힘
    /// </summary>
    /// <returns></returns>
    public static Vector3D Force_UniversalGravitation(Vector3D r, double M, double m)
    {
        if (r.Magnitude() <= ConstUtility.Epcilon12) return Vector3D.ZeroVector();

        double up = M * m * ConstUtility.G;//10^-11을 추후 곱해야함
        double rSize = r.Magnitude();
        double down = rSize*rSize+ConstUtility.Epcilon12;
        double forceSize = up / down;
        Vector3D result = r.Normalized() * forceSize;
        return result;
    }

    /// <summary>
    /// 힘으로부터 가속도 구하기
    /// F=ma
    /// </summary>
    /// <param name="force"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    public static Vector3D Accel_From_Force(Vector3D force,double m)
    {
        return force / m;
    }

    /// <summary>
    /// 운동량 계산
    /// </summary>
    /// <param name="m"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector3D Cal_Momentum(double m,Vector3D v)
    {
        return v * m;
    }
}
