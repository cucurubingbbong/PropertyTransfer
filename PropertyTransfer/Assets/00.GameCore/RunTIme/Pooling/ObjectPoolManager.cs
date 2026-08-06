using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 풀링 매니저 클래스
    /// </summary>
    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        /// <summary>
        /// 풀링 오브젝트를 관리하는 내부 클래스
        /// </summary>
        private class Pool
        {
            public PoolConfig config;
            public Transform parent;
            public Queue<PoolableObject> inactiveObjects = new();

            public Pool(PoolConfig config, Transform parent)
            {
                this.config = config;
                this.parent = parent;
            }
        }
        /// <summary>
        /// 풀링 오브젝트 설정 리스트
        /// </summary>
        [SerializeField] private List<PoolConfig> poolConfigs = new();

        /// <summary>
        /// 풀링 오브젝트 트랜스폼
        /// </summary>
        [SerializeField] private Transform poolRoot;

        /// <summary>
        /// 풀링 오브젝트 딕셔너리
        /// </summary>
        private readonly Dictionary<string, Pool> pools = new();

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
                return;

            CreateRoot();
            CreatePools();
        }
        

        /// <summary>
        /// 풀링 오브젝트들의 루트 오브젝트 생성
        /// </summary>
        private void CreateRoot()
        {
            if (poolRoot != null)
                return;

            GameObject rootObject = new GameObject("PoolRoot");
            rootObject.transform.SetParent(transform);
            poolRoot = rootObject.transform;
        }

        /// <summary>
        /// 풀링 오브젝트 생성 시도
        /// </summary>

        private void CreatePools()
        {
            foreach (PoolConfig config in poolConfigs)
            {
                if (config == null)
                    continue;

                if (config == null)
                {
                    Debug.LogWarning("[ObjectPoolManager] Pool ID가 비어있습니다.");
                    continue;
                }

                if (config.prefab == null)
                {
                    Debug.LogWarning($"[ObjectPoolManager] {config.id} 프리팹이 없습니다.");
                    continue;
                }

                if (pools.ContainsKey(config.id))
                {
                    Debug.LogWarning($"[ObjectPoolManager] 중복된 Pool ID: {config.id}");
                    continue;
                }

                GameObject poolObject = new GameObject($"Pool_{config.id}");
                poolObject.transform.SetParent(poolRoot);

                Pool pool = new Pool(config, poolObject.transform);
                pools.Add(config.id, pool);

                for (int i = 0; i < config.initialCount; i++)
                {
                    PoolableObject obj = CreateObject(pool);
                    pool.inactiveObjects.Enqueue(obj);
                }
            }
        }

        /// <summary>
        /// 풀링 오브젝트 생성 
        /// </summary>
        /// <param name="pool">풀링 오브젝트</param>
        /// <returns></returns>

        private PoolableObject CreateObject(Pool pool)
        {
            PoolableObject obj = Instantiate(pool.config.prefab, pool.parent);

            obj.Initialize(pool.config.id, this);
            obj.gameObject.SetActive(false);

            return obj;
        }

        /// <summary>
        /// 풀링 오브젝트 스폰
        /// </summary>
        /// <param name="id">풀링 오브젝트의 ID</param>
        /// <param name="position">스폰 위치</param>
        /// <param name="rotation">스폰 회전</param>
        /// <returns></returns>

        public PoolableObject Spawn(string id, Vector3 position, Quaternion rotation)
        {
            if (!pools.TryGetValue(id, out Pool pool))
            {
                Debug.LogWarning($"[ObjectPoolManager] Pool을 찾을 수 없습니다: {id}");
                return null;
            }

            PoolableObject obj = GetObject(pool);

            if (obj == null)
                return null;

            obj.transform.SetParent(null);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);

            obj.Spawned();

            return obj;
        }

        /// <summary>
        /// 풀링 오브젝트 스폰
        /// </summary>
        /// <typeparam name="T">풀링 오브젝트의 타입</typeparam>
        /// <param name="id">풀링 오브젝트의 ID</param>
        /// <param name="position">스폰 위치</param>
        /// <param name="rotation">스폰 회전</param>
        /// <returns></returns>
        public T Spawn<T>(string id, Vector3 position, Quaternion rotation) where T : PoolableObject
        {
            PoolableObject obj = Spawn(id, position, rotation);

            if (obj == null)
                return null;

            return obj as T;
        }

        /// <summary>
        /// 풀링 오브젝트 가져오기
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        private PoolableObject GetObject(Pool pool)
        {
            if (pool.inactiveObjects.Count > 0)
            {
                return pool.inactiveObjects.Dequeue();
            }

            if (pool.config.expandable)
            {
                return CreateObject(pool);
            }

            Debug.LogWarning($"[ObjectPoolManager] Pool 부족: {pool.config.id}");
            return null;
        }

        /// <summary>
        /// 풀링 오브젝트 반환
        /// </summary>
        /// <param name="obj"></param>
        public void Despawn(PoolableObject obj)
        {
            if (obj == null)
                return;

            if (!pools.TryGetValue(obj.PoolId, out Pool pool))
            {
                obj.gameObject.SetActive(false);
                return;
            }

            obj.Despawned();

            obj.gameObject.SetActive(false);
            obj.transform.SetParent(pool.parent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            pool.inactiveObjects.Enqueue(obj);
        }
    }
}