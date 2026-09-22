// TransformAnchorSO
// Responsibility: Shared runtime reference to a Transform (player, camera, bell) without scene
// lookups. A provider calls Provide in OnEnable and Unset in OnDisable. Readers must null check
// Value on every read. Listeners that Register while a value is already set receive it immediately,
// which removes execution order dependencies between provider and listener.
using System;
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "TransformAnchor", menuName = "Game/Anchors/Transform Anchor")]
    public sealed class TransformAnchorSO : ScriptableObject
    {
        private Transform value;
        private Action<Transform> onValueChanged;

        public Transform Value => value;
        public bool IsSet => value != null;

        public void Provide(Transform provided)
        {
            if (provided == null || value == provided)
            {
                return;
            }

            if (value != null)
            {
                Debug.LogWarning($"TransformAnchorSO '{name}': Value replaced by '{provided.name}'.", this);
            }

            value = provided;
            onValueChanged?.Invoke(value);
        }

        public void Unset(Transform provider)
        {
            if (value != provider)
            {
                return;
            }

            value = null;
            onValueChanged?.Invoke(null);
        }

        public void Register(Action<Transform> listener)
        {
            if (listener == null)
            {
                return;
            }

            onValueChanged -= listener;
            onValueChanged += listener;

            if (value != null)
            {
                listener(value);
            }
        }

        public void Unregister(Action<Transform> listener)
        {
            if (listener == null)
            {
                return;
            }

            onValueChanged -= listener;
        }

        private void OnDisable()
        {
            value = null;
            onValueChanged = null;
        }
    }
}
