// Vector3EventChannelSO
// Responsibility: Event channel carrying a world position such as where a pulse originated.
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "Vector3EventChannel", menuName = "Game/Events/Vector3 Event Channel")]
    public sealed class Vector3EventChannelSO : EventChannelSO<Vector3>
    {
    }
}
