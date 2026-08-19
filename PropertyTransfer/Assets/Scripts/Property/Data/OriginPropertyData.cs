using UnityEngine;

[CreateAssetMenu(fileName = "OriginPropertyData", menuName = "Data/OriginPropertyData")]
public class OriginPropertyData : ScriptableObject
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
}