// Switches music when the player enters this zone, and optionally restores or stops it on exit.
// Listens to the sibling PlayerTriggerZone through plain C# events.
using UnityEngine;

[RequireComponent(typeof(PlayerTriggerZone))]
[DisallowMultipleComponent]
public sealed class MusicZone : MonoBehaviour
{
    [SerializeField] private AudioServiceAnchorSO _audioService;
    [SerializeField] private AudioCueSO _enterMusic;
    [SerializeField] private AudioCueSO _exitMusic;
    [SerializeField] private bool _stopMusicOnExit;
    [SerializeField, Min(0f)] private float _crossfadeSeconds = 2f;

    private PlayerTriggerZone _zone;

    private IAudioService Service => _audioService != null ? _audioService.Value : null;

    private void Awake()
    {
        _zone = GetComponent<PlayerTriggerZone>();
    }

    private void OnEnable()
    {
        _zone.Entered += HandleEntered;
        _zone.Exited += HandleExited;
    }

    private void OnDisable()
    {
        _zone.Entered -= HandleEntered;
        _zone.Exited -= HandleExited;
    }

    private void HandleEntered()
    {
        IAudioService service = Service;
        if (service == null || _enterMusic == null)
        {
            return;
        }

        service.PlayMusic(_enterMusic, _crossfadeSeconds);
    }

    private void HandleExited()
    {
        IAudioService service = Service;
        if (service == null)
        {
            return;
        }

        if (_exitMusic != null)
        {
            service.PlayMusic(_exitMusic, _crossfadeSeconds);
        }
        else if (_stopMusicOnExit)
        {
            service.StopMusic(_crossfadeSeconds);
        }
    }
}
