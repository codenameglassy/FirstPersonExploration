// StrafeLeanEffect
// Responsibility: Strafe lean as a camera effect layer. Reads the character's sideways velocity
// relative to its facing, and eases a roll and a small head shift into that direction. Driven by
// velocity rather than input, so the lean builds and settles with the body's actual momentum.
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(CameraEffectStack))]
    public sealed class StrafeLeanEffect : MonoBehaviour, ICameraEffect
    {
        [SerializeField] private FirstPersonCharacter character;
        [SerializeField] private StrafeLeanSettingsSO settings;

        [Tooltip("Global comfort multiplier. 0 disables the lean entirely.")]
        [SerializeField, Range(0f, 1f)] private float intensity = 1f;

        private CameraEffectStack stack;
        private bool isConfigured;

        // -1 = full lean left, 0 = upright, 1 = full lean right.
        private float lean;

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
                Debug.LogError("StrafeLeanEffect: Character or StrafeLeanSettingsSO is not assigned.", this);
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

            lean = 0f;
        }

        public void Evaluate(float deltaTime, ref CameraOffset offset)
        {
            // The body's yaw tracks the camera every physics tick, so its right axis is the view's strafe axis.
            float lateralSpeed = Vector3.Dot(character.Velocity, character.transform.right);
            float targetLean = Mathf.Clamp(lateralSpeed / settings.FullLeanSpeed, -1f, 1f);

            lean = Mathf.Lerp(lean, targetLean, 1f - Mathf.Exp(-settings.LeanSharpness * deltaTime));

            float scale = lean * intensity;
            if (scale == 0f)
            {
                return;
            }

            // Positive Z roll tilts left, so moving right (positive lean) needs negative roll.
            offset.Rotation.z -= scale * settings.MaxLeanAngle;
            offset.Position.x += scale * settings.MaxLeanOffset;
        }
    }
}