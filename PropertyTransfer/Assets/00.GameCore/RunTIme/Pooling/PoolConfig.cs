using System;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public class PoolConfig
    {
        public string id;
        public PoolableObject prefab;
        public int initialCount = 10;
        public bool expandable = true;
    }
}