// EchoEmitter
// Responsibility: The player's echo. Charges while held and fires on release, driven only by
// commands so player input and scripted sequences share one pipeline. Firing shows a pooled
// EchoShell and raises one EchoWave on the channel; it never touches responders directly.
using System;
using Game.Core;
using UnityEngine;

namespace Game.Echo
{
    [DisallowMultipleComponent]
    public sealed class EchoEmitter : MonoBehaviour, ITickable
    {
        [SerializeField] private EchoEmitterProfileSO profile;
        [SerializeField] private EchoWaveEventChannelSO waveChannel;
        [Tooltip("Where waves originate. Chest height reads best. Empty uses this transform.")]
        [SerializeField] private Transform origin;
        [SerializeField, Min(1)] private int commandCapacity = 8;

        private CommandQueue commands;
        private TickManager tickManager;
        private PoolService poolService;
        private bool wantsCharge;
        private bool isCharging;
        private float chargeTimer;
        private float cooldownTimer;

        public event Action ChargeStarted;
        public event Action ChargeCancelled;
        public event Action<EchoWave> Fired;

        public bool IsCharging => isCharging;
        public bool IsReady => cooldownTimer <= 0f;
        public float Charge01 => profile != null ? Mathf.Clamp01(chargeTimer / profile.MaxChargeTime) : 0f;

        private Vector3 OriginPosition => origin != null ? origin.position : transform.position;

        private void Awake()
        {
            commands = new CommandQueue(commandCapacity);

            if (profile == null)
            {
                Debug.LogError("EchoEmitter: No emitter profile assigned.", this);
            }
        }

        private void OnEnable()
        {
            if (ServiceRegistry.TryGet(out tickManager))
            {
                tickManager.Register(this);
            }
            else
            {
                Debug.LogError("EchoEmitter: TickManager not found. Is a Bootstrapper in the scene?", this);
            }

            if (ServiceRegistry.TryGet(out poolService))
            {
                if (profile != null && profile.ShellPrefab != null)
                {
                    poolService.Prewarm(profile.ShellPrefab, profile.ShellPrewarmCount);
                }
            }
            else
            {
                Debug.LogError("EchoEmitter: PoolService not found. Is a Bootstrapper in the scene?", this);
            }
        }

        private void OnDisable()
        {
            if (tickManager != null)
            {
                tickManager.Unregister(this);
                tickManager = null;
            }

            poolService = null;
            commands.Clear();
            CancelCharge();
        }

        public bool Enqueue(ICommand command)
        {
            if (commands == null || !isActiveAndEnabled)
            {
                return false;
            }

            return commands.Enqueue(command);
        }

        public void Tick(float deltaTime)
        {
            commands.ExecuteAll();

            if (cooldownTimer > 0f)
            {
                cooldownTimer -= deltaTime;
            }

            if (wantsCharge && !isCharging && IsReady && profile != null)
            {
                StartCharging();
            }

            if (isCharging)
            {
                chargeTimer = Mathf.Min(chargeTimer + deltaTime, profile.MaxChargeTime);
            }
        }

        public void BeginCharge()
        {
            wantsCharge = true;
        }

        public void ReleaseCharge()
        {
            wantsCharge = false;

            if (!isCharging)
            {
                return;
            }

            float charge = Charge01;
            StopCharging();
            Fire(charge);
        }

        public void CancelCharge()
        {
            wantsCharge = false;

            if (!isCharging)
            {
                return;
            }

            StopCharging();
            ChargeCancelled?.Invoke();
        }

        // Scripted path: fires immediately at the given charge and ignores cooldown, so a sequence
        // echo always lands. It still starts the cooldown for the player.
        public void Fire(float charge)
        {
            if (profile == null)
            {
                return;
            }

            charge = Mathf.Clamp01(charge);

            EchoWave wave = new EchoWave(
                OriginPosition,
                charge,
                profile.RadiusAt(charge),
                profile.DurationAt(charge),
                profile.StartRadius01,
                Time.time);

            SpawnShell(in wave);
            cooldownTimer = profile.Cooldown;

            if (waveChannel != null)
            {
                waveChannel.Raise(wave);
            }

            Fired?.Invoke(wave);
        }

        private void StartCharging()
        {
            isCharging = true;
            chargeTimer = 0f;
            ChargeStarted?.Invoke();
        }

        private void StopCharging()
        {
            isCharging = false;
            chargeTimer = 0f;
        }

        private void SpawnShell(in EchoWave wave)
        {
            if (poolService == null || profile.ShellPrefab == null)
            {
                return;
            }

            if (EchoShell.TrySpawn(poolService, profile.ShellPrefab, wave.Origin, out EchoShell shell))
            {
                shell.Play(wave.Radius, wave.Duration, wave.StartRadius01, profile.ColourAt(wave.Charge), null, profile.ShellProfile);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (profile == null)
            {
                return;
            }

            Vector3 position = OriginPosition;
            Gizmos.color = new Color(1f, 0.85f, 0.5f, 0.35f);
            Gizmos.DrawWireSphere(position, profile.MinRadius);
            Gizmos.color = new Color(1f, 0.85f, 0.5f, 0.18f);
            Gizmos.DrawWireSphere(position, profile.MaxRadius);
        }
#endif
    }
}
