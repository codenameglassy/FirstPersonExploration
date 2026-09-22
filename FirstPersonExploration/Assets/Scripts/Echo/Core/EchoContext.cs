// EchoContext
// Responsibility: What a responder knows about the echo that just reached it: the wave itself and
// how far it travelled to get here, plus convenience values for scaling reactions.
using UnityEngine;

namespace Game.Echo
{
    public readonly struct EchoContext
    {
        public readonly EchoWave Wave;
        public readonly float Distance;

        public EchoContext(EchoWave wave, float distance)
        {
            Wave = wave;
            Distance = Mathf.Max(0f, distance);
        }

        // 0 at the echo origin, 1 at the edge of the wave.
        public float NormalisedDistance => Wave.Radius > 0f ? Mathf.Clamp01(Distance / Wave.Radius) : 0f;

        // Wave strength where it arrived. A full charge falls to 40 percent at the edge.
        public float Strength => Wave.Charge * (1f - NormalisedDistance * 0.6f);
    }
}
