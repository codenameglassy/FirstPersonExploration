// VoidChannelDebugLog
// Responsibility: Development tool. Logs a message to the Console whenever a VoidEventChannelSO is
// raised, to confirm a channel is wired end to end. The log is compiled out of release builds, so a
// forgotten instance costs nothing in the shipped game.
using Game.Core;
using UnityEngine;

namespace Game.Debugging
{
    [DisallowMultipleComponent]
    public sealed class VoidChannelDebugLog : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO channel;
        [SerializeField] private string message = "Test resonator reacted";

        private void OnEnable()
        {
            if (channel != null)
            {
                channel.Register(HandleRaised);
            }
        }

        private void OnDisable()
        {
            if (channel != null)
            {
                channel.Unregister(HandleRaised);
            }
        }

        private void HandleRaised()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(message, this);
#endif
        }
    }
}