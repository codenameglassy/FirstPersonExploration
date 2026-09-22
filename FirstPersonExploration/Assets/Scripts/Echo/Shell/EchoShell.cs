// EchoShell
// Responsibility: One pooled, visible echo wavefront: an outer shell plus a dimmer inner shell that
// trails it, which together read as a bubble with thickness. Grows and fades through EchoCurves so
// it stays in lockstep with responder timing, ticks through TickManager, and releases itself to its
// pool when done. Follows a moving target by copying its position instead of parenting, so a
// disabled parent can never strand a frozen shell.
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Echo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PooledInstance))]
    public sealed class EchoShell : MonoBehaviour, IPoolable, ITickable
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int LifeId = Shader.PropertyToID("_Life");

        private const float VisibleAlpha = 0.001f;

        [SerializeField] private MeshRenderer outerRenderer;
        [SerializeField] private MeshRenderer innerRenderer;

        private PooledInstance pooledInstance;
        private TickManager tickManager;
        private Transform outerTransform;
        private Transform innerTransform;
        private MaterialPropertyBlock outerBlock;
        private MaterialPropertyBlock innerBlock;
        private bool isConfigured;

        private EchoShellProfileSO profile;
        private Transform followTarget;
        private bool isFollowing;
        private bool isPlaying;
        private float waveRadius;
        private float waveDuration;
        private float waveStart01;
        private float elapsed;
        private Color outerColour;
        private Color innerColour;

        public static bool TrySpawn(PoolService pools, PooledInstance prefab, Vector3 position, out EchoShell shell)
        {
            shell = null;

            if (pools == null || prefab == null)
            {
                return false;
            }

            PooledInstance instance = pools.Spawn(prefab, position, Quaternion.identity);
            if (instance == null)
            {
                return false;
            }

            if (instance.TryGetComponent(out shell))
            {
                return true;
            }

            Debug.LogError($"EchoShell: Prefab '{prefab.name}' has no EchoShell component.", prefab);
            instance.Release();
            return false;
        }

        private void Awake()
        {
            pooledInstance = GetComponent<PooledInstance>();
            outerBlock = new MaterialPropertyBlock();
            innerBlock = new MaterialPropertyBlock();

            if (outerRenderer == null || innerRenderer == null)
            {
                Debug.LogError("EchoShell: Assign both Outer Renderer and Inner Renderer.", this);
                return;
            }

            outerTransform = outerRenderer.transform;
            innerTransform = innerRenderer.transform;
            outerTransform.localPosition = Vector3.zero;
            innerTransform.localPosition = Vector3.zero;

            ConfigureLayer(outerRenderer);
            ConfigureLayer(innerRenderer);
            isConfigured = true;
        }

        public void OnSpawned()
        {
            ResetState();
        }

        public void OnDespawned()
        {
            if (tickManager != null)
            {
                tickManager.Unregister(this);
            }

            ResetState();
        }

        public void Play(float radius, float duration, float startRadius01, Color colour, Transform follow, EchoShellProfileSO shellProfile)
        {
            if (!isConfigured)
            {
                Release();
                return;
            }

            if (shellProfile == null)
            {
                Debug.LogError("EchoShell: Played without a shell profile.", this);
                Release();
                return;
            }

            if (tickManager == null && !ServiceRegistry.TryGet(out tickManager))
            {
                Debug.LogError("EchoShell: TickManager not found. Is a Bootstrapper in the scene?", this);
                Release();
                return;
            }

            profile = shellProfile;
            waveRadius = Mathf.Max(0.01f, radius);
            waveDuration = Mathf.Max(0.05f, duration);
            waveStart01 = Mathf.Clamp(startRadius01, 0f, 0.95f);
            outerColour = colour;
            innerColour = Color.Lerp(colour, Color.white, profile.InnerWhiten);
            followTarget = follow;
            isFollowing = follow != null;
            elapsed = 0f;
            isPlaying = true;

            ApplyLayers();
            tickManager.Register(this);
        }

        public void Tick(float deltaTime)
        {
            if (!isPlaying)
            {
                return;
            }

            if (isFollowing)
            {
                if (followTarget != null)
                {
                    transform.position = followTarget.position;
                }
                else
                {
                    isFollowing = false;
                }
            }

            elapsed += deltaTime;
            ApplyLayers();

            if (elapsed >= waveDuration + profile.InnerDelay)
            {
                Release();
            }
        }

        private void ApplyLayers()
        {
            ApplyLayer(outerTransform, outerRenderer, outerBlock, outerColour, 0f, 1f);
            ApplyLayer(innerTransform, innerRenderer, innerBlock, innerColour, profile.InnerDelay, profile.InnerAlphaScale);
        }

        private void ApplyLayer(Transform layer, MeshRenderer layerRenderer, MaterialPropertyBlock block, Color colour, float delay, float alphaScale)
        {
            float t = Mathf.Clamp01((elapsed - delay) / waveDuration);
            float alpha = elapsed < delay ? 0f : EchoCurves.Alpha01(t, profile.AlphaPeak) * alphaScale;

            bool visible = alpha > VisibleAlpha;
            if (layerRenderer.enabled != visible)
            {
                layerRenderer.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            layer.localScale = Vector3.one * (waveRadius * EchoCurves.FrontRadius01(t, waveStart01));

            block.SetColor(ColorId, colour);
            block.SetFloat(AlphaId, alpha);
            block.SetFloat(LifeId, t);
            layerRenderer.SetPropertyBlock(block);
        }

        private void Release()
        {
            isPlaying = false;

            if (pooledInstance != null)
            {
                pooledInstance.Release();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void ResetState()
        {
            isPlaying = false;
            isFollowing = false;
            followTarget = null;
            profile = null;
            elapsed = 0f;

            if (isConfigured)
            {
                outerRenderer.enabled = false;
                innerRenderer.enabled = false;
            }
        }

        private static void ConfigureLayer(MeshRenderer layerRenderer)
        {
            if (layerRenderer.TryGetComponent(out MeshFilter filter))
            {
                filter.sharedMesh = EchoIcosphere.Get();
            }
            else
            {
                Debug.LogError($"EchoShell: '{layerRenderer.name}' needs a MeshFilter.", layerRenderer);
            }

            layerRenderer.shadowCastingMode = ShadowCastingMode.Off;
            layerRenderer.receiveShadows = false;
            layerRenderer.lightProbeUsage = LightProbeUsage.Off;
            layerRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            layerRenderer.allowOcclusionWhenDynamic = false;
            layerRenderer.enabled = false;
        }
    }
}
