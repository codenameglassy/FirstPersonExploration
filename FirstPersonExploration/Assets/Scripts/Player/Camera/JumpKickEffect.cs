// JumpKickEffect
// Responsibility: Jump takeoff kick as a camera effect layer. Listens to the character's Jumped
// event and kicks a damped spring downward, so the head briefly lags the body at push-off and
// catches up as it rises. The mirror of the landing dip.
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(CameraEffectStack))]
    public sealed class JumpKickEffect : MonoBehaviour, ICameraEffect
    {
        [SerializeField] private FirstPersonCharacter character;
        [SerializeField] private JumpKickSettingsSO settings;

        [Tooltip("Global comfort multiplier. 0 disables the kick entirely.")]
        [SerializeField, Range(0f, 1f)] private float intensity = 1f;

        private CameraEffectStack stack;
        private bool isConfigured;

        private DampedSpring spring;
        private bool kickPending;

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
                Debug.LogError("JumpKickEffect: Character or JumpKickSettingsSO is not assigned.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!isConfigured)
            {
                return;
            }

            character.Jumped += HandleJumped;
            if (stack != null)
            {
                stack.Register(this);
            }
        }

        private void OnDisable()
        {
            if (character != null)
            {
                character.Jumped -= HandleJumped;
            }

            if (stack != null)
            {
                stack.Unregister(this);
            }

            spring.Reset();
            kickPending = false;
        }

        public void Evaluate(float deltaTime, ref CameraOffset offset)
        {
            if (kickPending)
            {
                spring.Kick(-DampedSpring.SolveKickForPeak(settings.Drop, settings.Frequency, settings.DampingRatio));
                kickPending = false;
            }

            spring.Step(deltaTime, settings.Frequency, settings.DampingRatio);

            if (spring.IsAtRest || intensity <= 0f)
            {
                return;
            }

            offset.Position.y += spring.Position * intensity;
            offset.Rotation.x += (-spring.Position / settings.Drop) * settings.Nod * intensity;
        }

        // Called on the physics tick the jump is applied. Stored and applied on the next rendered frame.
        private void HandleJumped()
        {
            kickPending = true;
        }
    }
}