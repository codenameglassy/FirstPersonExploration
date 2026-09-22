// VoidEventChannelSO
// Responsibility: Event channel with no payload, for pure signals such as "finale started".
// Exists separately because Action<void> is not valid C#.
using System;
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "VoidEventChannel", menuName = "Game/Events/Void Event Channel")]
    public sealed class VoidEventChannelSO : ScriptableObject
    {
        private Action onEventRaised;

        public void Register(Action listener)
        {
            if (listener == null)
            {
                return;
            }

            onEventRaised -= listener;
            onEventRaised += listener;
        }

        public void Unregister(Action listener)
        {
            if (listener == null)
            {
                return;
            }

            onEventRaised -= listener;
        }

        public void Raise()
        {
            onEventRaised?.Invoke();
        }

        private void OnDisable()
        {
            onEventRaised = null;
        }
    }
}
