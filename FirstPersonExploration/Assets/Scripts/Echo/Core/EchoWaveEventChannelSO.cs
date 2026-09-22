// EchoWaveEventChannelSO
// Responsibility: Event channel carrying an EchoWave from the emitter to every responder and any
// other system that reacts to echoes, such as rhythm listening, audio and game flow.
using Game.Core;
using UnityEngine;

namespace Game.Echo
{
    [CreateAssetMenu(fileName = "EchoWaveEventChannel", menuName = "Game/Events/Echo Wave Event Channel")]
    public sealed class EchoWaveEventChannelSO : EventChannelSO<EchoWave>
    {
    }
}
