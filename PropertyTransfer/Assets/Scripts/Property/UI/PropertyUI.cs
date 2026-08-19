using GameCore;
using UnityEngine;

public class PropertyUI : UIScreen
{
    public enum PropertyUIOpenType
    {
        None,
        PlayerProperty,
        ObjectProperty
    }
    [SerializeField] private PropertyCard[] propertyCards = new PropertyCard[3];

    public PropertyUIOpenType currentUIOpenType = PropertyUIOpenType.None;

    public void SetCard(PropertyUIData[] data)
    {
        //Debug.Log(data.Length);
        for (int i = 0; i < propertyCards.Length; i++)
        {
            if (i < data.Length)
            {
                propertyCards[i].gameObject.SetActive(true);
                propertyCards[i].SetElements(data[i]);
                propertyCards[i].SetIndex(i);
            }
            else
            {
                propertyCards[i].gameObject.SetActive(false);
            }
        }
    }
}

