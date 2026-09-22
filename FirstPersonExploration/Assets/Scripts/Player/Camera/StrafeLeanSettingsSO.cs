// StrafeLeanSettingsSO
// Responsibility: Shared, read-only tuning for the strafe lean: how far the view rolls and shifts
// into sideways movement, at what speed the lean is full, and how fast it eases. Flyweight asset;
// never written to at runtime.
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "StrafeLeanSettings", menuName = "Game/Player/Strafe Lean Settings")]
    public sealed class StrafeLeanSettingsSO : ScriptableObject
    {
        [Tooltip("Roll into the strafe direction at full lean, in degrees. 1.5 to 3 reads as body lean; more reads as tilting the camera.")]
        [SerializeField, Min(0f)] private float maxLeanAngle = 2f;

        [Tooltip("Sideways head shift into the strafe at full lean, in metres.")]
        [SerializeField, Min(0f)] private float maxLeanOffset = 0.02f;

        [Tooltip("Sideways speed (m/s) at which the lean is full. Slower strafes lean proportionally less.")]
        [SerializeField, Min(0.01f)] private float fullLeanSpeed = 5f;

        [Tooltip("How fast the lean eases in and out. Lower feels heavier and more deliberate.")]
        [SerializeField, Min(0.01f)] private float leanSharpness = 6f;

        public float MaxLeanAngle => maxLeanAngle;
        public float MaxLeanOffset => maxLeanOffset;
        public float FullLeanSpeed => fullLeanSpeed;
        public float LeanSharpness => leanSharpness;
    }
}