// IntEventChannelSO
// Responsibility: Event channel carrying an integer value such as a count or index.
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "IntEventChannel", menuName = "Game/Events/Int Event Channel")]
    public sealed class IntEventChannelSO : EventChannelSO<int>
    {
    }
}
