// EchoChannelRelay
// Responsibility: Forwards this object's EchoResponder answers onto event channels, so systems that
// live elsewhere (puzzles, game flow, journal, audio) can react without referencing the object.
using Game.Core;
using UnityEngine;

namespace Game.Echo
{
    [DisallowMultipleComponent]
    public sealed class EchoChannelRelay : MonoBehaviour
    {
        [SerializeField] private EchoResponder responder;
        [Tooltip("Raised on engage. The one to use for anything that changes game state.")]
        [SerializeField] private VoidEventChannelSO engagedChannel;
        [Tooltip("Raised on acknowledge. Feedback only.")]
        [SerializeField] private VoidEventChannelSO acknowledgedChannel;

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
            if (engagedChannel != null)
            {
                engagedChannel.Raise();
            }
        }

        private void HandleAcknowledged(EchoContext context)
        {
            if (acknowledgedChannel != null)
            {
                acknowledgedChannel.Raise();
            }
        }
    }
}