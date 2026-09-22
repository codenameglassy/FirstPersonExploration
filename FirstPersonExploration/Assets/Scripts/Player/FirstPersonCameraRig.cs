// FirstPersonCameraRig
// Responsibility: Owns the first person view. Accumulates yaw and pitch from look deltas and places
// itself at the character's interpolated eye point every rendered frame. Must be a separate root,
// never a child of the character, so look is applied per frame instead of per physics tick.
using UnityEngine;

namespace Game.Player
{
    [DefaultExecutionOrder(ExecutionOrder)]
    public sealed class FirstPersonCameraRig : MonoBehaviour
    {
        // Runs after KCC's LateUpdate interpolation so the eye point is never one frame behind.
        private const int ExecutionOrder = 32000;

        [SerializeField] private FirstPersonCharacter character;
        [SerializeField, Range(1f, 89f)] private float maxPitch = 85f;

        private float yaw;
        private float pitch;
        private float eyeHeight;

        public Quaternion LookRotation => Quaternion.Euler(pitch, yaw, 0f);

        // Yaw only. Camera effects offset position in this frame so bob stays level when looking up or down.
        public Quaternion YawRotation => Quaternion.Euler(0f, yaw, 0f);

        private void Awake()
        {
            if (character == null || character.Profile == null)
            {
                Debug.LogError("FirstPersonCameraRig: Character or its LocomotionProfileSO is not assigned.", this);
                enabled = false;
                return;
            }

            yaw = character.transform.eulerAngles.y;
            pitch = 0f;
            eyeHeight = character.Profile.StandingEyeHeight;
        }

        // delta.x = yaw in degrees (positive turns right), delta.y = pitch in degrees (positive looks up).
        public void AddLookDelta(Vector2 delta)
        {
            yaw = Mathf.Repeat(yaw + delta.x, 360f);
            pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
        }

        private void LateUpdate()
        {
            LocomotionProfileSO profile = character.Profile;
            float targetEyeHeight = character.IsCrouching ? profile.CrouchEyeHeight : profile.StandingEyeHeight;
            eyeHeight = Mathf.Lerp(eyeHeight, targetEyeHeight, 1f - Mathf.Exp(-profile.EyeHeightSharpness * Time.deltaTime));

            Transform body = character.transform;
            transform.SetPositionAndRotation(body.position + body.up * eyeHeight, LookRotation);
        }
    }
}