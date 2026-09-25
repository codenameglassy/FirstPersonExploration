// Shared, immutable definition of a playable sound: clips, output mixer group, and playback settings.
// Holds no runtime state; per-cue state such as the last played clip lives in AudioManager.
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Audio Cue", fileName = "AudioCue")]
public sealed class AudioCueSO : ScriptableObject
{
    [Header("Clips")]
    [SerializeField] private AudioClip[] _clips = new AudioClip[0];

    [Header("Routing")]
    [SerializeField] private AudioMixerGroup _output;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;
    [SerializeField, Range(0f, 1f)] private float _volumeVariation;
    [SerializeField, Range(0.1f, 3f)] private float _pitch = 1f;
    [SerializeField, Range(0f, 0.5f)] private float _pitchVariation;
    [SerializeField] private bool _loop;
    [SerializeField] private bool _ignoreListenerPause;

    [Header("Spatial")]
    [SerializeField, Range(0f, 1f)] private float _spatialBlend = 1f;
    [SerializeField, Min(0f)] private float _minDistance = 1f;
    [SerializeField, Min(0f)] private float _maxDistance = 30f;
    [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;
    [SerializeField, Range(0f, 5f)] private float _dopplerLevel;

    public bool Loop => _loop;
    public AudioMixerGroup Output => _output;
    public int ClipCount => _clips != null ? _clips.Length : 0;

    public AudioClip GetClip(int index)
    {
        if (_clips == null || index < 0 || index >= _clips.Length)
        {
            return null;
        }

        return _clips[index];
    }

    // Picks a random clip index that differs from previousIndex when more than one clip exists.
    public int PickClipIndex(int previousIndex)
    {
        int count = ClipCount;
        if (count == 0)
        {
            return -1;
        }

        if (count == 1)
        {
            return 0;
        }

        if (previousIndex < 0 || previousIndex >= count)
        {
            return Random.Range(0, count);
        }

        int index = Random.Range(0, count - 1);
        if (index >= previousIndex)
        {
            index++;
        }

        return index;
    }

    public void ApplyTo(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.outputAudioMixerGroup = _output;
        source.volume = _volume * Random.Range(1f - _volumeVariation, 1f);
        source.pitch = Mathf.Max(0.1f, _pitch + Random.Range(-_pitchVariation, _pitchVariation));
        source.loop = _loop;
        source.ignoreListenerPause = _ignoreListenerPause;
        source.spatialBlend = _spatialBlend;
        source.minDistance = _minDistance;
        source.maxDistance = _maxDistance;
        source.rolloffMode = _rolloffMode;
        source.dopplerLevel = _dopplerLevel;
    }

    private void OnValidate()
    {
        if (_maxDistance < _minDistance)
        {
            _maxDistance = _minDistance;
        }
    }
}
