// EchoShellProfileSO
// Responsibility: Shared, read-only look of an echo shell over its lifetime: when it peaks and how
// the trailing inner shell differs from the outer one. Flyweight asset used by every shell that
// plays with it.
using UnityEngine;

namespace Game.Echo
{
    [CreateAssetMenu(fileName = "EchoShellProfile", menuName = "Game/Echo/Shell Profile")]
    public sealed class EchoShellProfileSO : ScriptableObject
    {
        [Tooltip("Fraction of the lifetime at which the shell reaches full brightness.")]
        [SerializeField, Range(0.01f, 0.5f)] private float alphaPeak = 0.12f;

        [Header("Inner shell")]
        [Tooltip("Seconds the inner shell trails the outer one. This lag is what reads as thickness.")]
        [SerializeField, Min(0f)] private float innerDelay = 0.06f;
        [SerializeField, Range(0f, 1f)] private float innerAlphaScale = 0.55f;
        [Tooltip("How far the inner shell's colour is pulled toward white.")]
        [SerializeField, Range(0f, 1f)] private float innerWhiten = 0.25f;

        public float AlphaPeak => alphaPeak;
        public float InnerDelay => innerDelay;
        public float InnerAlphaScale => innerAlphaScale;
        public float InnerWhiten => innerWhiten;
    }
}
