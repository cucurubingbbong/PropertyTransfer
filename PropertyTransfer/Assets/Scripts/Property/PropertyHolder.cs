using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 사물이 현재 가지고 있는 특성을 관리한다
/// </summary>
public class PropertyHolder : MonoBehaviour
{
    private const int MaxPropertyCount = 3;

    [SerializeField] private OriginPropertyData[] originProperties = new OriginPropertyData[MaxPropertyCount];

    [SerializeField] private PropertyData[] properties = new PropertyData[MaxPropertyCount];
    public PropertyData[] Properties => properties;

    /// <summary>
    /// 현재 가지고 있는 특성 개수
    /// </summary>
    public int PropertyCount
    {
        get
        {
            int count = 0;

            for (int i = 0; i < MaxPropertyCount; i++)
            {
                if (properties[i] != null)
                    count++;
            }

            return count;
        }
    }

    private Dictionary<PropertyType, PropertyData> propertyDictionary = new Dictionary<PropertyType, PropertyData>();

    [SerializeField] private PhysicsPropertyApplier physicsPropertyApplier = null;

    private void Awake()
    {
        propertyDictionary.Clear();

        for (int i = 0; i < MaxPropertyCount; i++)
            properties[i] = null;
    }

    private void Start()
    {
        int propertyIndex = 0;

        for (int i = 0; i < MaxPropertyCount; i++)
        {
            if (originProperties[i] == null)
                continue;

            PropertyData property = new PropertyData(originProperties[i]);

            properties[propertyIndex] = property;
            propertyIndex++;

            UpdateDict(property);
            physicsPropertyApplier.ApplyProperty(property);
        }
    }

    private void UpdateDict(PropertyData property)
    {
        if (property == null)
            return;

        propertyDictionary[property.PropertyType] = property;
    }

    public void SetProperties(PropertyData[] newProperties)
    {
        propertyDictionary.Clear();

        for (int i = 0; i < MaxPropertyCount; i++)
            properties[i] = null;

        int propertyIndex = 0;

        for (int i = 0; i < newProperties.Length; i++)
        {
            if (propertyIndex >= MaxPropertyCount)
                break;

            if (newProperties[i] == null)
                continue;

            properties[propertyIndex] = newProperties[i];

            UpdateDict(properties[propertyIndex]);
            physicsPropertyApplier.ApplyProperty(properties[propertyIndex]);

            propertyIndex++;
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
    /// 해당 특성을 추가할 수 있는지 확인한다.
    /// 같은 종류의 특성이 있거나 빈 슬롯이 있다면 추가할 수 있다.
    /// </summary>
    public bool CanAddProperty(PropertyData newProperty)
    {
        if (newProperty == null)
            return false;

        // 같은 특성은 기존 특성을 교체할 수 있다.
        if (propertyDictionary.ContainsKey(newProperty.PropertyType))
            return true;

        // 빈 슬롯 확인
        for (int i = 0; i < MaxPropertyCount; i++)
        {
            if (properties[i] == null)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 이미 존재하는 같은 특성의 데이터가 있다면 교체하고 기존 특성을 반환한다.
    /// 같은 특성이 없다면 빈 슬롯에 새 특성을 넣는다.
    /// </summary>
    public PropertyData AddProperty(PropertyData newProperty)
    {
        if (newProperty == null)
            return null;

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

    /// <summary>
    /// 현재 가지고 있는 특성을 제거하고 제거된 특성을 반환한다.
    /// </summary>
    public PropertyData RemoveProperty(PropertyData property)
    {
        if (property == null)
            return null;

        for (int i = 0; i < MaxPropertyCount; i++)
        {
            if (properties[i] != property)
                continue;

            PropertyData removedProperty = properties[i];

            properties[i] = null;

            if (propertyDictionary.TryGetValue(removedProperty.PropertyType, out PropertyData dictionaryProperty))
            {
                if (dictionaryProperty == removedProperty)
                    propertyDictionary.Remove(removedProperty.PropertyType);
            }

            CompactProperties();

            return removedProperty;
        }

        return null;
    }

    /// <summary>
    /// 특성 배열의 빈 공간을 제거하고 앞쪽부터 정렬한다.
    /// UI의 카드 Index와 Properties Index가 동일하게 유지되도록 한다.
    /// </summary>
    private void CompactProperties()
    {
        int targetIndex = 0;

        for (int i = 0; i < MaxPropertyCount; i++)
        {
            if (properties[i] == null)
                continue;

            if (targetIndex != i)
            {
                properties[targetIndex] = properties[i];
                properties[i] = null;
            }

            targetIndex++;
        }
    }
}