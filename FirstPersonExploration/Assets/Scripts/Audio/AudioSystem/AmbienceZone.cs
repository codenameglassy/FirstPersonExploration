// Fades a looping ambience cue in while the player is inside this zone and out when they leave.
// Plays at the zone's position, so a 2D cue covers the whole area and a 3D cue sits at the zone center.
using UnityEngine;

[RequireComponent(typeof(PlayerTriggerZone))]
[DisallowMultipleComponent]
public sealed class AmbienceZone : MonoBehaviour
{
    [SerializeField] private AudioServiceAnchorSO _audioService;
    [SerializeField] private AudioCueSO _ambience;
    [SerializeField, Min(0f)] private float _fadeInSeconds = 2f;
    [SerializeField, Min(0f)] private float _fadeOutSeconds = 2f;

    private PlayerTriggerZone _zone;
    private Transform _transform;
    private AudioHandle _handle;

    private IAudioService Service => _audioService != null ? _audioService.Value : null;

    private void Awake()
    {
        _zone = GetComponent<PlayerTriggerZone>();
        _transform = transform;
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
        StopAmbience();
    }

    private void HandleEntered()
    {
        if (_ambience == null || _handle.IsValid)
        {
            return;
        }

        IAudioService service = Service;
        if (service == null)
        {
            return;
        }

        _handle = service.PlayAt(_ambience, _transform.position, _fadeInSeconds);
    }

    private void HandleExited()
    {
        StopAmbience();
    }

    private void StopAmbience()
    {
        if (!_handle.IsValid)
        {
            return;
        }

        IAudioService service = Service;
        if (service != null)
        {
            service.Stop(_handle, _fadeOutSeconds);
        }

        _handle = AudioHandle.Invalid;
    }
}
