// FovSettingsSO
// Responsibility: Shared, read-only field of view offsets per locomotion state, relative to the
// camera's base FOV, plus a fall offset that scales with downward speed and the transition speed.
// Flyweight asset; never written to at runtime.
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "FovSettings", menuName = "Game/Player/FOV Settings")]
    public sealed class FovSettingsSO : ScriptableObject
    {
        [Header("Offsets from base FOV, in degrees")]
        [Tooltip("Standing still.")]
        [SerializeField] private float idleOffset = 0f;

        [Tooltip("Moving, not jogging. A subtle widen reads as motion without calling attention to itself.")]
        [SerializeField] private float walkOffset = 4f;

        [Tooltip("Jogging. A bigger widen, the classic speed cue.")]
        [SerializeField] private float jogOffset = 12f;

        [Tooltip("Fully crouched. Narrower than idle, which reinforces caution.")]
        [SerializeField] private float crouchOffset = -4f;

        [Header("Falling")]
        [Tooltip("Added on top of the current state's offset at Fall Speed Full. Scales down linearly to zero at Fall Speed Start.")]
        [SerializeField] private float fallOffset = 10f;

        [Tooltip("Downward speed (m/s) where widening begins. Below this, falling adds nothing.")]
        [SerializeField, Min(0f)] private float fallSpeedStart = 4f;

        [Tooltip("Downward speed (m/s) where the full fall offset applies.")]
        [SerializeField, Min(0f)] private float fallSpeedFull = 15f;

        [Header("Transition")]
        [Tooltip("How quickly FOV eases toward its target. Higher = snappier.")]
        [SerializeField, Min(0.01f)] private float transitionSharpness = 8f;

        public float IdleOffset => idleOffset;
        public float WalkOffset => walkOffset;
        public float JogOffset => jogOffset;
        public float CrouchOffset => crouchOffset;
        public float FallOffset => fallOffset;
        public float FallSpeedStart => fallSpeedStart;
        public float FallSpeedFull => fallSpeedFull;
        public float TransitionSharpness => transitionSharpness;

        private void OnValidate()
        {
            fallSpeedFull = Mathf.Max(fallSpeedFull, fallSpeedStart + 0.1f);
        }
    }
}