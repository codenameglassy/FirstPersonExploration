// EchoResponderProbe
// Responsibility: Temporary diagnostic. Logs every stage an EchoResponder reaches (arrived, engaged,
// acknowledged) with distance and charge, to find where an echo interaction stops. Remove it once
// the problem is found. Logs are compiled out of release builds.
using Game.Echo;
using UnityEngine;

namespace Game.Debugging
{
    [DisallowMultipleComponent]
    public sealed class EchoResponderProbe : MonoBehaviour
    {
        [Tooltip("Empty uses the EchoResponder on this GameObject.")]
        [SerializeField] private EchoResponder responder;

        private void Awake()
        {
            if (responder == null)
            {
                TryGetComponent(out responder);
            }

            if (responder == null)
            {
                Debug.LogError("EchoResponderProbe: No EchoResponder found.", this);
            }
        }

        private void OnEnable()
        {
            if (responder != null)
            {
                responder.Arrived += HandleArrived;
                responder.Engaged += HandleEngaged;
                responder.Acknowledged += HandleAcknowledged;
            }
        }

        private void OnDisable()
        {
            if (responder != null)
            {
                responder.Arrived -= HandleArrived;
                responder.Engaged -= HandleEngaged;
                responder.Acknowledged -= HandleAcknowledged;
            }
        }

        private void HandleArrived(EchoContext context)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Probe] {name}: wave arrived. Distance {context.Distance:0.00} m, charge {context.Wave.Charge:0.00}.", this);
#endif
        }

        private void HandleEngaged(EchoContext context)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Probe] {name}: ENGAGED.", this);
#endif
        }

        private void HandleAcknowledged(EchoContext context)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Probe] {name}: acknowledged only, not engaged.", this);
#endif
        }
    }
}