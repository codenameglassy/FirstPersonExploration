// BoolEventChannelSO
// Responsibility: Event channel carrying a true or false state change.
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "BoolEventChannel", menuName = "Game/Events/Bool Event Channel")]
    public sealed class BoolEventChannelSO : EventChannelSO<bool>
    {
    }
}
