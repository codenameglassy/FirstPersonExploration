// Plays an AudioCueSO from this GameObject. Suited to ambient loops and simple scene sounds.
// Plays automatically on Start and re-enable if configured, and stops with a fade on disable.
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AudioCuePlayer : MonoBehaviour
{
    [SerializeField] private AudioServiceAnchorSO _audioService;
    [SerializeField] private AudioCueSO _cue;
    [SerializeField] private bool _playAutomatically = true;
    [SerializeField] private bool _followTransform = true;
    [SerializeField, Min(0f)] private float _fadeInSeconds;
    [SerializeField, Min(0f)] private float _fadeOutOnDisable = 0.5f;

    private Transform _transform;
    private AudioHandle _handle;
    private bool _hasStarted;

    private void Awake()
    {
        _transform = transform;
    }

    private void Start()
    {
        _hasStarted = true;
        if (_playAutomatically)
        {
            Play();
        }
    }

    private void OnEnable()
    {
        if (_hasStarted && _playAutomatically)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        Stop(_fadeOutOnDisable);
    }

    public void Play()
    {
        if (_cue == null)
        {
            return;
        }

        if (_cue.Loop && _handle.IsValid)
        {
            return;
        }

        IAudioService service = _audioService != null ? _audioService.Value : null;
        if (service == null)
        {
            return;
        }

        _handle = _followTransform
            ? service.PlayAttached(_cue, _transform, _fadeInSeconds)
            : service.PlayAt(_cue, _transform.position, _fadeInSeconds);
    }

    public void Stop(float fadeOutSeconds)
    {
        if (!_handle.IsValid)
        {
            return;
        }

        IAudioService service = _audioService != null ? _audioService.Value : null;
        if (service != null)
        {
            service.Stop(_handle, fadeOutSeconds);
        }

        _handle = AudioHandle.Invalid;
    }
}
