using UnityEngine;

public static class UnityConversion
{
    //Vector3
    public static Vector3 ToUnity(this Vector3D v)
    {
        return new Vector3((float)v.x, (float)v.y, (float)v.z);
    }

    public static Vector3D ToDouble(this Vector3 v)
    {
        return new Vector3D(v.x, v.y, v.z);
    }

    //Quaternion
    public static Quaternion ToUnity(this CustomQuaternion q)
    {
        return new Quaternion((float)q.vec.x, (float)q.vec.y, (float)q.vec.z, (float)q.scala);
    }
    public static CustomQuaternion ToDouble(this Quaternion q)
    {
        return new CustomQuaternion(q.w, new Vector3D(q.x,q.y,q.z));
    }

    //Matrix
    public static Matrix4x4 ToUnity(this Matrix3x3D m)
    {
        Matrix4x4 result = Matrix4x4.identity;
        result.m00 = (float)m.m00; result.m01 = (float)m.m01; result.m02 = (float)m.m02;
        result.m10 = (float)m.m10; result.m11 = (float)m.m11; result.m12 = (float)m.m12;
        result.m20 = (float)m.m20; result.m21 = (float)m.m21; result.m22 = (float)m.m22;
        return result;
    }

    //Transform
    public static void ApplyTo(this TransformD transform, Transform target)
    {
        target.SetPositionAndRotation(transform.position.ToUnity(), transform.rotation.ToUnity());
    }
    public static TransformD ToDouble(this Transform transform)
    {
        return new TransformD(transform.position.ToDouble(), transform.rotation.ToDouble());
    }

    //´ë±Ô¸ð ÁÂÇ¥
    public static Vector3 ToUnityRelative(this Vector3D v, Vector3D origin)
    {
        return (v-origin).ToUnity();
    }
}
