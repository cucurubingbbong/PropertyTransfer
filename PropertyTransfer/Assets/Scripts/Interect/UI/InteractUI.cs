using GameCore;
using TMPro;
using UnityEngine;

public class InteractUI : UIScreen
{
    [SerializeField] TextMeshProUGUI InteractObjNameText = null;

    [SerializeField] TextMeshProUGUI InteractDesciption = null;

    public void ShowText(string interactName , string interactDes)
    {
        InteractObjNameText.text = interactName;
        InteractDesciption.text = interactDes;
    }
}
