// LocomotionProfileSO
// Responsibility: Shared, read-only tuning for first person locomotion: ground and air speeds,
// jump, capsule dimensions and eye heights. Flyweight asset; never written to at runtime.
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "LocomotionProfile", menuName = "Game/Player/Locomotion Profile")]
    public sealed class LocomotionProfileSO : ScriptableObject
    {
        [Header("Ground")]
        [SerializeField, Min(0f)] private float walkSpeed = 3f;
        [SerializeField, Min(0f)] private float jogSpeed = 5f;
        [SerializeField, Min(0f)] private float crouchSpeed = 1.6f;
        [Tooltip("How fast ground velocity builds when starting, turning or speeding up. Lower feels heavier.")]
        [FormerlySerializedAs("groundSharpness")]
        [SerializeField, Min(0.01f)] private float groundAcceleration = 10f;
        [Tooltip("How fast ground velocity drops when stopping, slowing or reversing. Higher stops crisper.")]
        [SerializeField, Min(0.01f)] private float groundDeceleration = 14f;

        [Header("Air")]
        [SerializeField, Min(0f)] private float maxAirSpeed = 4f;
        [SerializeField, Min(0f)] private float airAcceleration = 12f;
        [SerializeField, Min(0f)] private float airDrag = 0.1f;
        [SerializeField, Min(0f)] private float gravity = 20f;

        [Header("Jump")]
        [Tooltip("Apex height of a tapped jump, in metres. Always reached, however quickly the button is released.")]
        [SerializeField, Min(0f)] private float jumpHeight = 2f;
        [Tooltip("Apex height when the button is held through the whole rise, in metres. Equal to Jump Height disables the hold extension.")]
        [SerializeField, Min(0f)] private float maxJumpHeight = 3f;
        [Tooltip("A jump pressed up to this many seconds before landing still fires on landing.")]
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;
        [Tooltip("Seconds after leaving a ledge during which a jump is still allowed.")]
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;

        [Header("Capsule")]
        [SerializeField, Min(0.05f)] private float capsuleRadius = 0.35f;
        [SerializeField, Min(0.1f)] private float standingHeight = 1.8f;
        [SerializeField, Min(0.1f)] private float crouchHeight = 1.1f;

        [Header("Eyes")]
        [SerializeField, Min(0f)] private float standingEyeHeight = 1.65f;
        [SerializeField, Min(0f)] private float crouchEyeHeight = 0.95f;
        [Tooltip("How fast the eye height blends between standing and crouching.")]
        [SerializeField, Min(0.01f)] private float eyeHeightSharpness = 10f;

        public float WalkSpeed => walkSpeed;
        public float JogSpeed => jogSpeed;
        public float CrouchSpeed => crouchSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;

        public float MaxAirSpeed => maxAirSpeed;
        public float AirAcceleration => airAcceleration;
        public float AirDrag => airDrag;
        public float Gravity => gravity;

        // Launch speed that reaches Jump Height under normal gravity.
        public float JumpSpeed => Mathf.Sqrt(2f * gravity * jumpHeight);

        // Gravity scale while rising with the button held. Same launch speed under this gravity
        // peaks at exactly Max Jump Height, because apex height is inversely proportional to gravity.
        public float HeldJumpGravityScale => maxJumpHeight > 0f ? jumpHeight / maxJumpHeight : 1f;

        public float JumpBufferTime => jumpBufferTime;
        public float CoyoteTime => coyoteTime;

        public float CapsuleRadius => capsuleRadius;
        public float StandingHeight => standingHeight;
        public float CrouchHeight => crouchHeight;

        public float StandingEyeHeight => standingEyeHeight;
        public float CrouchEyeHeight => crouchEyeHeight;
        public float EyeHeightSharpness => eyeHeightSharpness;

        private void OnValidate()
        {
            maxJumpHeight = Mathf.Max(maxJumpHeight, jumpHeight);
            standingHeight = Mathf.Max(standingHeight, capsuleRadius * 2f);
            crouchHeight = Mathf.Clamp(crouchHeight, capsuleRadius * 2f, standingHeight);
            standingEyeHeight = Mathf.Min(standingEyeHeight, standingHeight);
            crouchEyeHeight = Mathf.Min(crouchEyeHeight, crouchHeight);
        }
    }
}