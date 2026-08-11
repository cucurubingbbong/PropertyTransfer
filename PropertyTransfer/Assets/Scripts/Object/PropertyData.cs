using UnityEngine;

[System.Serializable]
public class PropertyData
{
    [SerializeField] private string propertyName = string.Empty;
    public string PropertyName => propertyName;

    [SerializeField] private string propertyDescription = string.Empty;
    public string PropertyDescription => propertyDescription;

    [SerializeField] private string propertyIconPath = string.Empty;
    public string PropertyIconPath => propertyIconPath;

    [SerializeField] private Sprite propertyIcon = null;
    public Sprite PropertyIcon => propertyIcon;
    
    [SerializeField] private PropertyType propertyType = PropertyType.None;
    public PropertyType PropertyType => propertyType;

    /// <summary>
    /// 물리유형의 상태
    /// true : 무거움
    /// false : 가벼움등등...
    /// enum으로 안한 이유는 귀차늠 나중에 내가 하겟지
    /// </summary>
    [SerializeField] private bool isStatus = false;

    public bool IsStatus => isStatus;

    /// <summary>
    /// 물리유형의 값
    /// </summary>
    [SerializeField] private float value = 0f;
    public float Value => value;

    private void Start()
    {
        if (propertyIcon == null && !string.IsNullOrEmpty(propertyIconPath))
        {
            propertyIcon = Resources.Load<Sprite>(propertyIconPath);
        }
    }
}
