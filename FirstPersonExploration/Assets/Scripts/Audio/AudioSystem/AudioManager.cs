// Application-lifetime audio service. Pools sound emitters, drives music crossfades,
// owns mixer bus volumes, and exposes itself to callers through an AudioServiceAnchorSO.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour, IAudioService
{
    [Header("Service")]
    [SerializeField] private AudioServiceAnchorSO _serviceAnchor;
    [SerializeField] private bool _persistAcrossScenes = true;

    [Header("Mixer")]
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private MixerVolumeBinding[] _volumeBindings =
    {
        new MixerVolumeBinding(AudioBus.Master, "MasterVolume", 1f),
        new MixerVolumeBinding(AudioBus.Sfx, "SfxVolume", 1f),
        new MixerVolumeBinding(AudioBus.Voice, "VoiceVolume", 1f),
        new MixerVolumeBinding(AudioBus.Music, "MusicVolume", 0.8f)
    };

    [Header("Voices")]
    [SerializeField, Min(0)] private int _prewarmVoices = 8;
    [SerializeField, Min(1)] private int _maxVoices = 32;

    private readonly List<AudioEmitter> _active = new List<AudioEmitter>(32);
    private readonly Dictionary<AudioCueSO, int> _lastClipIndex = new Dictionary<AudioCueSO, int>(64);

    private ObjectPool<AudioEmitter> _pool;
    private MixerVolumeController _volumes;
    private MusicPlayer _music;
    private Transform _voiceRoot;
    private bool _isDuplicate;
    private bool _isProvided;
    private bool _isPaused;

    public bool IsPaused => _isPaused;

    private void Awake()
    {
        if (_serviceAnchor == null)
        {
            Debug.LogError("AudioManager: Service Anchor is not assigned. Callers cannot reach this service.", this);
        }
        else if (_serviceAnchor.IsSet)
        {
            _isDuplicate = true;
            enabled = false;
            Destroy(gameObject);
            return;
        }

        if (_persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        _volumes = new MixerVolumeController(_mixer, _volumeBindings);
        _music = new MusicPlayer(CreateMusicSource("Music A"), CreateMusicSource("Music B"));

        _voiceRoot = new GameObject("Voices").transform;
        _voiceRoot.SetParent(transform, false);

        _pool = new ObjectPool<AudioEmitter>(
            CreateEmitter,
            OnGetEmitter,
            OnReleaseEmitter,
            OnDestroyEmitter,
            true,
            Mathf.Min(_prewarmVoices, _maxVoices),
            _maxVoices);

        Prewarm();
    }

    private void OnEnable()
    {
        if (_isDuplicate || _serviceAnchor == null)
        {
            return;
        }

        _isProvided = _serviceAnchor.Provide(this);
    }

    private void Start()
    {
        _volumes.ApplyAll();
    }

    private void OnDisable()
    {
        if (_isProvided && _serviceAnchor != null)
        {
            _serviceAnchor.Unset(this);
        }

        _isProvided = false;
    }

    private void OnDestroy()
    {
        if (_isDuplicate)
        {
            return;
        }

        _pool?.Clear();
    }

    private void OnValidate()
    {
        if (_prewarmVoices > _maxVoices)
        {
            _prewarmVoices = _maxVoices;
        }
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        _music.Tick(deltaTime);

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].Tick(deltaTime, _isPaused))
            {
                ReleaseAt(i);
            }
        }
    }

    public AudioHandle Play(AudioCueSO cue, float fadeInSeconds = 0f)
    {
        return PlayInternal(cue, Vector3.zero, null, fadeInSeconds);
    }

    public AudioHandle PlayAt(AudioCueSO cue, Vector3 position, float fadeInSeconds = 0f)
    {
        return PlayInternal(cue, position, null, fadeInSeconds);
    }

    public AudioHandle PlayAttached(AudioCueSO cue, Transform followTarget, float fadeInSeconds = 0f)
    {
        if (followTarget == null)
        {
            return AudioHandle.Invalid;
        }

        return PlayInternal(cue, followTarget.position, followTarget, fadeInSeconds);
    }

    public void Stop(AudioHandle handle, float fadeOutSeconds = 0f)
    {
        if (!handle.IsValid)
        {
            return;
        }

        handle.Emitter.RequestStop(fadeOutSeconds);
    }

    public void StopAllSounds()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            ReleaseAt(i);
        }
    }

    public void PlayMusic(AudioCueSO musicCue, float crossfadeSeconds = 1f)
    {
        _music.Play(musicCue, crossfadeSeconds);
    }

    public void StopMusic(float fadeOutSeconds = 1f)
    {
        _music.Stop(fadeOutSeconds);
    }

    public void SetPaused(bool paused)
    {
        _isPaused = paused;
        AudioListener.pause = paused;
    }

    public float GetVolume(AudioBus bus)
    {
        return _volumes.GetVolume(bus);
    }

    public void SetVolume(AudioBus bus, float volume01)
    {
        _volumes.SetVolume(bus, volume01);
    }

    public void SaveVolumes()
    {
        _volumes.Save();
    }

    private AudioHandle PlayInternal(AudioCueSO cue, Vector3 position, Transform followTarget, float fadeInSeconds)
    {
        if (cue == null)
        {
            return AudioHandle.Invalid;
        }

        if (_active.Count >= _maxVoices)
        {
#if UNITY_EDITOR
            Debug.LogWarning("AudioManager: voice limit reached. Request dropped.", this);
#endif
            return AudioHandle.Invalid;
        }

        AudioClip clip = PickClip(cue);
        if (clip == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("AudioManager: cue '" + cue.name + "' has no valid clip.", cue);
#endif
            return AudioHandle.Invalid;
        }

        AudioEmitter emitter = _pool.Get();
        emitter.Play(cue, clip, position, followTarget, fadeInSeconds);
        _active.Add(emitter);
        return new AudioHandle(emitter, emitter.Generation);
    }

    private AudioClip PickClip(AudioCueSO cue)
    {
        if (!_lastClipIndex.TryGetValue(cue, out int previousIndex))
        {
            previousIndex = -1;
        }

        int index = cue.PickClipIndex(previousIndex);
        _lastClipIndex[cue] = index;
        return cue.GetClip(index);
    }

    private void ReleaseAt(int index)
    {
        AudioEmitter emitter = _active[index];
        int last = _active.Count - 1;
        _active[index] = _active[last];
        _active.RemoveAt(last);
        _pool.Release(emitter);
    }

    private void Prewarm()
    {
        int count = Mathf.Min(_prewarmVoices, _maxVoices);
        var warm = new AudioEmitter[count];

        for (int i = 0; i < count; i++)
        {
            warm[i] = _pool.Get();
        }

        for (int i = 0; i < count; i++)
        {
            _pool.Release(warm[i]);
        }
    }

    private AudioSource CreateMusicSource(string sourceName)
    {
        var sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private AudioEmitter CreateEmitter()
    {
        var emitterObject = new GameObject("AudioEmitter");
        emitterObject.SetActive(false);
        emitterObject.transform.SetParent(_voiceRoot, false);
        return emitterObject.AddComponent<AudioEmitter>();
    }

    private static void OnGetEmitter(AudioEmitter emitter)
    {
        emitter.gameObject.SetActive(true);
    }

    private static void OnReleaseEmitter(AudioEmitter emitter)
    {
        emitter.ResetState();
        emitter.gameObject.SetActive(false);
    }

    private static void OnDestroyEmitter(AudioEmitter emitter)
    {
        if (emitter != null)
        {
            Destroy(emitter.gameObject);
        }
    }
}
