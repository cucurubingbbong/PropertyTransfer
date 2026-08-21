using GameCore;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Camera playerCamera;

    /// <summary>
    /// 마우스 감도 설정
    /// </summary>
    public float sensitivity = 200.0f;
    private Vector2 mouseInput = Vector2.zero;

    [Header("마우스 최대회전 ")]
    [SerializeField] private Vector2 maxPov = new Vector2(55f , 70f);

    /// <summary>
    /// 카메라 회전 가능 여부
    /// </summary>
    [SerializeField] private bool canRotate = true;

    private void OnEnable()
    {
        UIManager.Instance.InputLock += SetCanRotate;
    }

    private void OnDisable()
    {
        UIManager.Instance.InputLock -= SetCanRotate;
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void Update()
    {
        if(!canRotate) return;
        HandleMouseInput();
    }

    /// <summary>
    /// 마우스 입력을 처리하여 카메라 회전을 제어합니다.
    /// </summary>
    private void HandleMouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        mouseInput.x += mouseX;
        mouseInput.y -= mouseY;
        mouseInput.y = Mathf.Clamp(mouseInput.y, -maxPov.x, maxPov.y);

        playerCamera.transform.localRotation = Quaternion.Euler(mouseInput.y, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, mouseInput.x, 0f);
    }

    private void SetCanRotate(bool flag)
    {
        canRotate = flag;
    }
}
