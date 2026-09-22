// EchoEmitterProfileSO
// Responsibility: Shared, read-only tuning for an echo emitter: charge and cooldown timing, wave size
// and speed across the charge range, colour, and which pooled shell to show. Flyweight asset; never
// written to at runtime.
using Game.Core;
using UnityEngine;

namespace Game.Echo
{
    [CreateAssetMenu(fileName = "EchoEmitterProfile", menuName = "Game/Echo/Emitter Profile")]
    public sealed class EchoEmitterProfileSO : ScriptableObject
    {
        [Header("Charge")]
        [Tooltip("Seconds of hold to reach full charge.")]
        [SerializeField, Min(0.01f)] private float maxChargeTime = 0.5f;
        [Tooltip("Seconds after firing before the next charge can begin. A button held through the cooldown starts charging the moment it ends.")]
        [SerializeField, Min(0f)] private float cooldown = 0.9f;

        [Header("Wave")]
        [SerializeField, Min(0.1f)] private float minRadius = 3f;
        [SerializeField, Min(0.1f)] private float maxRadius = 12f;
        [SerializeField, Min(0.05f)] private float minDuration = 0.45f;
        [SerializeField, Min(0.05f)] private float maxDuration = 0.7f;
        [Tooltip("Fraction of the full radius the front starts at, so the shell is visible on its first frame.")]
        [SerializeField, Range(0f, 0.5f)] private float startRadius01 = 0.15f;

        [Header("Visuals")]
        [ColorUsage(true, true)]
        [SerializeField] private Color echoColour = new Color(1.6f, 1.5f, 1.2f, 1f);
        [Tooltip("Brightness multiplier at zero charge. Full charge is always 1.")]
        [SerializeField, Range(0f, 1f)] private float minChargeBrightness = 0.65f;
        [SerializeField] private PooledInstance shellPrefab;
        [SerializeField] private EchoShellProfileSO shellProfile;
        [Tooltip("Shells created up front. Responders share this pool, so size it for a full wave in your busiest room.")]
        [SerializeField, Min(0)] private int shellPrewarmCount = 16;

        public float MaxChargeTime => maxChargeTime;
        public float Cooldown => cooldown;
        public float MinRadius => minRadius;
        public float MaxRadius => maxRadius;
        public float StartRadius01 => startRadius01;
        public PooledInstance ShellPrefab => shellPrefab;
        public EchoShellProfileSO ShellProfile => shellProfile;
        public int ShellPrewarmCount => shellPrewarmCount;

        public float RadiusAt(float charge01)
        {
            return Mathf.Lerp(minRadius, maxRadius, charge01);
        }

        public float DurationAt(float charge01)
        {
            return Mathf.Lerp(minDuration, maxDuration, charge01);
        }

        public Color ColourAt(float charge01)
        {
            Color colour = echoColour * Mathf.Lerp(minChargeBrightness, 1f, charge01);
            colour.a = 1f;
            return colour;
        }

        private void OnValidate()
        {
            maxRadius = Mathf.Max(maxRadius, minRadius);
            maxDuration = Mathf.Max(maxDuration, minDuration);
        }
    }
}
