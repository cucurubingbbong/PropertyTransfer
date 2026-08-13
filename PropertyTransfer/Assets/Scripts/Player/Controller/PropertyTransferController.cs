using UnityEngine;

/// <summary>
/// 플레이어의 특성 추출 / 부여를 관리한다.
/// </summary>
public class PropertyTransferController : MonoBehaviour
{
    private enum TransferType
    {
        None,
        Extract,
        Grant
    }

    private enum TransferState
    {
        None,
        SelectingProperty,
        Processing
    }

    [SerializeField] private TransferType currentTransferType = TransferType.None;
    [SerializeField] private TransferState currentState = TransferState.None;

    [SerializeField] private PropertyData[] gunPropertyData = new PropertyData[6];
    [SerializeField] private PropertyData selectedPropertyData;
    [SerializeField] private PropertyHolder currentPropertyHolder;

    [SerializeField] private LayerMask propertyLayerMask = 1 << 6;
    [SerializeField] private float maxRayDistance = 20f;

    /// <summary>
    /// UI스크린 오브젝트
    /// </summary>
    [SerializeField] private PropertyUI propertyUI = null;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        TryGetInput();
        ProcessTransfer();
    }

    /// <summary>
    /// 추출 / 부여 입력을 확인한다.
    /// </summary>
    private void TryGetInput()
    {
        if (Input.GetKeyDown(KeyCode.Q)) { Cancel(); return; }
        if (currentState != TransferState.None) return;

        if (Input.GetMouseButtonDown(0)) TryStartTransfer(TransferType.Extract);
        else if (Input.GetMouseButtonDown(1)) TryStartTransfer(TransferType.Grant);
    }

    /// <summary>
    /// 대상을 찾아 전달을 시작한다.
    /// </summary>
    private void TryStartTransfer(TransferType transferType)
    {
        PropertyHolder propertyHolder = TryGetPropertyHolder();
        if (propertyHolder == null) return;

        currentTransferType = transferType;
        currentPropertyHolder = propertyHolder;
        currentState = TransferState.SelectingProperty;

        OpenPropertyUI();
    }

    /// <summary>
    /// 마우스 위치의 PropertyHolder를 찾는다.
    /// </summary>
    private PropertyHolder TryGetPropertyHolder()
    {
        if (mainCamera == null) return null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, propertyLayerMask))
            return hit.collider.GetComponentInParent<PropertyHolder>();

        return null;
    }

    /// <summary>
    /// 현재 전달 상태를 처리한다.
    /// </summary>
    private void ProcessTransfer()
    {
        if (currentState != TransferState.Processing) return;
        ExecuteTransfer();
    }

    /// <summary>
    /// 전달 타입에 따라 특성을 처리한다.
    /// </summary>
    private void ExecuteTransfer()
    {
        if (currentPropertyHolder == null || selectedPropertyData == null) { Cancel(); return; }

        switch (currentTransferType)
        {
            case TransferType.Extract: ExtractProperty(); break;
            case TransferType.Grant: GrantProperty(); break;
        }

        Cancel();
    }

    private void ExtractProperty()
    {
        Debug.Log($"{selectedPropertyData.PropertyName} 특성 추출");
    }

    private void GrantProperty()
    {
        Debug.Log($"{selectedPropertyData.PropertyName} 특성 부여");
    }

    /// <summary>
    /// 전달 타입에 맞는 특성 선택 UI를 연다.
    /// </summary>
    private void OpenPropertyUI()
    {
        switch (currentTransferType)
        {
            case TransferType.Extract: Debug.Log("추출 특성 선택 UI"); break;
            case TransferType.Grant: Debug.Log("부여 특성 선택 UI"); break;
        }
    }

    /// <summary>
    /// UI에서 선택한 특성을 설정한다.
    /// </summary>
    public void SelectProperty(PropertyData propertyData)
    {
        if (currentState != TransferState.SelectingProperty || propertyData == null) return;

        selectedPropertyData = propertyData;
        currentState = TransferState.Processing;
    }

    /// <summary>
    /// 현재 전달 작업을 초기화한다.
    /// </summary>
    private void Cancel()
    {
        currentTransferType = TransferType.None;
        currentState = TransferState.None;
        currentPropertyHolder = null;
        selectedPropertyData = null;
    }
}