using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    //카메라 오브젝트
    [SerializeField] Camera mainCamera;

    //축 이동량
    private float hAxis;
    private float vAxis;
    private float yMoveAmount;

    //이동 속도
    [SerializeField] private float moveSpeed;

    //회전 속도
    [SerializeField] private float rotateSpeed;

    //줌 속도
    [SerializeField] private float zoomSpeed;

    private void LateUpdate()
    {
        CameraZoom();
        CameraRotate();
        MoveInput();
        Move();
    }

    /// <summary>
    /// 이동
    /// </summary>
    void MoveInput()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.Q)) yMoveAmount = 1.0f;
        else if (Input.GetKey(KeyCode.E)) yMoveAmount = -1.0f;
        else yMoveAmount = 0.0f;
    }

    //오브젝트가 바라보는 방향으로 키보드 이동
    //ex, 오브젝트가 북서쪽을 향하면 위키 눌렀을 때 북서쪽으로 이동해야함
    private void Move()
    {
        Vector3 moveVec = mainCamera.transform.right * hAxis + mainCamera.transform.forward * vAxis + mainCamera.transform.up * yMoveAmount;
        mainCamera.transform.position += moveVec * moveSpeed * Time.deltaTime;//좌표 이동
    }

    /// <summary>
    /// 줌인
    /// </summary>
    void CameraZoom()
    {
        float distance = Input.GetAxis("Mouse ScrollWheel") * -1 * zoomSpeed;//줌 거리
        if (distance != 0)
        {
            mainCamera.fieldOfView += distance;//화각 변경(줌 배율 변경)
        }
    }
    /// <summary>
    /// 회전
    /// </summary>
    void CameraRotate()
    {
        if (Input.GetMouseButton(1))//마우스 우클릭
        {
            Vector3 rot = mainCamera.transform.rotation.eulerAngles; // 현재 카메라의 각도를 Vector3로 반환
            rot.y += Input.GetAxis("Mouse X") * rotateSpeed; // 마우스 X 위치 * 회전 속도
            rot.x += -1 * Input.GetAxis("Mouse Y") * rotateSpeed; // 마우스 Y 위치 * 회전 속도
            Quaternion q = Quaternion.Euler(rot); // Quaternion으로 변환
            q.z = 0;//z축 고정
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, q, 2f); // 자연스럽게 회전
        }
    }
}
