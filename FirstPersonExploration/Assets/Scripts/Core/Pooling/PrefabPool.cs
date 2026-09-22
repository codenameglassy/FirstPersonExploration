// PrefabPool
// Responsibility: Wraps Unity's ObjectPool<T> for one prefab. Places the instance before activating
// it, guarantees spawn and despawn notifications, returns released instances under the pool root,
// and destroys late releases after the pool is disposed.
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Core
{
    public sealed class PrefabPool
    {
        private readonly PooledInstance prefab;
        private readonly Transform root;
        private readonly ObjectPool<PooledInstance> pool;
        private bool isDisposed;

        public PrefabPool(PooledInstance prefab, Transform root, int defaultCapacity, int maxSize)
        {
            this.prefab = prefab;
            this.root = root;

            pool = new ObjectPool<PooledInstance>(
                CreateInstance,
                null,
                HandleRelease,
                HandleDestroy,
                Application.isEditor,
                Mathf.Max(1, defaultCapacity),
                Mathf.Max(1, maxSize));
        }

        public int CountActive => pool.CountActive;
        public int CountInactive => pool.CountInactive;

        public PooledInstance Spawn(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (isDisposed)
            {
                return null;
            }

            PooledInstance instance = pool.Get();

            // An instance destroyed externally while inactive is skipped rather than reused.
            while (instance == null)
            {
                instance = pool.Get();
            }

            Transform instanceTransform = instance.transform;
            instanceTransform.SetParent(parent != null ? parent : root, false);
            instanceTransform.SetPositionAndRotation(position, rotation);

            instance.gameObject.SetActive(true);
            instance.NotifySpawned();
            return instance;
        }

        public void Release(PooledInstance instance)
        {
            if (instance == null || !instance.IsSpawned)
            {
                return;
            }

            if (isDisposed)
            {
                instance.NotifyDespawned();
                Object.Destroy(instance.gameObject);
                return;
            }

            pool.Release(instance);
        }

        public void Prewarm(int count)
        {
            if (isDisposed || count <= pool.CountInactive)
            {
                return;
            }

            PooledInstance[] warmed = new PooledInstance[count];

            for (int i = 0; i < count; i++)
            {
                warmed[i] = pool.Get();
            }

            for (int i = 0; i < count; i++)
            {
                pool.Release(warmed[i]);
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            pool.Dispose();
        }

        private PooledInstance CreateInstance()
        {
            PooledInstance instance = Object.Instantiate(prefab, root);
            instance.gameObject.SetActive(false);
            instance.Bind(this);
            return instance;
        }

        private void HandleRelease(PooledInstance instance)
        {
            instance.NotifyDespawned();
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(root, false);
        }

        private static void HandleDestroy(PooledInstance instance)
        {
            if (instance != null)
            {
                Object.Destroy(instance.gameObject);
            }
        }
    }
}
