using UnityEngine;

namespace GameCore
{
    public class UIScreen : MonoBehaviour
    {
        [SerializeField] private string screenId;

        public string ScreenId => screenId;

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}