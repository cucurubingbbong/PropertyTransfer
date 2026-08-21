using System;
using System.Data.SqlTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct PropertyUIData
{
    public string Name;
    public string Des;
    public Sprite Icon;
    public int Step;

    public PropertyUIData(string name, string des, Sprite icon, int step)
    {
        Name = name;
        Des = des;
        Icon = icon;
        Step = step;
    }
}
public class PropertyCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI propertyName = null;

    [SerializeField] private TextMeshProUGUI propertyDes = null;

    [SerializeField] private Image propertyIcon = null;

    [SerializeField] private Slider stepSlider = null;

    public int index;

    public void SetElements(PropertyUIData Data)
    {
        propertyName.text = Data.Name;
        propertyDes.text = Data.Des;
        propertyIcon.sprite = Data.Icon;
        stepSlider.value = Data.Step;
    }

    public void SetIndex(int value)
    {
        index = value;
    }
}
