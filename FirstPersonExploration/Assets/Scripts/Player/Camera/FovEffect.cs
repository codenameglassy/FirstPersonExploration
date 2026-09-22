// FovEffect
// Responsibility: Per-state field of view as a camera effect layer. Locomotion (idle, walk, jog)
// picks the standing target, crouch blends toward the crouch offset in sync with the eye height,
// a fall offset scales in with downward speed, and the result eases smoothly into the stack's FOV
// delta.
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(CameraEffectStack))]
    public sealed class FovEffect : MonoBehaviour, ICameraEffect
    {
        [SerializeField] private FirstPersonCharacter character;
        [SerializeField] private FovSettingsSO settings;

        private CameraEffectStack stack;
        private bool isConfigured;

        private float currentOffset;
        private float crouchWeight;

        private void Awake()
        {
            stack = GetComponent<CameraEffectStack>();

            isConfigured = character != null && character.Profile != null && settings != null;
            if (!isConfigured)
            {
                Debug.LogError("FovEffect: Character, its LocomotionProfileSO, or FovSettingsSO is not assigned.", this);
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

            currentOffset = 0f;
        }

        public void Evaluate(float deltaTime, ref CameraOffset offset)
        {
            // Crouch eases at the same rate as the eye height, so the FOV change lands with the visual duck.
            float crouchTarget = character.IsCrouching ? 1f : 0f;
            crouchWeight = Mathf.Lerp(crouchWeight, crouchTarget, 1f - Mathf.Exp(-character.Profile.EyeHeightSharpness * deltaTime));

            float standingOffset = settings.IdleOffset;
            if (character.HasMoveInput)
            {
                standingOffset = character.IsJogging ? settings.JogOffset : settings.WalkOffset;
            }

            float targetOffset = Mathf.Lerp(standingOffset, settings.CrouchOffset, crouchWeight);

            // Falling only: rising from a jump contributes nothing; widening grows with downward speed.
            if (!character.IsGrounded)
            {
                float verticalSpeed = Vector3.Dot(character.Velocity, character.Motor.CharacterUp);
                float fallSpeed = Mathf.Max(0f, -verticalSpeed);
                targetOffset += settings.FallOffset * Mathf.InverseLerp(settings.FallSpeedStart, settings.FallSpeedFull, fallSpeed);
            }

            currentOffset = Mathf.Lerp(currentOffset, targetOffset, 1f - Mathf.Exp(-settings.TransitionSharpness * deltaTime));

            offset.FieldOfView += currentOffset;
        }
    }
}