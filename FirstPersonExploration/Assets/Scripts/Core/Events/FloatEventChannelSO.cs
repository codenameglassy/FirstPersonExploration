// FloatEventChannelSO
// Responsibility: Event channel carrying a float value such as a normalized progress.
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "FloatEventChannel", menuName = "Game/Events/Float Event Channel")]
    public sealed class FloatEventChannelSO : EventChannelSO<float>
    {
    }
}
