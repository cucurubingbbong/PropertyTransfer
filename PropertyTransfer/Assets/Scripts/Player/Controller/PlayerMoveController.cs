using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMoveController : MonoBehaviour
{
    /// <summary>
    /// 현재 들고있는 무게에 따라 이동속도가 달라집니다
    /// 기본 이동속도 : 
    /// </summary>
    [SerializeField] private float moveSpeed = 0.0f;

    /// <summary>
    /// 점프량
    /// </summary>
    [SerializeField] private float jumpPower = 1.0f;

    [SerializeField] Rigidbody rb;
    private Vector2 currentMoveInput;

    void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnMoveInput += SetDirection;
            InputManager.Instance.OnJumpInput += Jump;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnMoveInput -= SetDirection;
            InputManager.Instance.OnJumpInput -= Jump;
        }
    }

    private void SetDirection(Vector2 moveInput)
    {
        currentMoveInput = moveInput;
    }

    private void Move()
    {
        Vector3 direction = new Vector3(currentMoveInput.x, 0f, currentMoveInput.y);
        direction = transform.TransformDirection(direction);

        Vector3 velocity = direction * moveSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private void Jump()
    {
        rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        Move();
    }
}
