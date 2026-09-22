// HeadbobSettingsSO
// Responsibility: Shared, read-only headbob tuning: one bob profile per gait plus response and
// footstep settings. Amplitudes in metres, frequency in full bob cycles per second (one cycle =
// two footfalls). Flyweight asset; never written to at runtime.
using System;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "HeadbobSettings", menuName = "Game/Player/Headbob Settings")]
    public sealed class HeadbobSettingsSO : ScriptableObject
    {
        [Serializable]
        public struct BobProfile
        {
            [Tooltip("Vertical travel, in metres. 0.03 to 0.08 reads as walking; more reads as seasickness.")]
            [SerializeField] private float verticalAmplitude;

            [Tooltip("Side to side sway, in metres. Usually about half the vertical.")]
            [SerializeField] private float horizontalAmplitude;

            [Tooltip("Full bob cycles per second. One cycle = two footfalls.")]
            [SerializeField] private float frequency;

            [Tooltip("Camera roll at sway extremes, in degrees. Subtle (0.3 to 1.5) unless you want a drunk or injured feel.")]
            [SerializeField] private float rollAmount;

            public BobProfile(float verticalAmplitude, float horizontalAmplitude, float frequency, float rollAmount)
            {
                this.verticalAmplitude = verticalAmplitude;
                this.horizontalAmplitude = horizontalAmplitude;
                this.frequency = frequency;
                this.rollAmount = rollAmount;
            }

            public float VerticalAmplitude => verticalAmplitude;
            public float HorizontalAmplitude => horizontalAmplitude;
            public float Frequency => frequency;
            public float RollAmount => rollAmount;

            public static BobProfile Lerp(BobProfile a, BobProfile b, float t)
            {
                return new BobProfile(
                    Mathf.Lerp(a.verticalAmplitude, b.verticalAmplitude, t),
                    Mathf.Lerp(a.horizontalAmplitude, b.horizontalAmplitude, t),
                    Mathf.Lerp(a.frequency, b.frequency, t),
                    Mathf.Lerp(a.rollAmount, b.rollAmount, t));
            }
        }

        [Header("Profiles")]
        [SerializeField] private BobProfile walk = new BobProfile(0.045f, 0.025f, 0.9f, 0.5f);
        [SerializeField] private BobProfile jog = new BobProfile(0.075f, 0.035f, 1.35f, 1.1f);
        [SerializeField] private BobProfile crouch = new BobProfile(0.022f, 0.018f, 0.6f, 0.3f);

        [Header("Response")]
        [Tooltip("Horizontal speed (m/s) at which bob reaches full amplitude and cadence. Below this it fades in proportionally.")]
        [SerializeField, Min(0.01f)] private float fullAmplitudeSpeed = 1.5f;

        [Tooltip("How quickly amplitude eases in and out. Higher = more immediate; lower = a softer settle when you stop.")]
        [SerializeField, Min(0.01f)] private float amplitudeSmoothSharpness = 9f;

        [Tooltip("Bob keeps its phase but is silenced while airborne. No bobbing mid-jump.")]
        [SerializeField] private bool suppressWhileAirborne = true;

        [Header("Footsteps")]
        [Tooltip("Minimum amplitude factor (0 to 1) before footsteps fire. Stops near-stationary drift from triggering steps.")]
        [SerializeField, Range(0f, 1f)] private float footstepMinAmplitude = 0.25f;

        public BobProfile Walk => walk;
        public BobProfile Jog => jog;
        public BobProfile Crouch => crouch;
        public float FullAmplitudeSpeed => fullAmplitudeSpeed;
        public float AmplitudeSmoothSharpness => amplitudeSmoothSharpness;
        public bool SuppressWhileAirborne => suppressWhileAirborne;
        public float FootstepMinAmplitude => footstepMinAmplitude;
    }
}