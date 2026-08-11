using UnityEngine;
using System;

namespace GameCore
{
    /// <summary>
    /// 풀링 오브젝트 클래스
    /// </summary>
    public class PoolableObject : MonoBehaviour
    {
        public string PoolId { get; private set; }

        private ObjectPoolManager owner;

        public void Initialize(string poolId, ObjectPoolManager poolManager)
        {
            PoolId = poolId;
            owner = poolManager;
        }

        public void Spawned()
        {
            OnSpawned();
        }

        public void Despawned()
        {
            OnDespawned();
        }

        public void ReturnToPool()
        {
            if (owner == null)
            {
                gameObject.SetActive(false);
                return;
            }

            owner.Despawn(this);
        }

        protected virtual void OnSpawned()
        {
        }

        protected virtual void OnDespawned()
        {
        }
    }
}