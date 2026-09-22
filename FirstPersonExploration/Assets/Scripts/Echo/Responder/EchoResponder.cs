// EchoResponder
// Responsibility: Replaces the Resonator. Listens for EchoWaves, measures to the nearest point on its
// own colliders so it answers the moment the visible shell touches it, and responds one beat later in
// one of two tiers. Engage commits state and needs range, charge and line of sight; acknowledge is
// purely expressive. Reactions on the same prefab subscribe to the C# events. Ticks only while an
// echo is on its way.
using System;
using Game.Core;
using UnityEngine;

namespace Game.Echo
{
    [DisallowMultipleComponent]
    public sealed class EchoResponder : MonoBehaviour, ITickable
    {
        private const int MaxPending = 4;

        // Shared by every responder. Safe because resolves run one at a time on the main thread.
        private static readonly RaycastHit[] OcclusionHits = new RaycastHit[8];

        [SerializeField] private EchoResponderProfileSO profile;
        [SerializeField] private EchoWaveEventChannelSO waveChannel;
        [Tooltip("Point used for line of sight and the answering shell. Place it at the object's visual centre. Empty uses this transform.")]
        [SerializeField] private Transform anchor;
        [Tooltip("Colliders that define the object's surface for reach. Empty uses every collider on this object and its children. Box, Sphere, Capsule and convex Mesh colliders are supported.")]
        [SerializeField] private Collider[] reachColliders;

        private readonly PendingEcho[] pending = new PendingEcho[MaxPending];
        private Collider[] surface;
        private int pendingCount;
        private TickManager tickManager;
        private PoolService poolService;
        private bool isTicking;
        private bool isSpent;
        private float nextEngageTime;

        // The front reached this object. Fires before the response delay, at any distance.
        public event Action<EchoContext> Arrived;

        // Within range, charged enough, in line of sight. The tier that changes game state.
        public event Action<EchoContext> Engaged;

        // Reached but not engaged. Expressive only; never change game state from this.
        public event Action<EchoContext> Acknowledged;

        public bool IsSpent => isSpent;
        public bool CanEngage => !isSpent && Time.time >= nextEngageTime;

        private Transform AnchorTransform => anchor != null ? anchor : transform;
        private Vector3 AnchorPosition => AnchorTransform.position;

        private struct PendingEcho
        {
            public EchoContext Context;
            public float ArrivalTime;
            public float AnswerTime;
            public bool HasArrived;
        }

        private void Awake()
        {
            if (profile == null)
            {
                Debug.LogError("EchoResponder: No responder profile assigned.", this);
            }

            CacheSurface();
        }

        private void OnEnable()
        {
            if (!ServiceRegistry.TryGet(out tickManager))
            {
                Debug.LogError("EchoResponder: TickManager not found. Is a Bootstrapper in the scene?", this);
            }

            ServiceRegistry.TryGet(out poolService);

            if (waveChannel != null)
            {
                waveChannel.Register(HandleWave);
            }
        }

        private void OnDisable()
        {
            if (waveChannel != null)
            {
                waveChannel.Unregister(HandleWave);
            }

            // Anything still in flight is dropped, not committed, so a responder switched off
            // mid-wave is never spent without having answered.
            StopTicking();
            pendingCount = 0;
            nextEngageTime = 0f;
            tickManager = null;
            poolService = null;
        }

        private void HandleWave(EchoWave wave)
        {
            if (profile == null || tickManager == null)
            {
                return;
            }

            float distance = ReachDistance(wave.Origin);
            if (!wave.TryGetArrivalTime(distance, out float arrivalTime))
            {
                return;
            }

            if (pendingCount == MaxPending)
            {
                Debug.LogWarning("EchoResponder: Too many echoes in flight, one was dropped.", this);
                return;
            }

            pending[pendingCount] = new PendingEcho
            {
                Context = new EchoContext(wave, distance),
                ArrivalTime = arrivalTime,
                AnswerTime = arrivalTime + profile.ResponseDelay,
                HasArrived = false
            };

            pendingCount++;
            StartTicking();
        }

        public void Tick(float deltaTime)
        {
            float now = Time.time;
            int i = 0;

            while (i < pendingCount)
            {
                if (!pending[i].HasArrived && now >= pending[i].ArrivalTime)
                {
                    pending[i].HasArrived = true;
                    Arrived?.Invoke(pending[i].Context);

                    // A listener may have disabled this responder, which clears everything.
                    if (i >= pendingCount)
                    {
                        break;
                    }
                }

                if (now >= pending[i].AnswerTime)
                {
                    EchoContext context = pending[i].Context;
                    RemoveAt(i);
                    Resolve(in context);
                    continue;
                }

                i++;
            }

            if (pendingCount == 0)
            {
                StopTicking();
            }
        }

        // Distance from a point to this object's nearest surface, minus the profile's grace. Falls
        // back to the anchor point when no supported collider is available.
        private float ReachDistance(Vector3 point)
        {
            float nearest = float.MaxValue;

            for (int i = 0; i < surface.Length; i++)
            {
                Collider surfaceCollider = surface[i];
                if (surfaceCollider == null || !surfaceCollider.enabled || !surfaceCollider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = Vector3.Distance(point, surfaceCollider.ClosestPoint(point));
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            if (nearest == float.MaxValue)
            {
                nearest = Vector3.Distance(point, AnchorPosition);
            }

            return Mathf.Max(0f, nearest - profile.ReachGrace);
        }

        private void Resolve(in EchoContext context)
        {
            bool engaged = ShouldEngage(in context);

            if (engaged)
            {
                nextEngageTime = Time.time + profile.RefractoryPeriod;
                if (profile.EngageOnce)
                {
                    isSpent = true;
                }
            }
            else if (!profile.Acknowledge)
            {
                return;
            }

            SpawnAnsweringShell(engaged);

            if (engaged)
            {
                Engaged?.Invoke(context);
            }
            else
            {
                Acknowledged?.Invoke(context);
            }
        }

        private bool ShouldEngage(in EchoContext context)
        {
            if (isSpent || Time.time < nextEngageTime)
            {
                return false;
            }

            if (context.Wave.Charge < profile.MinimumCharge)
            {
                return false;
            }

            if (profile.EngageDistance > 0f && context.Distance > profile.EngageDistance)
            {
                return false;
            }

            // Checked last: the only test that touches physics.
            return !profile.RequireLineOfSight || HasLineOfSight(context.Wave.Origin);
        }

        private bool HasLineOfSight(Vector3 from)
        {
            Vector3 delta = AnchorPosition - from;
            float distance = delta.magnitude;
            if (distance < 0.001f)
            {
                return true;
            }

            int count = Physics.RaycastNonAlloc(
                from,
                delta / distance,
                OcclusionHits,
                distance,
                profile.OcclusionMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hit = OcclusionHits[i].collider;

                // The object's own colliders never block its own echo.
                if (hit != null && !hit.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        private void SpawnAnsweringShell(bool engaged)
        {
            if (!profile.EmitShell || poolService == null || profile.ShellPrefab == null)
            {
                return;
            }

            if (EchoShell.TrySpawn(poolService, profile.ShellPrefab, AnchorPosition, out EchoShell shell))
            {
                shell.Play(
                    profile.ShellRadius,
                    profile.ShellDuration,
                    profile.ShellStartRadius01,
                    profile.ShellColourFor(engaged),
                    AnchorTransform,
                    profile.ShellProfile);
            }
        }

        private void CacheSurface()
        {
            Collider[] source = reachColliders != null && reachColliders.Length > 0
                ? reachColliders
                : GetComponentsInChildren<Collider>(true);

            int usable = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (SupportsClosestPoint(source[i]))
                {
                    usable++;
                }
                else if (source[i] != null)
                {
                    Debug.LogWarning($"EchoResponder: '{source[i].name}' cannot report a closest point (only Box, Sphere, Capsule and convex Mesh colliders can), so it is ignored for reach.", source[i]);
                }
            }

            surface = new Collider[usable];
            int write = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (SupportsClosestPoint(source[i]))
                {
                    surface[write] = source[i];
                    write++;
                }
            }

            if (usable == 0)
            {
                Debug.LogWarning("EchoResponder: No usable colliders, so reach is measured to the anchor point.", this);
            }
        }

        private static bool SupportsClosestPoint(Collider candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (candidate is MeshCollider meshCollider)
            {
                return meshCollider.convex;
            }

            return candidate is BoxCollider || candidate is SphereCollider || candidate is CapsuleCollider;
        }

        private void RemoveAt(int index)
        {
            int last = pendingCount - 1;
            pending[index] = pending[last];
            pending[last] = default;
            pendingCount = last;
        }

        private void StartTicking()
        {
            if (isTicking || tickManager == null)
            {
                return;
            }

            tickManager.Register(this);
            isTicking = true;
        }

        private void StopTicking()
        {
            if (!isTicking)
            {
                return;
            }

            isTicking = false;

            if (tickManager != null)
            {
                tickManager.Unregister(this);
            }
        }

#if UNITY_EDITOR
        // Approximate engage zone: the object's bounds grown by engage distance plus grace. Exact on
        // the faces, slightly generous at the corners, where the true zone is rounded.
        private void OnDrawGizmosSelected()
        {
            if (profile == null || profile.EngageDistance <= 0f)
            {
                return;
            }

            Collider[] colliders = reachColliders != null && reachColliders.Length > 0
                ? reachColliders
                : GetComponentsInChildren<Collider>();

            bool hasBounds = false;
            Bounds combined = new Bounds(AnchorPosition, Vector3.zero);

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                {
                    continue;
                }

                if (hasBounds)
                {
                    combined.Encapsulate(colliders[i].bounds);
                }
                else
                {
                    combined = colliders[i].bounds;
                    hasBounds = true;
                }
            }

            float reach = profile.EngageDistance + profile.ReachGrace;
            Gizmos.color = new Color(0.4f, 1f, 0.9f, 0.5f);
            Gizmos.DrawWireCube(combined.center, combined.size + Vector3.one * (2f * reach));
        }
#endif
    }
}