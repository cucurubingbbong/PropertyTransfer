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
    /// <summary>
    /// 현재 추출 대상의 오브젝트 특성홀더
    /// </summary>
    [SerializeField] PropertyHolder currentObjectPropertyHolder = null;

    public PropertyUIOpenType currentUIOpenType = PropertyUIOpenType.None;

    public override void Show()
    {
        base.Show();
    }

    public void OpenSelectUI(PropertyUIOpenType propertyUIOpenType)
    {
        
    }
}
