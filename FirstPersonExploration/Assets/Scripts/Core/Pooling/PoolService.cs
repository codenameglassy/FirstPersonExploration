// PoolService
// Responsibility: Owns one PrefabPool per prefab and is the single entry point for spawning pooled
// objects (VFX pulses, audio sources, echo visuals). Created and registered by the Bootstrapper.
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public sealed class PoolService
    {
        private readonly Dictionary<PooledInstance, PrefabPool> pools = new Dictionary<PooledInstance, PrefabPool>(16);
        private readonly Transform root;
        private readonly int defaultCapacity;
        private readonly int maxSize;
        private bool isDisposed;

        public PoolService(Transform root, int defaultCapacity, int maxSize)
        {
            this.root = root;
            this.defaultCapacity = defaultCapacity;
            this.maxSize = maxSize;
        }

        public PooledInstance Spawn(PooledInstance prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            PrefabPool pool = GetPool(prefab);
            return pool != null ? pool.Spawn(position, rotation, parent) : null;
        }

        public void Prewarm(PooledInstance prefab, int count)
        {
            PrefabPool pool = GetPool(prefab);
            if (pool != null)
            {
                pool.Prewarm(count);
            }
        }

        public PrefabPool GetPool(PooledInstance prefab)
        {
            if (isDisposed)
            {
                return null;
            }

            if (prefab == null)
            {
                Debug.LogError("PoolService: Cannot pool a null prefab.");
                return null;
            }

            if (pools.TryGetValue(prefab, out PrefabPool existing))
            {
                return existing;
            }

            Transform poolRoot = new GameObject(prefab.name).transform;
            poolRoot.SetParent(root, false);

            PrefabPool created = new PrefabPool(prefab, poolRoot, defaultCapacity, maxSize);
            pools.Add(prefab, created);
            return created;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            foreach (PrefabPool pool in pools.Values)
            {
                pool.Dispose();
            }

            pools.Clear();
        }
    }
}
