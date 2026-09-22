// Bootstrapper
// Responsibility: Composition root. Runs before every other script, creates the application
// lifetime services (TickManager, PoolService) and registers them. Optionally persists across
// scenes, in which case later Bootstrappers destroy themselves and the first one stays the owner.
using UnityEngine;

namespace Game.Core
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TickManager))]
    public sealed class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private bool persistAcrossScenes;
        [SerializeField, Min(1)] private int poolDefaultCapacity = 16;
        [SerializeField, Min(1)] private int poolMaxSize = 256;

        private static Bootstrapper persistentInstance;

        private TickManager tickManager;
        private PoolService poolService;
        private bool isOwner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            persistentInstance = null;
        }

        private void Awake()
        {
            if (persistentInstance != null)
            {
                Destroy(gameObject);
                return;
            }

            isOwner = true;

            if (persistAcrossScenes)
            {
                if (transform.parent == null)
                {
                    persistentInstance = this;
                    DontDestroyOnLoad(gameObject);
                }
                else
                {
                    Debug.LogWarning("Bootstrapper: Persist Across Scenes requires a root GameObject.", this);
                }
            }

            tickManager = GetComponent<TickManager>();

            Transform poolRoot = new GameObject("Pools").transform;
            poolRoot.SetParent(transform, false);
            poolService = new PoolService(poolRoot, poolDefaultCapacity, poolMaxSize);

            ServiceRegistry.Register(tickManager);
            ServiceRegistry.Register(poolService);
        }

        private void OnDestroy()
        {
            if (!isOwner)
            {
                return;
            }

            if (persistentInstance == this)
            {
                persistentInstance = null;
            }

            ServiceRegistry.Unregister(tickManager);
            ServiceRegistry.Unregister(poolService);
            poolService.Dispose();
        }
    }
}
