using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 사물이 현재 가지고 있는 특성을 관리한다
/// </summary>
public class PropertyHolder : MonoBehaviour
{
    [SerializeField] private PropertyData[] properties =  new PropertyData[3];

    public PropertyData[] Properties => properties;

    private Dictionary<PropertyType, PropertyData> propertyDictionary = new Dictionary<PropertyType, PropertyData>();

    [SerializeField] private PhysicsPropertyApplier physicsPropertyApplier = null;

    private void Awake()
    {
        foreach (var property in properties)
        {
            propertyDictionary[property.PropertyType] = property;
            UpdateDict(property);
            physicsPropertyApplier.ApplyProperty(property);
        }
    }

    private void UpdateDict(PropertyData property)
    {
        if (propertyDictionary.ContainsKey(property.PropertyType))
        {
            propertyDictionary[property.PropertyType] = property;
        }
        else
        {
            propertyDictionary.Add(property.PropertyType, property);
        }
    }


    public void SetProperties(PropertyData[] newProperties)
    {
        properties = newProperties;
        propertyDictionary.Clear();
        foreach (var property in properties)
        {
            propertyDictionary[property.PropertyType] = property;
            UpdateDict(property);
            physicsPropertyApplier.ApplyProperty(property);
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
    /// 이미 존재하는 같은 특성의 데이터가 있다면 반환하고 새 특성을 넣습니다
    /// </summary>
    public PropertyData AddProperty(PropertyData newProperty)
    {
        if (propertyDictionary.TryGetValue(newProperty.PropertyType, out var existingProperty))
        {
            propertyDictionary[newProperty.PropertyType] = newProperty;
            physicsPropertyApplier.ApplyProperty(newProperty);
            return existingProperty;
        }
        else
        {
            propertyDictionary.Add(newProperty.PropertyType, newProperty);
            physicsPropertyApplier.ApplyProperty(newProperty);
            return null;
        }
    }
}
