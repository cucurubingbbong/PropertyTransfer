using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 싱글톤 패턴을 구현한 클래스
    /// </summary>
    /// <typeparam name="T">싱글톤으로 구현할 클래스의 타입</typeparam>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        /// 싱글톤 인스턴스에 접근하기 위한 정적 프로퍼티
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// Init 메서드에서 싱글톤 인스턴스를 초기화하고, 중복된 인스턴스가 존재할 경우 파괴합니다.
        /// </summary>
        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // 객체를 T로 캐스팅해 Instanse에 할당
            Instance = this as T;
        }
    }
}