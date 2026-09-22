// HeadbobEffect
// Responsibility: Procedural figure eight headbob as a camera effect layer. Vertical runs at twice
// the sway frequency, so one cycle is two footfalls. Amplitude and cadence follow actual horizontal
// speed, crouch blends over the active gait in sync with the eye height, and a footstep is raised
// at the exact bottom of each vertical dip.
using System;
using Game.Core;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(CameraEffectStack))]
    public sealed class HeadbobEffect : MonoBehaviour, ICameraEffect
    {
        private const float TwoPi = Mathf.PI * 2f;

        [SerializeField] private FirstPersonCharacter character;
        [SerializeField] private HeadbobSettingsSO settings;

        [Tooltip("Optional. Raised once per footfall for listeners outside this prefab (audio, AI noise).")]
        [SerializeField] private VoidEventChannelSO footstepChannel;

        [Tooltip("Global comfort multiplier. 0 disables headbob entirely.")]
        [SerializeField, Range(0f, 1f)] private float intensity = 1f;

        private CameraEffectStack stack;
        private bool isConfigured;

        private float phase;
        private float amplitudeFactor;
        private float crouchWeight;
        private float previousDerivativeSign;

        // Raised once per footfall. For listeners on this prefab.
        public event Action Footstep;

        public float Intensity
        {
            get => intensity;
            set => intensity = Mathf.Clamp01(value);
        }

        // Current bob strength, 0 to 1. Useful for scaling other effects.
        public float AmplitudeFactor => amplitudeFactor;

        private void Awake()
        {
            stack = GetComponent<CameraEffectStack>();

            isConfigured = character != null && character.Profile != null && settings != null;
            if (!isConfigured)
            {
                Debug.LogError("HeadbobEffect: Character, its LocomotionProfileSO, or HeadbobSettingsSO is not assigned.", this);
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

            amplitudeFactor = 0f;
            previousDerivativeSign = 0f;
        }

        public void Evaluate(float deltaTime, ref CameraOffset offset)
        {
            // Crouch eases at the same rate as the eye height, so the bob change lands with the visual duck.
            float crouchTarget = character.IsCrouching ? 1f : 0f;
            crouchWeight = Mathf.Lerp(crouchWeight, crouchTarget, 1f - Mathf.Exp(-character.Profile.EyeHeightSharpness * deltaTime));

            HeadbobSettingsSO.BobProfile profile = character.IsJogging ? settings.Jog : settings.Walk;
            if (crouchWeight > 0.0001f)
            {
                profile = HeadbobSettingsSO.BobProfile.Lerp(profile, settings.Crouch, crouchWeight);
            }

            bool suppressed = settings.SuppressWhileAirborne && !character.IsGrounded;
            float horizontalSpeed = Vector3.ProjectOnPlane(character.Velocity, character.Motor.CharacterUp).magnitude;
            float speedFactor = suppressed ? 0f : Mathf.Clamp01(horizontalSpeed / settings.FullAmplitudeSpeed);

            amplitudeFactor = Mathf.Lerp(amplitudeFactor, speedFactor, 1f - Mathf.Exp(-settings.AmplitudeSmoothSharpness * deltaTime));

            // Cadence scales with speed: half speed gives half as many footfalls, not the same rhythm quieter.
            phase += profile.Frequency * speedFactor * TwoPi * deltaTime;
            if (phase > TwoPi)
            {
                phase -= TwoPi;
            }

            DetectFootstep(speedFactor);

            float scale = amplitudeFactor * intensity;
            if (scale <= 0f)
            {
                return;
            }

            float vertical = Mathf.Sin(phase * 2f);
            float horizontal = Mathf.Cos(phase);

            offset.Position.x += horizontal * profile.HorizontalAmplitude * scale;
            offset.Position.y += vertical * profile.VerticalAmplitude * scale;
            offset.Rotation.z += -horizontal * profile.RollAmount * scale;
        }

        // A footfall is the bottom of the vertical curve: the derivative flipping from falling to rising.
        // Exact regardless of frame rate or how fast the phase is advancing.
        private void DetectFootstep(float speedFactor)
        {
            float derivativeSign = Mathf.Sign(Mathf.Cos(phase * 2f));
            bool reachedBottom = previousDerivativeSign < 0f && derivativeSign >= 0f;

            if (reachedBottom && amplitudeFactor >= settings.FootstepMinAmplitude && speedFactor > 0f)
            {
                Footstep?.Invoke();
                if (footstepChannel != null)
                {
                    footstepChannel.Raise();
                }
            }

            previousDerivativeSign = derivativeSign;
        }
    }
}