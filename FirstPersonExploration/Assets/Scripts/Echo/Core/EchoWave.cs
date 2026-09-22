// EchoWave
// Responsibility: Immutable description of one emitted echo: where, how strong, how far, how long
// and when. Raised once on the echo wave channel; every listener derives its own timing from it.
using UnityEngine;

namespace Game.Echo
{
    public readonly struct EchoWave
    {
        public readonly Vector3 Origin;
        public readonly float Charge;
        public readonly float Radius;
        public readonly float Duration;
        public readonly float StartRadius01;
        public readonly float EmitTime;

        public EchoWave(Vector3 origin, float charge, float radius, float duration, float startRadius01, float emitTime)
        {
            Origin = origin;
            Charge = Mathf.Clamp01(charge);
            Radius = Mathf.Max(0.01f, radius);
            Duration = Mathf.Max(0.05f, duration);
            StartRadius01 = Mathf.Clamp(startRadius01, 0f, 0.95f);
            EmitTime = emitTime;
        }

        // Front radius in metres, elapsed seconds after emission.
        public float FrontRadiusAt(float elapsed)
        {
            return Radius * EchoCurves.FrontRadius01(elapsed / Duration, StartRadius01);
        }

        // Time.time at which the front reaches a point at the given distance from Origin.
        // Returns false when the point lies outside the wave.
        public bool TryGetArrivalTime(float distance, out float arrivalTime)
        {
            if (distance > Radius)
            {
                arrivalTime = 0f;
                return false;
            }

            arrivalTime = EmitTime + Duration * EchoCurves.ArrivalTime01(distance / Radius, StartRadius01);
            return true;
        }
    }
}
