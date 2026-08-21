using GameCore;
using UnityEngine;
using UnityEngine.UI;

public class PropertyUI : UIScreen
{
    public enum PropertyUIOpenType
    {
        None,
        PlayerProperty,
        ObjectProperty
    }

    [SerializeField] private PropertyCard[] propertyCards = new PropertyCard[3];

    [SerializeField] private Button confirmBtn = null;

    [SerializeField] private Button cancelBtn = null;

    /// <summary>
    /// 현재 선택한 카드 Index
    /// -1은 선택되지 않은 상태
    /// </summary>
    [SerializeField] private int selectIndex = -1;

    public PropertyUIOpenType currentUIOpenType = PropertyUIOpenType.None;

    public PropertyTransferController Ptc;

    public void SetCard(PropertyUIData[] data)
    {
        selectIndex = -1;

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

    public void SelectCard(int index)
    {
        selectIndex = index;
    }

    public void Confirm()
    {
        if (selectIndex < 0)
        {
            Debug.Log("선택된 특성이 없습니다.");
            return;
        }

        Ptc.SelectProperty(selectIndex);
    }

    public void Cancel()
    {
        selectIndex = -1;
        Ptc.Cancel();
    }
}