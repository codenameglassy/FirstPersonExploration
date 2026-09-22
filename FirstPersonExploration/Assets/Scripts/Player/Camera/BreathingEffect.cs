// BreathingEffect
// Responsibility: Idle breathing as a camera effect layer. A breath cycle lifts and tilts the view
// on each inhale, getting faster and deeper with exertion built up by jogging, and slow Perlin drift
// keeps the view organically alive. Fades in when standing still and out when moving or airborne.
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(CameraEffectStack))]
    public sealed class BreathingEffect : MonoBehaviour, ICameraEffect
    {
        private const float TwoPi = Mathf.PI * 2f;

        // Separate Perlin rows per axis so the three drift channels never move in lockstep.
        private const float PitchNoiseRow = 11.3f;
        private const float YawNoiseRow = 47.9f;
        private const float RollNoiseRow = 83.1f;

        [SerializeField] private FirstPersonCharacter character;
        [SerializeField] private BreathingSettingsSO settings;

        [Tooltip("Global comfort multiplier. 0 disables breathing entirely.")]
        [SerializeField, Range(0f, 1f)] private float intensity = 1f;

        private CameraEffectStack stack;
        private bool isConfigured;

        private float breathPhase;
        private float noiseTime;
        private float weight;
        private float exertion;

        public float Intensity
        {
            get => intensity;
            set => intensity = Mathf.Clamp01(value);
        }

        // 0 = calm, 1 = fully out of breath. Readable by other systems, such as breath audio later.
        public float Exertion => exertion;

        private void Awake()
        {
            stack = GetComponent<CameraEffectStack>();

            isConfigured = character != null && settings != null;
            if (!isConfigured)
            {
                Debug.LogError("BreathingEffect: Character or BreathingSettingsSO is not assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (isConfigured && stack != null)
            {
                stack.Register(this);
            }
        }

        private void OnDisable()
        {
            if (stack != null)
            {
                stack.Unregister(this);
            }

            weight = 0f;
        }

        public void Evaluate(float deltaTime, ref CameraOffset offset)
        {
            UpdateExertion(deltaTime);

            float planarSpeed = Vector3.ProjectOnPlane(character.Velocity, character.Motor.CharacterUp).magnitude;
            float targetWeight = character.IsGrounded ? 1f - Mathf.Clamp01(planarSpeed / settings.FadeOutSpeed) : 0f;
            weight = Mathf.Lerp(weight, targetWeight, 1f - Mathf.Exp(-settings.FadeSharpness * deltaTime));

            // Phase keeps running even while faded out, so breathing resumes mid-rhythm instead of restarting.
            float rate = Mathf.Lerp(settings.CalmRate, settings.ExertedRate, exertion);
            breathPhase = Mathf.Repeat(breathPhase + rate * TwoPi * deltaTime, TwoPi);
            noiseTime += settings.DriftSpeed * deltaTime;

            float scale = weight * intensity;
            if (scale <= 0.0001f)
            {
                return;
            }

            float breath = Mathf.Sin(breathPhase);
            float depth = Mathf.Lerp(settings.CalmDepth, settings.ExertedDepth, exertion);
            float pitch = Mathf.Lerp(settings.CalmPitch, settings.ExertedPitch, exertion);

            // Inhale lifts the head and tilts the view slightly up (negative X pitch).
            offset.Position.y += breath * depth * scale;
            offset.Rotation.x -= breath * pitch * scale;

            float drift = settings.DriftAngle * scale;
            offset.Rotation.x += SampleNoise(PitchNoiseRow) * drift;
            offset.Rotation.y += SampleNoise(YawNoiseRow) * drift;
            offset.Rotation.z += SampleNoise(RollNoiseRow) * drift;
        }

        private void UpdateExertion(float deltaTime)
        {
            bool exerting = character.IsJogging && character.HasMoveInput && character.IsGrounded;
            float rate = exerting ? 1f / settings.TimeToExhaust : -1f / settings.RecoveryTime;
            exertion = Mathf.Clamp01(exertion + rate * deltaTime);
        }

        // Perlin noise remapped from 0..1 to -1..1.
        private float SampleNoise(float row)
        {
            return Mathf.PerlinNoise(noiseTime, row) * 2f - 1f;
        }
    }
}