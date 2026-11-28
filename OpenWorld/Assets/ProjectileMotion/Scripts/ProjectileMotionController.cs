using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 포물선 운동 제어.
/// Space 를 누르면 입력한 속도·각도가 숫자인지 확인한 뒤 발사하고,
/// +x 방향 이동 거리를 소수점 첫째 자리까지 실시간 표시한다.
/// 계산은 Common/Utility 의 MathUtility · IntegratorUtility · ConstUtility 를 쓴다.
/// </summary>
public class ProjectileMotionController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private InputField speedInput;   // 속도 (m/s)
    [SerializeField] private InputField angleInput;   // 각도 (도)
    [SerializeField] private Text distanceText;       // +x 이동 거리
    [SerializeField] private Text statusText;         // 안내·오류 문구

    [Header("Object")]
    [SerializeField] private Transform ball;          // 발사체(Unity 기본 Sphere)

    private Vector3D launchPos;     // 발사 위치
    private Vector3D r;             // 현재 위치
    private Vector3D v;             // 현재 속도
    private bool isFlying;
    private double distanceX;       // +x 이동 거리

    private static readonly Vector3D GravityAccel = new Vector3D(0, -ConstUtility.gravity, 0);

    private void Start()
    {
        if (ball != null)
        {
            // 직접 적분으로 움직이므로 물리 엔진 강체는 쓰지 않는다.
            var rb = ball.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            launchPos = ball.position.ToDouble();
            r = launchPos;
        }

        SetDistance(0);
        SetStatus("속도와 각도를 입력하고 Space 를 누르세요.");
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            TryLaunch();
        else if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            ResetBall();
    }

    /// <summary>
    /// Esc: 공을 처음 위치로 되돌리고 이동 거리 표시를 0.0 m 로 초기화한다.
    /// </summary>
    private void ResetBall()
    {
        if (ball == null) return;

        isFlying = false;
        r = launchPos;
        v = Vector3D.Zero;
        distanceX = 0;
        ball.position = r.ToUnity();
        SetDistance(0);
        SetStatus("처음 위치로 돌아왔습니다. Space 로 발사합니다.");

        // 카메라도 바로 공 위치로 옮긴다(멀리서 천천히 되돌아오지 않게).
        var cam = Camera.main;
        if (cam != null && cam.TryGetComponent(out CameraFollow follow))
            follow.SnapToTarget();
    }

    private void FixedUpdate()
    {
        if (!isFlying) return;

        Vector3D prev = r;
        IntegratorUtility.VelocityVerlet(ref r, ref v, Time.fixedDeltaTime, _ => GravityAccel);

        // 발사 높이 아래로 내려가면 착지. 직전 위치와 선형 보간해 착지점을 맞춘다.
        if (r.y <= launchPos.y)
        {
            double t = (prev.y - launchPos.y) / (prev.y - r.y);
            r = Vector3D.Lerp(prev, r, MathUtility.ClampValue(t, 0, 1));
            r.y = launchPos.y;
            isFlying = false;
            SetStatus("착지했습니다. Space 로 다시 발사합니다.");
        }

        distanceX = r.x - launchPos.x;
        SetDistance(distanceX);
        ball.position = r.ToUnity();
    }

    private void TryLaunch()
    {
        if (ball == null || speedInput == null || angleInput == null)
        {
            SetStatus("Inspector 연결이 비어 있습니다.");
            return;
        }

        // Space 가 입력칸에 들어가는 경우가 있어 공백을 지우고 입력칸 선택을 푼다.
        string speedStr = speedInput.text.Trim();
        string angleStr = angleInput.text.Trim();
        speedInput.text = speedStr;
        angleInput.text = angleStr;
        speedInput.DeactivateInputField();
        angleInput.DeactivateInputField();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (!TryParseNumber(speedStr, out double speed))
        {
            SetStatus("속도가 숫자가 아닙니다.");
            return;
        }
        if (!TryParseNumber(angleStr, out double angleDeg))
        {
            SetStatus("각도가 숫자가 아닙니다.");
            return;
        }

        double rad = MathUtility.ToRadianAngle(angleDeg);
        r = launchPos;
        v = new Vector3D(speed * MathUtility.Cos(rad), speed * MathUtility.Sin(rad), 0);
        ball.position = r.ToUnity();
        distanceX = 0;
        SetDistance(0);

        // 수직 속도가 0 이하이면 바로 착지 판정이 나므로 발사하지 않는다.
        if (v.y <= 0)
        {
            SetStatus("각도는 위쪽(0 초과 180 미만)이어야 합니다.");
            return;
        }

        isFlying = true;
        SetStatus($"발사: 속도 {speed} m/s, 각도 {angleDeg}°");
    }

    private static bool TryParseNumber(string s, out double value)
    {
        bool ok = double.TryParse(s, System.Globalization.NumberStyles.Float,
                                  System.Globalization.CultureInfo.InvariantCulture, out value);
        return ok && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private void SetDistance(double d)
    {
        if (distanceText != null)
            distanceText.text = $"+x 이동 거리: {MathUtility.Round(d, 1):F1} m";
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

}
