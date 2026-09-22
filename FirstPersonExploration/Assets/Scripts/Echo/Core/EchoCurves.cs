// EchoCurves
// Responsibility: The single source of truth for how an echo wavefront grows and fades. Shell
// visuals and every responder's arrival timing both go through these functions, so what the player
// sees and when things answer can never drift apart.
using UnityEngine;

namespace Game.Echo
{
    public static class EchoCurves
    {
        private const float InverseQuintic = 0.2f;

        // Ease out quint: about 60 percent of the distance in the first 15 percent of the lifetime.
        public static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u * u * u;
        }

        // Exact inverse of Ease: the normalized time at which Ease reaches the given value.
        public static float InverseEase(float value)
        {
            value = Mathf.Clamp01(value);
            return 1f - Mathf.Pow(1f - value, InverseQuintic);
        }

        // Normalized front radius at normalized time t. The front starts at startRadius01 rather than
        // zero so the shell is visible on its first frame.
        public static float FrontRadius01(float t, float startRadius01)
        {
            return Mathf.Lerp(startRadius01, 1f, Ease(t));
        }

        // Normalized time at which the front reaches normalized distance01. Everything inside the
        // starting radius is reached on the first frame.
        public static float ArrivalTime01(float distance01, float startRadius01)
        {
            if (distance01 <= startRadius01)
            {
                return 0f;
            }

            if (distance01 >= 1f)
            {
                return 1f;
            }

            float eased = (distance01 - startRadius01) / Mathf.Max(1f - startRadius01, 0.0001f);
            return InverseEase(eased);
        }

        // Normalized alpha over normalized lifetime. Rises hard to a peak, then decays. Never starts
        // fully transparent, so the first frame already reads.
        public static float Alpha01(float t, float peak)
        {
            t = Mathf.Clamp01(t);
            peak = Mathf.Clamp(peak, 0.0001f, 0.9999f);

            if (t < peak)
            {
                return Mathf.Lerp(0.35f, 1f, t / peak);
            }

            float k = (t - peak) / (1f - peak);
            return Mathf.Pow(1f - k, 1.6f);
        }
    }
}
