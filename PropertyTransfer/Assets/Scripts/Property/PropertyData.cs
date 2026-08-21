using UnityEngine;

/// <summary>
/// 물리 특성의 단계
/// </summary>
public enum PropertyStatus
{
    None = -1,
    Low,
    Medium,
    High
}

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

    [SerializeField] private PropertyStatus isStatus = PropertyStatus.None;
    public PropertyStatus IsStatus => isStatus;

    [SerializeField] private float value = 0f;
    public float Value => value;

    public PropertyData(OriginPropertyData originData)
    {
        propertyName = originData.PropertyName;
        propertyDescription = originData.PropertyDescription;
        propertyIconPath = originData.PropertyIconPath;
        propertyIcon = originData.PropertyIcon;
        propertyType = originData.PropertyType;
        isStatus = originData.IsStatus;
        value = originData.Value;
    }

    /// <summary>
    ///  추후 addressable로 변경
    /// </summary>
    public void LoadIcon()
    {
        propertyIcon = Resources.Load<Sprite>(propertyIconPath);
    }
}