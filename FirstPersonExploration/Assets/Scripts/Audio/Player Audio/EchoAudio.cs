// EchoAudio
// Responsibility: Plays the player's echo emission sound whenever an echo wave is raised. Listens to
// the echo wave channel, so the emitter never knows audio exists. Picks a heavier cue for a full
// charge when one is assigned.
using UnityEngine;

namespace Game.Echo
{
    [DisallowMultipleComponent]
    public sealed class EchoAudio : MonoBehaviour
    {
        [Header("Channels")]
        [SerializeField] private EchoWaveEventChannelSO waveChannel;

        [Header("Audio")]
        [SerializeField] private AudioServiceAnchorSO audioService;
        [Tooltip("Played for every echo below the full charge threshold.")]
        [SerializeField] private AudioCueSO emitCue;
        [Tooltip("Optional. Played instead of the emit cue when the echo's charge reaches the threshold.")]
        [SerializeField] private AudioCueSO fullChargeCue;
        [SerializeField, Range(0f, 1f)] private float fullChargeThreshold = 0.95f;

        private void OnEnable()
        {
            if (waveChannel != null)
            {
                waveChannel.Register(HandleWave);
            }
        }

        private void OnDisable()
        {
            if (waveChannel != null)
            {
                waveChannel.Unregister(HandleWave);
            }
        }

        private void HandleWave(EchoWave wave)
        {
            AudioCueSO cue = fullChargeCue != null && wave.Charge >= fullChargeThreshold
                ? fullChargeCue
                : emitCue;

            if (cue == null)
            {
                return;
            }

            IAudioService service = audioService != null ? audioService.Value : null;
            if (service == null)
            {
                return;
            }

            service.PlayAt(cue, wave.Origin);
        }
    }
}
