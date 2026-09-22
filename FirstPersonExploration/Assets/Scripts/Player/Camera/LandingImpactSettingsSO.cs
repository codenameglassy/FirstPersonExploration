// LandingImpactSettingsSO
// Responsibility: Shared, read-only tuning for the landing camera dip: which falls count, how deep
// the dip gets, and the spring that recovers from it. Flyweight asset; never written at runtime.
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "LandingImpactSettings", menuName = "Game/Player/Landing Impact Settings")]
    public sealed class LandingImpactSettingsSO : ScriptableObject
    {
        [Header("Fall Speed Range (m/s, downward)")]
        [Tooltip("Landings slower than this are ignored. Keeps step-offs and small drops silent.")]
        [SerializeField, Min(0f)] private float minFallSpeed = 3.5f;

        [Tooltip("Landings at or above this speed get the maximum dip.")]
        [SerializeField, Min(0f)] private float maxFallSpeed = 16f;

        [Header("Dip")]
        [Tooltip("Dip depth in metres at Min Fall Speed.")]
        [SerializeField, Min(0f)] private float minDrop = 0.04f;

        [Tooltip("Dip depth in metres at Max Fall Speed.")]
        [SerializeField, Min(0.01f)] private float maxDrop = 0.2f;

        [Tooltip("Downward nod in degrees at the deepest point of a max dip. Scales with the actual dip.")]
        [SerializeField, Min(0f)] private float maxNod = 3f;

        [Header("Spring")]
        [Tooltip("Oscillation speed in cycles per second. Higher = snappier recovery.")]
        [SerializeField, Min(0.1f)] private float frequency = 2.5f;

        [Tooltip("1 = no overshoot. Lower = more bounce back past rest. 0.4 to 0.6 reads as legs absorbing the fall.")]
        [SerializeField, Range(0.1f, 0.95f)] private float dampingRatio = 0.45f;

        public float MinFallSpeed => minFallSpeed;
        public float MaxFallSpeed => maxFallSpeed;
        public float MinDrop => minDrop;
        public float MaxDrop => maxDrop;
        public float MaxNod => maxNod;
        public float Frequency => frequency;
        public float DampingRatio => dampingRatio;

        private void OnValidate()
        {
            maxFallSpeed = Mathf.Max(maxFallSpeed, minFallSpeed + 0.1f);
            minDrop = Mathf.Min(minDrop, maxDrop);
        }
    }
}