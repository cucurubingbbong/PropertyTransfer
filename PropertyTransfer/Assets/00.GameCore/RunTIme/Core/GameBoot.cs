using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 게임 부트 클래스
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBoot : MonoBehaviour
    {
        private void Awake()
        {
            // 씬 전환 시에도 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);
        }
    }
}
