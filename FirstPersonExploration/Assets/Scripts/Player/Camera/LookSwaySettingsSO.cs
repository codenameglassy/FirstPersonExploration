// LookSwaySettingsSO
// Responsibility: Shared, read-only tuning for look sway: how far the view rolls when turning, at
// what turn speed the roll is full, and how smoothly it follows. Flyweight asset; never written to
// at runtime.
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "LookSwaySettings", menuName = "Game/Player/Look Sway Settings")]
    public sealed class LookSwaySettingsSO : ScriptableObject
    {
        [Tooltip("Roll at full turn speed, in degrees. Positive banks into the turn, negative away from it. Keep it around 1.")]
        [SerializeField] private float maxRoll = 1.2f;

        [Tooltip("Turn speed (degrees per second) at which the roll is full. A brisk mouse flick is around 360.")]
        [SerializeField, Min(1f)] private float fullSwayTurnRate = 360f;

        [Tooltip("How closely the roll follows the turn. Lower feels heavier and smooths out mouse jitter; higher feels more responsive.")]
        [SerializeField, Min(0.01f)] private float sharpness = 8f;

        public float MaxRoll => maxRoll;
        public float FullSwayTurnRate => fullSwayTurnRate;
        public float Sharpness => sharpness;
    }
}