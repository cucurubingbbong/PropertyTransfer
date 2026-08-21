using UnityEngine;
using GameCore;
using System.Linq;

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

    /// <summary>
    /// 현재 총이 가지고 있는 특성
    /// </summary>
    [SerializeField] private PropertyData gunPropertyData = null;

    /// <summary>
    /// 현재 작업에서 선택한 특성
    /// </summary>
    [SerializeField] private PropertyData selectedPropertyData = null;

    /// <summary>
    /// 현재 추출 / 부여 대상
    /// </summary>
    [SerializeField] private PropertyHolder currentPropertyHolder = null;

    [SerializeField] private LayerMask propertyLayerMask = 1 << 6;
    [SerializeField] private float maxRayDistance = 20f;

    /// <summary>
    /// UI스크린 오브젝트
    /// </summary>
    [SerializeField] private PropertyUI propertyUI = null;

    private Camera mainCamera;

    public PropertyData GunPropertyData => gunPropertyData;

    private void Awake()
    {
        mainCamera = Camera.main;

        // 게임 시작 시 총은 아무 특성도 가지고 있지 않는다.
        gunPropertyData = null;
    }

    private void Start()
    {
        propertyUI.Ptc = this;
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
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Cancel();
            return;
        }

        if (currentState != TransferState.None)
            return;

        if (Input.GetMouseButtonDown(0))
            TryStartTransfer(TransferType.Extract);
        else if (Input.GetMouseButtonDown(1))
            TryStartTransfer(TransferType.Grant);
    }

    /// <summary>
    /// 대상을 찾아 전달을 시작한다.
    /// </summary>
    private void TryStartTransfer(TransferType transferType)
    {
        PropertyHolder propertyHolder = TryGetPropertyHolder();
        if (propertyHolder == null)
            return;

        currentTransferType = transferType;
        currentPropertyHolder = propertyHolder;

        switch (currentTransferType)
        {
            case TransferType.Extract:
                TryStartExtract();
                break;

            case TransferType.Grant:
                TryStartGrant();
                break;
        }
    }

    /// <summary>
    /// 마우스 위치의 PropertyHolder를 찾는다.
    /// </summary>
    private PropertyHolder TryGetPropertyHolder()
    {
        if (mainCamera == null)
            return null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, propertyLayerMask))
            return hit.collider.GetComponentInParent<PropertyHolder>();

        return null;
    }

    /// <summary>
    /// 특성 추출을 시작한다.
    /// </summary>
    private void TryStartExtract()
    {
        // 총에 이미 특성이 있다면 추가로 추출할 수 없다.
        if (gunPropertyData != null)
        {
            Debug.Log($"총에 이미 {gunPropertyData.PropertyName} 특성이 있습니다.");

            ResetTransfer();
            return;
        }

        if (currentPropertyHolder.PropertyCount <= 0)
        {
            Debug.Log("추출할 특성이 없습니다.");

            ResetTransfer();
            return;
        }

        currentState = TransferState.SelectingProperty;

        OpenPropertyUI();
    }

    /// <summary>
    /// 특성 부여를 시작한다.
    /// </summary>
    private void TryStartGrant()
    {
        if (gunPropertyData == null)
        {
            Debug.Log("총에 부여할 특성이 없습니다.");

            ResetTransfer();
            return;
        }

        // 같은 특성을 교체하거나 빈 슬롯이 있어야 부여할 수 있다.
        if (!currentPropertyHolder.CanAddProperty(gunPropertyData))
        {
            Debug.Log("대상이 더 이상 특성을 가질 수 없습니다.");

            ResetTransfer();
            return;
        }

        selectedPropertyData = gunPropertyData;
        currentState = TransferState.Processing;
    }

    /// <summary>
    /// 현재 전달 상태를 처리한다.
    /// </summary>
    private void ProcessTransfer()
    {
        if (currentState != TransferState.Processing)
            return;

        ExecuteTransfer();
        ResetTransfer();
    }

    /// <summary>
    /// 전달 타입에 따라 특성을 처리한다.
    /// </summary>
    private void ExecuteTransfer()
    {
        if (currentPropertyHolder == null || selectedPropertyData == null)
            return;

        switch (currentTransferType)
        {
            case TransferType.Extract:
                ExtractProperty();
                break;

            case TransferType.Grant:
                GrantProperty();
                break;
        }
    }

    /// <summary>
    /// 선택한 특성을 대상에서 제거하고 총에 저장한다.
    /// </summary>
    private void ExtractProperty()
    {
        PropertyData extractedProperty = currentPropertyHolder.RemoveProperty(selectedPropertyData);

        if (extractedProperty == null)
        {
            Debug.LogWarning("특성 추출에 실패했습니다.");
            return;
        }

        gunPropertyData = extractedProperty;

        Debug.Log($"{gunPropertyData.PropertyName} 특성 추출");
    }

    /// <summary>
    /// 총이 가지고 있는 특성을 대상에게 부여한다.
    /// 같은 종류의 특성이 있다면 서로 교환한다.
    /// </summary>
    private void GrantProperty()
    {
        if (!currentPropertyHolder.CanAddProperty(selectedPropertyData))
        {
            Debug.Log("특성을 부여할 수 없습니다.");
            return;
        }

        PropertyData grantedProperty = selectedPropertyData;
        PropertyData replacedProperty = currentPropertyHolder.AddProperty(grantedProperty);

        // 같은 특성이 이미 있었다면 기존 특성을 총이 가져온다.
        if (replacedProperty != null)
        {
            gunPropertyData = replacedProperty;

            Debug.Log($"{grantedProperty.PropertyName} 특성 부여 / {replacedProperty.PropertyName} 특성 회수");
            return;
        }

        // 빈 슬롯에 정상적으로 들어간 경우 총을 비운다.
        gunPropertyData = null;

        Debug.Log($"{grantedProperty.PropertyName} 특성 부여");
    }

    /// <summary>
    /// 전달 타입에 맞는 특성 선택 UI를 연다.
    /// </summary>
    private void OpenPropertyUI()
    {
        PropertyUIData[] uiData = CreatePropertyUIData();

        propertyUI.SetCard(uiData);
        UIManager.Instance.Show("Property");
    }

    /// <summary>
    /// 현재 대상의 특성을 UI 데이터로 변환한다.
    /// </summary>
    private PropertyUIData[] CreatePropertyUIData()
    {
        return currentPropertyHolder.Properties
            .Where(property => property != null)
            .Select(property => new PropertyUIData(
                property.PropertyName,
                property.PropertyDescription,
                property.PropertyIcon,
                (int)property.IsStatus))
            .ToArray();
    }

    /// <summary>
    /// UI에서 선택한 특성을 설정한다.
    /// </summary>
    public void SelectProperty(int index)
    {
        if (currentState != TransferState.SelectingProperty)
            return;

        if (currentTransferType != TransferType.Extract)
            return;

        if (index < 0 || index >= currentPropertyHolder.PropertyCount)
        {
            Debug.LogWarning($"잘못된 특성 Index: {index}");
            return;
        }

        selectedPropertyData = currentPropertyHolder.Properties[index];

        if (selectedPropertyData == null)
            return;

        currentState = TransferState.Processing;

        UIManager.Instance.Hide("Property");
    }

    /// <summary>
    /// 현재 전달 작업을 초기화한다.
    /// </summary>
    public void Cancel()
    {
        if (currentState == TransferState.SelectingProperty)
            UIManager.Instance.Hide("Property");

        ResetTransfer();
    }

    /// <summary>
    /// 현재 전달 작업에 사용되는 임시 데이터만 초기화한다.
    /// 총이 가지고 있는 특성은 초기화하지 않는다.
    /// </summary>
    private void ResetTransfer()
    {
        currentTransferType = TransferType.None;
        currentState = TransferState.None;
        currentPropertyHolder = null;
        selectedPropertyData = null;
    }
}