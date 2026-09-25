// Converts linear 0-1 bus volumes to decibels on an AudioMixer and persists them in PlayerPrefs.
using System;
using UnityEngine;
using UnityEngine.Audio;

public sealed class MixerVolumeController
{
    private const float MinLinear = 0.0001f;
    private const float MinDecibels = -80f;
    private const string PrefsPrefix = "audio.volume.";

    private readonly AudioMixer _mixer;
    private readonly string[] _parameters;
    private readonly string[] _prefsKeys;
    private readonly float[] _volumes;

    public MixerVolumeController(AudioMixer mixer, MixerVolumeBinding[] bindings)
    {
        _mixer = mixer;

        int busCount = Enum.GetValues(typeof(AudioBus)).Length;
        _parameters = new string[busCount];
        _prefsKeys = new string[busCount];
        _volumes = new float[busCount];

        for (int i = 0; i < busCount; i++)
        {
            _volumes[i] = 1f;
        }

        if (bindings != null)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                MixerVolumeBinding binding = bindings[i];
                int index = (int)binding.Bus;
                if (index < 0 || index >= busCount)
                {
                    continue;
                }

                _parameters[index] = binding.ExposedParameter;
                _prefsKeys[index] = PrefsPrefix + binding.Bus.ToString();
                _volumes[index] = Mathf.Clamp01(PlayerPrefs.GetFloat(_prefsKeys[index], binding.DefaultVolume));
            }
        }

        if (_mixer == null)
        {
            Debug.LogWarning("MixerVolumeController: no AudioMixer assigned. Bus volumes will not be applied.");
            return;
        }

        for (int i = 0; i < busCount; i++)
        {
            if (string.IsNullOrEmpty(_parameters[i]))
            {
                Debug.LogWarning("MixerVolumeController: no exposed parameter bound for bus " + ((AudioBus)i).ToString() + ".");
            }
        }
    }

    // Call from Start or later. SetFloat is commonly reported to have no effect when called during Awake.
    public void ApplyAll()
    {
        for (int i = 0; i < _parameters.Length; i++)
        {
            if (string.IsNullOrEmpty(_parameters[i]))
            {
                continue;
            }

            if (!TryApply(i))
            {
                Debug.LogWarning("MixerVolumeController: exposed parameter '" + _parameters[i] + "' was not found on the mixer. Expose it or fix the binding.");
                _parameters[i] = null;
            }
        }
    }

    public float GetVolume(AudioBus bus)
    {
        int index = (int)bus;
        return IsValidIndex(index) ? _volumes[index] : 0f;
    }

    public void SetVolume(AudioBus bus, float volume01)
    {
        int index = (int)bus;
        if (!IsValidIndex(index))
        {
            return;
        }

        _volumes[index] = Mathf.Clamp01(volume01);
        TryApply(index);

        if (_prefsKeys[index] != null)
        {
            PlayerPrefs.SetFloat(_prefsKeys[index], _volumes[index]);
        }
    }

    public void Save()
    {
        PlayerPrefs.Save();
    }

    private bool TryApply(int index)
    {
        if (_mixer == null || string.IsNullOrEmpty(_parameters[index]))
        {
            return false;
        }

        return _mixer.SetFloat(_parameters[index], ToDecibels(_volumes[index]));
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _volumes.Length;
    }

    private static float ToDecibels(float linear)
    {
        return linear <= MinLinear ? MinDecibels : Mathf.Log10(linear) * 20f;
    }
}
