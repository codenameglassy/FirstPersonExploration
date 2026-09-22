// CeilingBumpEffect
// Responsibility: Camera jolt when the head hits a ceiling, as a camera effect layer. Listens to the
// character's CeilingBumped event, converts upward impact speed into a downward kick on a stiff
// damped spring, and applies the result as a head drop and nod.
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(CameraEffectStack))]
    public sealed class CeilingBumpEffect : MonoBehaviour, ICameraEffect
    {
        [SerializeField] private FirstPersonCharacter character;
        [SerializeField] private CeilingBumpSettingsSO settings;

        [Tooltip("Global comfort multiplier. 0 disables the jolt entirely.")]
        [SerializeField, Range(0f, 1f)] private float intensity = 1f;

        private CameraEffectStack stack;
        private bool isConfigured;

        private DampedSpring spring;
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
                Debug.LogError("CeilingBumpEffect: Character or CeilingBumpSettingsSO is not assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!isConfigured)
            {
                return;
            }

            character.CeilingBumped += HandleCeilingBumped;
            if (stack != null)
            {
                stack.Register(this);
            }
        }

        private void OnDisable()
        {
            if (character != null)
            {
                character.CeilingBumped -= HandleCeilingBumped;
            }

            if (stack != null)
            {
                stack.Unregister(this);
            }

            spring.Reset();
            pendingKick = 0f;
        }

        public void Evaluate(float deltaTime, ref CameraOffset offset)
        {
            if (pendingKick > 0f)
            {
                spring.Kick(-pendingKick);
                pendingKick = 0f;
            }

            spring.Step(deltaTime, settings.Frequency, settings.DampingRatio);

            if (spring.IsAtRest || intensity <= 0f)
            {
                return;
            }

            offset.Position.y += spring.Position * intensity;
            offset.Rotation.x += (-spring.Position / settings.MaxDrop) * settings.MaxNod * intensity;
        }

        // Called on the physics tick after the bump. Stored and applied on the next rendered frame.
        private void HandleCeilingBumped(float upwardSpeed)
        {
            if (upwardSpeed < settings.MinImpactSpeed)
            {
                return;
            }

            float t = Mathf.InverseLerp(settings.MinImpactSpeed, settings.MaxImpactSpeed, upwardSpeed);
            float drop = Mathf.Lerp(settings.MinDrop, settings.MaxDrop, t);

            pendingKick = Mathf.Max(pendingKick, DampedSpring.SolveKickForPeak(drop, settings.Frequency, settings.DampingRatio));
        }
    }
}