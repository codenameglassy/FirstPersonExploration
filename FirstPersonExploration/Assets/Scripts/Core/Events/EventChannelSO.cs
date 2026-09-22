// EventChannelSO<T>
// Responsibility: Generic base for ScriptableObject event channels carrying one payload of type T.
// Listeners Register in OnEnable and Unregister in OnDisable. The delegate is cleared in this
// asset's OnDisable because ScriptableObjects outlive Play mode.
using System;
using UnityEngine;

namespace Game.Core
{
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        private Action<T> onEventRaised;

        public void Register(Action<T> listener)
        {
            if (listener == null)
            {
                return;
            }

            onEventRaised -= listener;
            onEventRaised += listener;
        }

        public void Unregister(Action<T> listener)
        {
            if (listener == null)
            {
                return;
            }

            onEventRaised -= listener;
        }

        public void Raise(T value)
        {
            onEventRaised?.Invoke(value);
        }

        protected virtual void OnDisable()
        {
            onEventRaised = null;
        }
    }
}
