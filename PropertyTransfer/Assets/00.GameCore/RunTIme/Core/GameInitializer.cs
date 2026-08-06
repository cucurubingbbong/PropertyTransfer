using UnityEngine;

/// <summary>
/// 게임 초기화 클래스
/// </summary>

namespace GameCore
{
    public class GameInitializer : Singleton<GameInitializer>
    {
        void Start()
        {
            Initialize();
        }
        /// <summary>
        /// 게임 초기화 메서드
        /// </summary>
        public void Initialize()
        {
            Debug.Log("게임 코어 초기화");
        }
    }
}

