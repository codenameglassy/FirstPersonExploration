// Serializable mapping from an AudioBus to an exposed AudioMixer volume parameter and its default level.
using System;
using UnityEngine;

[Serializable]
public struct MixerVolumeBinding
{
    [SerializeField] private AudioBus _bus;
    [SerializeField] private string _exposedParameter;
    [SerializeField, Range(0f, 1f)] private float _defaultVolume;

    public AudioBus Bus => _bus;
    public string ExposedParameter => _exposedParameter;
    public float DefaultVolume => _defaultVolume;

    public MixerVolumeBinding(AudioBus bus, string exposedParameter, float defaultVolume)
    {
        _bus = bus;
        _exposedParameter = exposedParameter;
        _defaultVolume = defaultVolume;
    }
}
