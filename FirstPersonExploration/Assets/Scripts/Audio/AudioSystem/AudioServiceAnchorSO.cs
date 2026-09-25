// Runtime anchor exposing the active IAudioService to any object that references this asset.
// Provide in OnEnable, Unset in OnDisable, null-check Value on every read.
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Service Anchor", fileName = "AudioServiceAnchor")]
public sealed class AudioServiceAnchorSO : ScriptableObject
{
    public IAudioService Value { get; private set; }
    public bool IsSet => Value != null;

    public bool Provide(IAudioService service)
    {
        if (service == null)
        {
            return false;
        }

        if (Value != null && Value != service)
        {
            return false;
        }

        Value = service;
        return true;
    }

    public void Unset(IAudioService service)
    {
        if (Value == service)
        {
            Value = null;
        }
    }

    private void OnDisable()
    {
        Value = null;
    }
}
