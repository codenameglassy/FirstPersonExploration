// TransformAnchorProvider
// Responsibility: Publishes this GameObject's Transform into a TransformAnchorSO while enabled.
using UnityEngine;

namespace Game.Core
{
    public sealed class TransformAnchorProvider : MonoBehaviour
    {
        [SerializeField] private TransformAnchorSO anchor;

        private void OnEnable()
        {
            if (anchor != null)
            {
                anchor.Provide(transform);
            }
        }

        private void OnDisable()
        {
            if (anchor != null)
            {
                anchor.Unset(transform);
            }
        }
    }
}
