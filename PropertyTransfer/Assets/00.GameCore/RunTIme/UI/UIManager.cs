using System.Collections.Generic;
using GameCore;
using UnityEngine;

namespace GameCore
{
    public class UIManager : Singleton<UIManager>
    {
        private readonly Dictionary<string, UIScreen> screens = new();
        private readonly Stack<UIPopup> popupStack = new();

        public void Register(UIScreen screen)
        {
            if (screen == null)
                return;

            string id = screen.ScreenId;

            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("[UIManager] ScreenId가 비어있습니다.");
                return;
            }

            if (screens.ContainsKey(id))
            {
                screens[id] = screen;
                return;
            }

            screens.Add(id, screen);
        }

        public void Unregister(UIScreen screen)
        {
            if (screen == null)
                return;

            string id = screen.ScreenId;

            if (screens.ContainsKey(id))
                screens.Remove(id);
        }

        public T Get<T>(string id) where T : UIScreen
        {
            if (screens.TryGetValue(id, out UIScreen screen))
                return screen as T;

            return null;
        }

        public void Show(string id)
        {
            if (screens.TryGetValue(id, out UIScreen screen))
            {
                screen.Show();
                return;
            }

            Debug.LogWarning($"[UIManager] 찾을 수 없는 UI: {id}");
        }

        public void Hide(string id)
        {
            if (screens.TryGetValue(id, out UIScreen screen))
            {
                screen.Hide();
                return;
            }

            Debug.LogWarning($"[UIManager] 찾을 수 없는 UI: {id}");
        }

        public void HideAll()
        {
            foreach (UIScreen screen in screens.Values)
            {
                screen.Hide();
            }
        }

        public void OpenPopup(string id)
        {
            if (!screens.TryGetValue(id, out UIScreen screen))
            {
                Debug.LogWarning($"[UIManager] 찾을 수 없는 팝업: {id}");
                return;
            }

            if (screen is not UIPopup popup)
            {
                Debug.LogWarning($"[UIManager] {id}는 UIPopup이 아닙니다.");
                return;
            }

            popup.Show();
            popupStack.Push(popup);
        }

        public void CloseTopPopup()
        {
            if (popupStack.Count <= 0)
                return;

            UIPopup popup = popupStack.Pop();
            popup.Close();
        }

        public void CloseAllPopups()
        {
            while (popupStack.Count > 0)
            {
                UIPopup popup = popupStack.Pop();
                popup.Close();
            }
        }
    }
}