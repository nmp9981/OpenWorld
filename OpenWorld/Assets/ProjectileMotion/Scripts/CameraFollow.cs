using UnityEngine;

/// <summary>
/// Main Camera 가 대상(공)을 따라다닌다.
/// 시작 시점의 카메라-대상 간격을 유지하고, 지수 감쇠로 부드럽게 따라간다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;          // 따라갈 대상
    [SerializeField] private float sharpness = 8f;      // 클수록 빨리 따라감 (0 이면 즉시)
    [SerializeField] private bool followY = true;       // 높이도 따라갈지

    private Vector3 offset;

    public void SetTarget(Transform t)
    {
        target = t;
        if (target != null) offset = transform.position - target.position;
    }

    /// <summary>보간 없이 즉시 대상 위치(간격 유지)로 옮긴다.</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        if (!followY) desired.y = transform.position.y;
        transform.position = desired;
    }

    private void Start()
    {
        SetTarget(target);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        if (!followY) desired.y = transform.position.y;

        if (sharpness <= 0f)
        {
            transform.position = desired;
            return;
        }

        // 프레임 간격과 무관한 보간 계수: 1 - e^(-k·dt)
        float t = (float)(1.0 - MathUtility.Exp(-sharpness * Time.deltaTime));
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }
}
