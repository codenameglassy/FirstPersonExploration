// EchoAnimatorReaction
// Responsibility: Sets Animator triggers when this object's EchoResponder engages or acknowledges.
// The simplest reaction for doors, prayer wheels and flag lines: the animation owns the motion and
// this only starts it. Lives on the same prefab as the responder, so it listens through C# events.
// Trigger names are serialized, so their hashes are cached once in Awake rather than as statics.
using UnityEngine;

namespace Game.Echo
{
    [DisallowMultipleComponent]
    public sealed class EchoAnimatorReaction : MonoBehaviour
    {
        [SerializeField] private EchoResponder responder;
        [SerializeField] private Animator animator;
        [Tooltip("Trigger set on engage. Leave empty for none.")]
        [SerializeField] private string engagedTrigger = "Engage";
        [Tooltip("Trigger set on acknowledge. Leave empty for none.")]
        [SerializeField] private string acknowledgedTrigger = "";

        private int engagedHash;
        private int acknowledgedHash;
        private bool hasEngagedTrigger;
        private bool hasAcknowledgedTrigger;

        private void Awake()
        {
            hasEngagedTrigger = !string.IsNullOrEmpty(engagedTrigger);
            if (hasEngagedTrigger)
            {
                engagedHash = Animator.StringToHash(engagedTrigger);
            }

            hasAcknowledgedTrigger = !string.IsNullOrEmpty(acknowledgedTrigger);
            if (hasAcknowledgedTrigger)
            {
                acknowledgedHash = Animator.StringToHash(acknowledgedTrigger);
            }
        }

        private void OnEnable()
        {
            if (responder != null)
            {
                responder.Engaged += HandleEngaged;
                responder.Acknowledged += HandleAcknowledged;
            }
        }

        private void OnDisable()
        {
            if (responder != null)
            {
                responder.Engaged -= HandleEngaged;
                responder.Acknowledged -= HandleAcknowledged;
            }
        }

        private void HandleEngaged(EchoContext context)
        {
            if (hasEngagedTrigger && animator != null)
            {
                animator.SetTrigger(engagedHash);
            }
        }

        private void HandleAcknowledged(EchoContext context)
        {
            if (hasAcknowledgedTrigger && animator != null)
            {
                animator.SetTrigger(acknowledgedHash);
            }
        }
    }
}
