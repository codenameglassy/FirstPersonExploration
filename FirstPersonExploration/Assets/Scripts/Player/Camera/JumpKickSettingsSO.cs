// JumpKickSettingsSO
// Responsibility: Shared, read-only tuning for the jump takeoff kick: how far the head lags down at
// takeoff, the nod that goes with it, and the spring that recovers. Flyweight asset; never written
// to at runtime.
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(fileName = "JumpKickSettings", menuName = "Game/Player/Jump Kick Settings")]
    public sealed class JumpKickSettingsSO : ScriptableObject
    {
        [Tooltip("How far the head lags down at takeoff, in metres.")]
        [SerializeField, Min(0.001f)] private float drop = 0.04f;

        [Tooltip("Downward nod at the deepest point of the kick, in degrees.")]
        [SerializeField, Min(0f)] private float nod = 1.5f;

        [Tooltip("Oscillation speed in cycles per second. Higher = quicker push.")]
        [SerializeField, Min(0.1f)] private float frequency = 3.5f;

        [Tooltip("1 = no overshoot. Keep it fairly high so the takeoff reads as one push, not a bounce.")]
        [SerializeField, Range(0.1f, 0.95f)] private float dampingRatio = 0.6f;

        public float Drop => drop;
        public float Nod => nod;
        public float Frequency => frequency;
        public float DampingRatio => dampingRatio;
    }
}