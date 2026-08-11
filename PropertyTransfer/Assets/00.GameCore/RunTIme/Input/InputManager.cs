using GameCore; 
using UnityEngine; 
using System; 

public class InputManager : Singleton<InputManager> 
{ 
    [Header("조작 키")] 
    public KeyCode JumpKeyCode = KeyCode.Space; 
    public KeyCode InterectKeyCode = KeyCode.E; 
    public KeyCode ResetRoomKeyCode = KeyCode.R; 
    public KeyCode SettingMenuKey = KeyCode.Escape; 

    public Vector2 MoveDirection { get; private set; }

    // 이벤트들
    public Action<Vector2> OnMoveInput; // 필요할 경우 사용
    public Action OnJumpInput; 
    public Action OnInterectInput; 
    public Action OnResetRoomInput; 
    public Action OnOpenSettingInput; 

    void Update() 
    { 
        HandleMovement();
        HandleActionKeys();
    } 

    private void HandleMovement()
    {
        Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        
        if (move != MoveDirection)
        {
            MoveDirection = move;
            OnMoveInput?.Invoke(MoveDirection);
        }
    }

    private void HandleActionKeys()
    {
        if(Input.GetKeyDown(JumpKeyCode)) OnJumpInput?.Invoke(); 
        if(Input.GetKeyDown(InterectKeyCode)) OnInterectInput?.Invoke(); 
        if(Input.GetKeyDown(ResetRoomKeyCode)) OnResetRoomInput?.Invoke(); 
        if(Input.GetKeyDown(SettingMenuKey)) OnOpenSettingInput?.Invoke(); 
    }
}