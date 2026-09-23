// GrassInteractionDriver
// Responsibility: Feeds the grass shader its world interactions as shader globals: the pusher's
// position for player push, and up to four active echo wavefronts for ripples. Ripple fronts come
// from EchoWave.FrontRadiusAt and fade with EchoCurves.Alpha01, the same curves that drive the echo
// shell and responders, so grass, shell and responders stay in lockstep. Globals are only pushed
// when something changed. One per scene.
using System;
using Game.Core;
using Game.Echo;
using UnityEngine;

namespace Game.Foliage
{
    [DisallowMultipleComponent]
    public sealed class GrassInteractionDriver : MonoBehaviour, ITickable
    {
        // Must match GRASS_MAX_RIPPLES in GrassCommon.hlsl.
        private const int MaxRipples = 4;

        // Same default as EchoShellProfileSO. Used only when no shell profile is assigned.
        private const float DefaultAlphaPeak = 0.12f;

        private const float MoveThresholdSqr = 0.0001f;

        private static readonly int PusherId = Shader.PropertyToID("_GrassPusher");
        private static readonly int RipplesId = Shader.PropertyToID("_GrassRipples");
        private static readonly int RippleAmplitudesId = Shader.PropertyToID("_GrassRippleAmplitudes");

        [Tooltip("Transform whose position pushes grass aside. Use a transform at the player's feet.")]
        [SerializeField] private Transform pusher;

        [SerializeField] private EchoWaveEventChannelSO echoWaveChannel;

        [Tooltip("The player's echo shell profile, so ripples fade exactly like the shell.")]
        [SerializeField] private EchoShellProfileSO shellProfile;

        [Tooltip("Ripple strength for a zero-charge echo. Full charge is always 1.")]
        [SerializeField, Range(0f, 1f)] private float minChargeStrength = 0.6f;

        private readonly EchoWave[] waves = new EchoWave[MaxRipples];
        private readonly bool[] slotActive = new bool[MaxRipples];
        private readonly Vector4[] rippleData = new Vector4[MaxRipples];
        private readonly float[] rippleAmplitudes = new float[MaxRipples];

        private Action<EchoWave> onEchoWave;
        private TickManager tickManager;
        private Vector3 lastPusherPosition;
        private bool pusherSent;
        private int activeRipples;
        private bool ripplesDirty;

        private void Awake()
        {
            onEchoWave = HandleEchoWave;
        }

        private void OnEnable()
        {
            if (ServiceRegistry.TryGet(out tickManager))
            {
                tickManager.Register(this);
            }
            else
            {
                Debug.LogError($"{nameof(GrassInteractionDriver)}: TickManager not found. Is a Bootstrapper in the scene?", this);
            }

            if (echoWaveChannel != null)
            {
                echoWaveChannel.Register(onEchoWave);
            }

            ClearGlobals();
        }

        private void OnDisable()
        {
            if (tickManager != null)
            {
                tickManager.Unregister(this);
                tickManager = null;
            }

            if (echoWaveChannel != null)
            {
                echoWaveChannel.Unregister(onEchoWave);
            }

            ClearGlobals();
        }

        public void Tick(float deltaTime)
        {
            UpdatePusher();

            if (activeRipples > 0 || ripplesDirty)
            {
                UpdateRipples();
            }
        }

        private void HandleEchoWave(EchoWave wave)
        {
            int slot = FindSlot();
            waves[slot] = wave;

            if (!slotActive[slot])
            {
                slotActive[slot] = true;
                activeRipples++;
            }

            ripplesDirty = true;
        }

        private int FindSlot()
        {
            int oldest = 0;

            for (int i = 0; i < MaxRipples; i++)
            {
                if (!slotActive[i])
                {
                    return i;
                }

                if (waves[i].EmitTime < waves[oldest].EmitTime)
                {
                    oldest = i;
                }
            }

            return oldest;
        }

        private void UpdatePusher()
        {
            if (pusher == null)
            {
                if (pusherSent)
                {
                    Shader.SetGlobalVector(PusherId, Vector4.zero);
                    pusherSent = false;
                }

                return;
            }

            Vector3 position = pusher.position;
            if (pusherSent && (position - lastPusherPosition).sqrMagnitude < MoveThresholdSqr)
            {
                return;
            }

            Shader.SetGlobalVector(PusherId, new Vector4(position.x, position.y, position.z, 1f));
            lastPusherPosition = position;
            pusherSent = true;
        }

        private void UpdateRipples()
        {
            float now = Time.time;
            float alphaPeak = shellProfile != null ? shellProfile.AlphaPeak : DefaultAlphaPeak;

            for (int i = 0; i < MaxRipples; i++)
            {
                if (!slotActive[i])
                {
                    continue;
                }

                EchoWave wave = waves[i];
                float elapsed = now - wave.EmitTime;
                float t = elapsed / wave.Duration;

                if (t >= 1f)
                {
                    slotActive[i] = false;
                    activeRipples--;
                    rippleData[i] = Vector4.zero;
                    rippleAmplitudes[i] = 0f;
                    continue;
                }

                Vector3 origin = wave.Origin;
                rippleData[i] = new Vector4(origin.x, origin.y, origin.z, wave.FrontRadiusAt(elapsed));
                rippleAmplitudes[i] = EchoCurves.Alpha01(t, alphaPeak) * Mathf.Lerp(minChargeStrength, 1f, wave.Charge);
            }

            Shader.SetGlobalVectorArray(RipplesId, rippleData);
            Shader.SetGlobalFloatArray(RippleAmplitudesId, rippleAmplitudes);
            ripplesDirty = false;
        }

        private void ClearGlobals()
        {
            for (int i = 0; i < MaxRipples; i++)
            {
                slotActive[i] = false;
                rippleData[i] = Vector4.zero;
                rippleAmplitudes[i] = 0f;
            }

            activeRipples = 0;
            ripplesDirty = false;
            pusherSent = false;

            Shader.SetGlobalVector(PusherId, Vector4.zero);
            Shader.SetGlobalVectorArray(RipplesId, rippleData);
            Shader.SetGlobalFloatArray(RippleAmplitudesId, rippleAmplitudes);
        }
    }
}
