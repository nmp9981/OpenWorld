using UnityEngine;

[System.Serializable]
public struct TransformD
{
    public Vector3D position;
    public CustomQuaternion rotation;

    public TransformD(Vector3D position, CustomQuaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }

    public static TransformD Identity = new TransformD(Vector3D.Zero, CustomQuaternion.Identity);

    //점, 방향 전환

    /// <summary>
    /// 로컬 점->월드
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Vector3D TransformPoint(Vector3D point)
    {
        return rotation * point + position;
    }
    /// <summary>
    /// 로컬 방향->월드
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Vector3D TransformDirection(Vector3D direction)
    {
        return rotation * direction;
    }
    /// <summary>
    /// 월드 점->로컬
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Vector3D InverseTransformPoint(Vector3D point)
    {
        return CustomQuaternion.Inverse(rotation) * (point - position);
    }
    /// <summary>
    /// 월드 방향->로컬
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public Vector3D InverseTransformDirection(Vector3D direction)
    {
        return CustomQuaternion.Inverse(rotation) * direction;
    }

    //군 연산

    //(R^-1, -R^-1*P)
    public TransformD Inverse
    {
        get
        {
            CustomQuaternion invRot = CustomQuaternion.Inverse(rotation);
            Vector3D invPos = invRot * (-position);
            return new TransformD(invPos, invRot);
        }
    }
    /// <summary>
    /// 합성, b->a
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static TransformD operator *(TransformD a, TransformD b)=> new TransformD(a.rotation * b.position+a.position, (a.rotation*b.rotation).Normalized);
    
    /// <summary>
    /// 점변환
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Vector3D operator *(TransformD a, Vector3D b) => a.TransformPoint(b);

    //기저축
    public Vector3D Right=> rotation * Vector3D.Right;
    public Vector3D Up => rotation * Vector3D.Up;
    public Vector3D Forward => rotation * Vector3D.Forward;

    /// <summary>관성 텐서, I_world = R·I_body·Rᵀ</summary>
    public Matrix3x3D TransformInertia(in Matrix3x3D inertia)=> Matrix3x3D.Similarity(QuaternionUtility.QuaternionToMat3(rotation), inertia);

    /// <summary>
    /// 보간
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static TransformD Lerp(TransformD a, TransformD b, double t)
    {
        return new TransformD(Vector3D.Lerp(a.position, b.position, t), CustomQuaternion.Slerp(a.rotation, b.rotation, t));
    }
    /// <summary>
    /// 행렬 변환
    /// </summary>
    /// <returns></returns>
    public Matrix3x3D ToRotationMatrix()
    {
        return QuaternionUtility.QuaternionToMat3(rotation);
    }

    public bool IsFinite()
        => position.IsFinite() && rotation.IsFinite();

    public override string ToString()
        => $"pos({position}) rot({rotation})";

    #region 강체 물리
    /// <summary>두 자세 사이의 상대 변환. from에서 to로 가는 T</summary>
    public static TransformD Delta(TransformD from, TransformD to)=>to*from.Inverse;

    /// <summary>
    /// 각속도로 적분 (ω는 월드 프레임)
    /// </summary>
    /// <param name="omegaWorld"></param>
    /// <param name="dt"></param>
    /// <returns></returns>
    public TransformD IntegrateAngular(Vector3D omegaWorld, double dt)
    {
        CustomQuaternion dq = CustomQuaternion.Exp(omegaWorld * dt);
        return new TransformD(position, (dq * rotation).Normalized);
    }
    #endregion
}
