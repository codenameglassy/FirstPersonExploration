// LandingImpactEffect
// Responsibility: Camera dip on landing as a camera effect layer. Listens to the character's Landed
// event, converts fall speed into a downward kick on a damped spring, and integrates the spring per
// frame. The kick is solved so the deepest point of the dip matches the authored depth exactly.
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(CameraEffectStack))]
    public sealed class LandingImpactEffect : MonoBehaviour, ICameraEffect
    {
        // Substep size keeps the spring stable through frame spikes, which are common on WebGL.
        private const float MaxStep = 1f / 120f;
        private const float SettlePosition = 0.00001f;
        private const float SettleVelocity = 0.0001f;

        [SerializeField] private FirstPersonCharacter character;
        [SerializeField] private LandingImpactSettingsSO settings;

        [Tooltip("Global comfort multiplier. 0 disables the landing dip entirely.")]
        [SerializeField, Range(0f, 1f)] private float intensity = 1f;

        private CameraEffectStack stack;
        private bool isConfigured;

        private float position;
        private float velocity;
        private float pendingKick;

        public float Intensity
        {
            get => intensity;
            set => intensity = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            stack = GetComponent<CameraEffectStack>();

            isConfigured = character != null && settings != null;
            if (!isConfigured)
            {
                Debug.LogError("LandingImpactEffect: Character or LandingImpactSettingsSO is not assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!isConfigured)
            {
                return;
            }

            character.Landed += HandleLanded;
            if (stack != null)
            {
                stack.Register(this);
            }
        }

        private void OnDisable()
        {
            if (character != null)
            {
                character.Landed -= HandleLanded;
            }

            if (stack != null)
            {
                stack.Unregister(this);
            }

            position = 0f;
            velocity = 0f;
            pendingKick = 0f;
        }

        public void Evaluate(float deltaTime, ref CameraOffset offset)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (pendingKick > 0f)
            {
                velocity -= pendingKick;
                pendingKick = 0f;
            }

            if (position == 0f && velocity == 0f)
            {
                return;
            }

            IntegrateSpring(deltaTime);

            float scale = intensity;
            if (scale <= 0f)
            {
                return;
            }

            offset.Position.y += position * scale;
            offset.Rotation.x += (-position / settings.MaxDrop) * settings.MaxNod * scale;
        }

        // Called on the physics tick the character lands. Stored and applied on the next rendered frame.
        private void HandleLanded(float fallSpeed)
        {
            if (fallSpeed < settings.MinFallSpeed)
            {
                return;
            }

            float t = Mathf.InverseLerp(settings.MinFallSpeed, settings.MaxFallSpeed, fallSpeed);
            float drop = Mathf.Lerp(settings.MinDrop, settings.MaxDrop, t);

            // Two landings between frames keep the stronger kick rather than stacking.
            pendingKick = Mathf.Max(pendingKick, SolveKick(drop));
        }

        // Initial downward velocity that makes an underdamped spring peak at exactly `drop`.
        // Peak displacement = (v0 / omega) * exp(-zeta * acos(zeta) / sqrt(1 - zeta^2)).
        private float SolveKick(float drop)
        {
            float zeta = settings.DampingRatio;
            float omega = 2f * Mathf.PI * settings.Frequency;
            float peakFactor = Mathf.Exp(-zeta * Mathf.Acos(zeta) / Mathf.Sqrt(1f - zeta * zeta));
            return drop * omega / peakFactor;
        }

        private void IntegrateSpring(float deltaTime)
        {
            float omega = 2f * Mathf.PI * settings.Frequency;
            float stiffness = omega * omega;
            float damping = 2f * settings.DampingRatio * omega;

            int steps = Mathf.CeilToInt(deltaTime / MaxStep);
            float h = deltaTime / steps;

            for (int i = 0; i < steps; i++)
            {
                // Semi-implicit Euler: velocity first, then position. Stable at this step size.
                float acceleration = -stiffness * position - damping * velocity;
                velocity += acceleration * h;
                position += velocity * h;
            }

            if (Mathf.Abs(position) < SettlePosition && Mathf.Abs(velocity) < SettleVelocity)
            {
                position = 0f;
                velocity = 0f;
            }
        }
    }
}