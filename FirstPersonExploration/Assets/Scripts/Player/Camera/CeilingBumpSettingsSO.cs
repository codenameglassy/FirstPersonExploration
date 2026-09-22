// CeilingBumpSettingsSO
// Responsibility: Shared, read-only tuning for the ceiling bump camera jolt: which impacts count,
// how far the head is knocked down, and the stiff spring that snaps it back. Flyweight asset;
// never written to at runtime.
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "CeilingBumpSettings", menuName = "Game/Player/Ceiling Bump Settings")]
    public sealed class CeilingBumpSettingsSO : ScriptableObject
    {
        [Header("Impact Speed Range (m/s, upward)")]
        [Tooltip("Bumps slower than this are ignored.")]
        [SerializeField, Min(0f)] private float minImpactSpeed = 1f;

        [Tooltip("Bumps at or above this speed get the maximum jolt. Your full jump launch speed is a good value.")]
        [SerializeField, Min(0f)] private float maxImpactSpeed = 9f;

        [Header("Jolt")]
        [Tooltip("Head drop in metres at Min Impact Speed.")]
        [SerializeField, Min(0f)] private float minDrop = 0.015f;

        [Tooltip("Head drop in metres at Max Impact Speed.")]
        [SerializeField, Min(0.001f)] private float maxDrop = 0.05f;

        [Tooltip("Downward nod in degrees at the deepest point of a max jolt. Scales with the actual drop.")]
        [SerializeField, Min(0f)] private float maxNod = 4f;

        [Header("Spring")]
        [Tooltip("Oscillation speed in cycles per second. Keep it high so the bump reads as a sharp knock.")]
        [SerializeField, Min(0.1f)] private float frequency = 5f;

        [Tooltip("1 = no overshoot. Lower = more rebound. A little rebound sells the knock.")]
        [SerializeField, Range(0.1f, 0.95f)] private float dampingRatio = 0.35f;

        public float MinImpactSpeed => minImpactSpeed;
        public float MaxImpactSpeed => maxImpactSpeed;
        public float MinDrop => minDrop;
        public float MaxDrop => maxDrop;
        public float MaxNod => maxNod;
        public float Frequency => frequency;
        public float DampingRatio => dampingRatio;

        private void OnValidate()
        {
            maxImpactSpeed = Mathf.Max(maxImpactSpeed, minImpactSpeed + 0.1f);
            minDrop = Mathf.Min(minDrop, maxDrop);
        }
    }
}