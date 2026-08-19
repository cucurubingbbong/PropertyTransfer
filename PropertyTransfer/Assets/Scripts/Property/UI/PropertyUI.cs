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

    public void SetCard(PropertyUIData[] Data)
    {
        for (int i = 0; i < propertyCards.Length; i++)
        {
            if (i < Data.Length)
            {
                propertyCards[i].gameObject.SetActive(true);
                propertyCards[i].SetElements(Data[i]);
            }
            else
            {
                propertyCards[i].gameObject.SetActive(false);
            }
        }
    }
}
