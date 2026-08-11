using UnityEngine;

public static class MathDGizmos
{
    /// <summary>
    /// 좌표축 표시
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="scale"></param>
    public static void DrawTransform(TransformD transform, float scale = 1.0f)
    {
        Vector3 position = transform.position.ToUnity();
        // Draw axes
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + transform.Right.ToUnity()*scale);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(position, position + transform.Up.ToUnity() * scale);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(position, position + position + transform.Forward.ToUnity() * scale);
    }

    /// <summary>
    /// 관성텐서 주축 (고유벡터 × √고유값)
    /// </summary>
    /// <param name="t"></param>
    /// <param name="inertiaBody"></param>
    public static void DrawInertia(TransformD t, Matrix3x3D inertiaBody)
    {
        if (!Matrix3x3D.SymmetricEigen(inertiaBody, out Vector3D ev, out Matrix3x3D axes))
            return;
    }
}
