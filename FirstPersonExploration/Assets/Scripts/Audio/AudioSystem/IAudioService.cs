// Contract for playing sounds and music and controlling bus volumes. Resolved through AudioServiceAnchorSO.
using UnityEngine;

public interface IAudioService
{
    bool IsPaused { get; }

    AudioHandle Play(AudioCueSO cue, float fadeInSeconds = 0f);
    AudioHandle PlayAt(AudioCueSO cue, Vector3 position, float fadeInSeconds = 0f);
    AudioHandle PlayAttached(AudioCueSO cue, Transform followTarget, float fadeInSeconds = 0f);
    void Stop(AudioHandle handle, float fadeOutSeconds = 0f);
    void StopAllSounds();

    void PlayMusic(AudioCueSO musicCue, float crossfadeSeconds = 1f);
    void StopMusic(float fadeOutSeconds = 1f);

    void SetPaused(bool paused);

    float GetVolume(AudioBus bus);
    void SetVolume(AudioBus bus, float volume01);
    void SaveVolumes();
}
