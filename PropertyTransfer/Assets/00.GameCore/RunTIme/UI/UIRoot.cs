using UnityEngine;

namespace GameCore
{
    public class UIRoot : MonoBehaviour
    {
        [SerializeField] private bool hideOnStart = true;

        private void Start()
        {
            RegisterScreens();

            if (hideOnStart)
                HideAllChildren();
        }

        private void RegisterScreens()
        {
            UIScreen[] screens = GetComponentsInChildren<UIScreen>(true);

            foreach (UIScreen screen in screens)
            {
                UIManager.Instance.Register(screen);
            }
        }

        private void HideAllChildren()
        {
            UIScreen[] screens = GetComponentsInChildren<UIScreen>(true);

            foreach (UIScreen screen in screens)
            {
                screen.Hide();
            }
        }
    }
}