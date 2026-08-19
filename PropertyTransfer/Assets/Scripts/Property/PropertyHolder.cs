using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 사물이 현재 가지고 있는 특성을 관리한다
/// </summary>
public class PropertyHolder : MonoBehaviour
{
    private const int MaxPropertyCount = 3;

    [SerializeField] private OriginPropertyData[] originProperties = new OriginPropertyData[MaxPropertyCount];

    private PropertyData[] properties = new PropertyData[MaxPropertyCount];
    public PropertyData[] Properties => properties;

    private Dictionary<PropertyType, PropertyData> propertyDictionary = new Dictionary<PropertyType, PropertyData>();

    [SerializeField] private PhysicsPropertyApplier physicsPropertyApplier = null;

    private void Awake()
    {
        propertyDictionary.Clear();

        for (int i = 0; i < MaxPropertyCount; i++)
        {
            if (originProperties[i] == null)
                continue;

            PropertyData property = new PropertyData(originProperties[i]);

            properties[i] = property;

            UpdateDict(property);
            physicsPropertyApplier.ApplyProperty(property);
        }
    }

    private void UpdateDict(PropertyData property)
    {
        propertyDictionary[property.PropertyType] = property;
    }

    public void SetProperties(PropertyData[] newProperties)
    {
        propertyDictionary.Clear();

        for (int i = 0; i < MaxPropertyCount; i++)
        {
            if (i < newProperties.Length)
            {
                properties[i] = newProperties[i];

                UpdateDict(properties[i]);
                physicsPropertyApplier.ApplyProperty(properties[i]);
            }
            else
            {
                properties[i] = null;
            }
        }
    }

    public PropertyData GetProperty(PropertyType propertyType)
    {
        if (propertyDictionary.TryGetValue(propertyType, out var property))
        {
            return property;
        }

        return null;
    }

    /// <summary>
    /// 이미 존재하는 같은 특성의 데이터가 있다면 교체하고 기존 특성을 반환한다.
    /// 같은 특성이 없다면 빈 슬롯에 새 특성을 넣는다.
    /// </summary>
    public PropertyData AddProperty(PropertyData newProperty)
    {
        if (propertyDictionary.TryGetValue(newProperty.PropertyType, out var existingProperty))
        {
            for (int i = 0; i < MaxPropertyCount; i++)
            {
                if (properties[i] == existingProperty)
                {
                    properties[i] = newProperty;
                    break;
                }
            }

            UpdateDict(newProperty);
            physicsPropertyApplier.ApplyProperty(newProperty);

            return existingProperty;
        }

        for (int i = 0; i < MaxPropertyCount; i++)
        {
            if (properties[i] == null)
            {
                properties[i] = newProperty;

                UpdateDict(newProperty);
                physicsPropertyApplier.ApplyProperty(newProperty);

                return null;
            }
        }

        return null;
    }
}