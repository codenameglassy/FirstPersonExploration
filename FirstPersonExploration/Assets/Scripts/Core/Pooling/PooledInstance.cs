// PooledInstance
// Responsibility: Marks a prefab as poolable, links each instance back to its PrefabPool, and
// forwards spawn and despawn notifications to every IPoolable on the object and its children.
// Calling Release on an object that was never pooled destroys it instead.
using UnityEngine;

namespace Game.Core
{
    [DisallowMultipleComponent]
    public sealed class PooledInstance : MonoBehaviour
    {
        private PrefabPool owner;
        private IPoolable[] poolables;
        private bool isSpawned;

        public bool IsSpawned => isSpawned;

        public void Release()
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }

            if (!isSpawned)
            {
                return;
            }

            owner.Release(this);
        }

        internal void Bind(PrefabPool pool)
        {
            owner = pool;
        }

        internal void NotifySpawned()
        {
            CachePoolables();
            isSpawned = true;

            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawned();
            }
        }

        internal void NotifyDespawned()
        {
            if (!isSpawned)
            {
                return;
            }

            isSpawned = false;
            CachePoolables();

            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnDespawned();
            }
        }

        private void CachePoolables()
        {
            if (poolables == null)
            {
                poolables = GetComponentsInChildren<IPoolable>(true);
            }
        }
    }
}
