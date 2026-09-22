// DampedSpring
// Responsibility: Reusable one dimensional damped spring for camera effects. Integrates with fixed
// substeps so it stays stable through frame spikes, settles cleanly to rest, and can solve the kick
// velocity needed to reach an exact peak displacement.
using UnityEngine;

namespace Game.Player
{
    public struct DampedSpring
    {
        // Substep size keeps the spring stable through frame spikes, which are common on WebGL.
        private const float MaxStep = 1f / 120f;
        private const float SettlePosition = 0.00001f;
        private const float SettleVelocity = 0.0001f;

        public float Position;
        public float Velocity;

        public bool IsAtRest => Position == 0f && Velocity == 0f;

        public void Kick(float velocityDelta)
        {
            Velocity += velocityDelta;
        }

        public void Reset()
        {
            Position = 0f;
            Velocity = 0f;
        }

        public void Step(float deltaTime, float frequency, float dampingRatio)
        {
            if (deltaTime <= 0f || IsAtRest)
            {
                return;
            }

            float omega = 2f * Mathf.PI * frequency;
            float stiffness = omega * omega;
            float damping = 2f * dampingRatio * omega;

            int steps = Mathf.CeilToInt(deltaTime / MaxStep);
            float h = deltaTime / steps;

            for (int i = 0; i < steps; i++)
            {
                // Semi-implicit Euler: velocity first, then position. Stable at this step size.
                float acceleration = -stiffness * Position - damping * Velocity;
                Velocity += acceleration * h;
                Position += Velocity * h;
            }

            if (Mathf.Abs(Position) < SettlePosition && Mathf.Abs(Velocity) < SettleVelocity)
            {
                Reset();
            }
        }

        // Kick velocity that makes an underdamped spring at rest peak at exactly `peak`.
        // Peak displacement = (v0 / omega) * exp(-zeta * acos(zeta) / sqrt(1 - zeta^2)).
        // Damping ratio must be below 1.
        public static float SolveKickForPeak(float peak, float frequency, float dampingRatio)
        {
            float omega = 2f * Mathf.PI * frequency;
            float peakFactor = Mathf.Exp(-dampingRatio * Mathf.Acos(dampingRatio) / Mathf.Sqrt(1f - dampingRatio * dampingRatio));
            return peak * omega / peakFactor;
        }
    }
}