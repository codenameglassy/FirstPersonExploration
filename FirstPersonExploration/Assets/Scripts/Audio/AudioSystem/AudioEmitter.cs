// Pooled sound voice. Plays one AudioCueSO, optionally follows a transform, fades in and out on request,
// and reports through Tick when it has finished so AudioManager can return it to the pool.
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public sealed class AudioEmitter : MonoBehaviour
{
    private AudioSource _source;
    private Transform _transform;
    private Transform _followTarget;
    private bool _isFollowing;
    private bool _stopRequested;
    private bool _isFading;
    private bool _stopWhenFaded;
    private float _fadeFrom;
    private float _fadeTo;
    private float _fadeDuration;
    private float _fadeTimer;

    public int Generation { get; private set; }
    public bool IsActive { get; private set; }

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _transform = transform;
        _source.playOnAwake = false;
    }

    public void Play(AudioCueSO cue, AudioClip clip, Vector3 position, Transform followTarget, float fadeInSeconds)
    {
        Generation++;
        IsActive = true;
        _stopRequested = false;
        _isFading = false;
        _stopWhenFaded = false;
        _followTarget = followTarget;
        _isFollowing = followTarget != null;
        _transform.position = _isFollowing ? followTarget.position : position;

        cue.ApplyTo(_source, clip);

        if (fadeInSeconds > 0f)
        {
            BeginFade(0f, _source.volume, fadeInSeconds, false);
        }

        _source.Play();
    }

    public void RequestStop(float fadeOutSeconds)
    {
        if (!IsActive)
        {
            return;
        }

        if (fadeOutSeconds <= 0f)
        {
            _stopRequested = true;
            return;
        }

        if (_isFading && _stopWhenFaded)
        {
            return;
        }

        BeginFade(_source.volume, 0f, fadeOutSeconds, true);
    }

    // Returns true when this emitter is finished and should be released.
    public bool Tick(float deltaTime, bool listenerPaused)
    {
        if (_stopRequested)
        {
            return true;
        }

        if (listenerPaused && !_source.ignoreListenerPause)
        {
            return false;
        }

        if (_isFollowing)
        {
            if (_followTarget == null)
            {
                _isFollowing = false;
                if (_source.loop)
                {
                    return true;
                }
            }
            else
            {
                _transform.position = _followTarget.position;
            }
        }

        if (_isFading)
        {
            _fadeTimer += deltaTime;
            float t = _fadeTimer >= _fadeDuration ? 1f : _fadeTimer / _fadeDuration;
            _source.volume = Mathf.Lerp(_fadeFrom, _fadeTo, t);

            if (t >= 1f)
            {
                _isFading = false;
                if (_stopWhenFaded)
                {
                    return true;
                }
            }
        }

        return !_source.isPlaying;
    }

    public void ResetState()
    {
        _source.Stop();
        _source.clip = null;
        _followTarget = null;
        _isFollowing = false;
        _stopRequested = false;
        _isFading = false;
        _stopWhenFaded = false;
        _fadeTimer = 0f;
        IsActive = false;
        Generation++;
    }

    private void BeginFade(float from, float to, float duration, bool stopWhenFaded)
    {
        _source.volume = from;
        _fadeFrom = from;
        _fadeTo = to;
        _fadeDuration = duration;
        _fadeTimer = 0f;
        _stopWhenFaded = stopWhenFaded;
        _isFading = true;
    }
}
