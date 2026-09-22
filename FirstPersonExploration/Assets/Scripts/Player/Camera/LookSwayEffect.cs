// LookSwayEffect
// Responsibility: Look sway as a camera effect layer. Measures the view's yaw turn rate from the
// camera rig, smooths it to filter mouse jitter, and rolls the view slightly into the turn, easing
// back to level when turning stops. Gives the view a sense of head mass.
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(CameraEffectStack))]
    public sealed class LookSwayEffect : MonoBehaviour, ICameraEffect
    {
        [SerializeField] private FirstPersonCameraRig rig;
        [SerializeField] private LookSwaySettingsSO settings;

        [Tooltip("Global comfort multiplier. 0 disables look sway entirely.")]
        [SerializeField, Range(0f, 1f)] private float intensity = 1f;

        private CameraEffectStack stack;
        private bool isConfigured;

        private float lastYaw;
        private bool hasLastYaw;
        private float smoothedTurnRate;

        public float Intensity
        {
            get => intensity;
            set => intensity = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            stack = GetComponent<CameraEffectStack>();

            isConfigured = rig != null && settings != null;
            if (!isConfigured)
            {
                Debug.LogError("LookSwayEffect: Camera Rig or LookSwaySettingsSO is not assigned.", this);
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

            hasLastYaw = false;
            smoothedTurnRate = 0f;
        }

        public void Evaluate(float deltaTime, ref CameraOffset offset)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            float yaw = rig.Yaw;
            if (!hasLastYaw)
            {
                lastYaw = yaw;
                hasLastYaw = true;
            }

            // DeltaAngle handles the 360 to 0 wrap, so crossing north doesn't read as a huge spin.
            float turnRate = Mathf.DeltaAngle(lastYaw, yaw) / deltaTime;
            lastYaw = yaw;

            smoothedTurnRate = Mathf.Lerp(smoothedTurnRate, turnRate, 1f - Mathf.Exp(-settings.Sharpness * deltaTime));

            float sway = Mathf.Clamp(smoothedTurnRate / settings.FullSwayTurnRate, -1f, 1f);

            // Positive Z roll tilts left, so a right turn (positive rate) banks right with negative roll.
            offset.Rotation.z -= sway * settings.MaxRoll * intensity;
        }
    }
}