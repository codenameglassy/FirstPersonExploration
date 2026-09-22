// BreathingSettingsSO
// Responsibility: Shared, read-only tuning for idle breathing: calm and exerted breath cycles,
// how exertion builds and recovers, organic drift, and when the effect fades in. Flyweight asset;
// never written to at runtime.
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "BreathingSettings", menuName = "Game/Player/Breathing Settings")]
    public sealed class BreathingSettingsSO : ScriptableObject
    {
        [Header("Calm Breath")]
        [Tooltip("Breaths per second at rest. 0.25 is 15 breaths a minute.")]
        [SerializeField, Min(0.01f)] private float calmRate = 0.25f;
        [Tooltip("Vertical rise on each inhale, in metres.")]
        [SerializeField, Min(0f)] private float calmDepth = 0.006f;
        [Tooltip("Upward tilt on each inhale, in degrees.")]
        [SerializeField, Min(0f)] private float calmPitch = 0.25f;

        [Header("Exerted Breath")]
        [Tooltip("Breaths per second when fully out of breath.")]
        [SerializeField, Min(0.01f)] private float exertedRate = 0.6f;
        [Tooltip("Vertical rise on each inhale when fully out of breath, in metres.")]
        [SerializeField, Min(0f)] private float exertedDepth = 0.016f;
        [Tooltip("Upward tilt on each inhale when fully out of breath, in degrees.")]
        [SerializeField, Min(0f)] private float exertedPitch = 0.6f;

        [Header("Exertion")]
        [Tooltip("Seconds of continuous jogging to become fully out of breath.")]
        [SerializeField, Min(0.1f)] private float timeToExhaust = 6f;
        [Tooltip("Seconds to recover from fully out of breath to calm.")]
        [SerializeField, Min(0.1f)] private float recoveryTime = 8f;

        [Header("Drift")]
        [Tooltip("Maximum irregular sway on pitch, yaw and roll, in degrees.")]
        [SerializeField, Min(0f)] private float driftAngle = 0.3f;
        [Tooltip("How fast the drift wanders. Keep it slow; fast drift reads as shaking.")]
        [SerializeField, Min(0f)] private float driftSpeed = 0.15f;

        [Header("Fade")]
        [Tooltip("Planar speed (m/s) at which breathing has fully faded out. Headbob takes over from here.")]
        [SerializeField, Min(0.01f)] private float fadeOutSpeed = 1.5f;
        [Tooltip("How fast breathing fades in and out. Low values let it settle in gently after stopping.")]
        [SerializeField, Min(0.01f)] private float fadeSharpness = 2f;

        public float CalmRate => calmRate;
        public float CalmDepth => calmDepth;
        public float CalmPitch => calmPitch;
        public float ExertedRate => exertedRate;
        public float ExertedDepth => exertedDepth;
        public float ExertedPitch => exertedPitch;
        public float TimeToExhaust => timeToExhaust;
        public float RecoveryTime => recoveryTime;
        public float DriftAngle => driftAngle;
        public float DriftSpeed => driftSpeed;
        public float FadeOutSpeed => fadeOutSpeed;
        public float FadeSharpness => fadeSharpness;
    }
}