// Plays one music track at a time on two alternating AudioSources and crossfades between tracks.
// Owned and ticked by AudioManager with unscaled time so fades keep running while the game is paused.
using UnityEngine;

public sealed class MusicPlayer
{
    private readonly AudioSource[] _sources = new AudioSource[2];
    private readonly float[] _fadeFrom = new float[2];
    private readonly float[] _fadeTo = new float[2];
    private readonly float[] _fadeDuration = new float[2];
    private readonly float[] _fadeTimer = new float[2];
    private readonly bool[] _isFading = new bool[2];
    private readonly bool[] _stopWhenFaded = new bool[2];

    private int _activeIndex;
    private AudioCueSO _currentCue;

    public AudioCueSO CurrentCue => _currentCue;

    public MusicPlayer(AudioSource first, AudioSource second)
    {
        _sources[0] = first;
        _sources[1] = second;
    }

    public void Play(AudioCueSO cue, float crossfadeSeconds)
    {
        if (cue == null)
        {
            return;
        }

        if (cue == _currentCue && _sources[_activeIndex].isPlaying)
        {
            return;
        }

        AudioClip clip = cue.GetClip(cue.PickClipIndex(-1));
        if (clip == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("MusicPlayer: music cue '" + cue.name + "' has no valid clip.", cue);
#endif
            return;
        }

        int previousIndex = _activeIndex;
        _activeIndex = 1 - _activeIndex;
        AudioSource next = _sources[_activeIndex];

        cue.ApplyTo(next, clip);
        next.spatialBlend = 0f;
        float targetVolume = next.volume;
        next.Play();

        BeginFade(_activeIndex, 0f, targetVolume, crossfadeSeconds, false);
        BeginFade(previousIndex, _sources[previousIndex].volume, 0f, crossfadeSeconds, true);
        _currentCue = cue;
    }

    public void Stop(float fadeOutSeconds)
    {
        if (_currentCue == null)
        {
            return;
        }

        BeginFade(_activeIndex, _sources[_activeIndex].volume, 0f, fadeOutSeconds, true);
        _currentCue = null;
    }

    public void Tick(float deltaTime)
    {
        for (int i = 0; i < 2; i++)
        {
            if (!_isFading[i])
            {
                continue;
            }

            _fadeTimer[i] += deltaTime;
            float t = Mathf.Clamp01(_fadeTimer[i] / _fadeDuration[i]);
            _sources[i].volume = Mathf.Lerp(_fadeFrom[i], _fadeTo[i], t);

            if (t >= 1f)
            {
                _isFading[i] = false;
                if (_stopWhenFaded[i])
                {
                    StopSource(i);
                }
            }
        }
    }

    private void BeginFade(int index, float from, float to, float duration, bool stopWhenFaded)
    {
        if (duration <= 0f)
        {
            _isFading[index] = false;
            _sources[index].volume = to;
            if (stopWhenFaded)
            {
                StopSource(index);
            }

            return;
        }

        _sources[index].volume = from;
        _fadeFrom[index] = from;
        _fadeTo[index] = to;
        _fadeDuration[index] = duration;
        _fadeTimer[index] = 0f;
        _stopWhenFaded[index] = stopWhenFaded;
        _isFading[index] = true;
    }

    private void StopSource(int index)
    {
        _sources[index].Stop();
        _sources[index].clip = null;
    }
}
