// Sets the scene's base music on Start by crossfading to the assigned cue.
// Leave the cue empty to fade music out, for a scene that should start in silence.
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SceneMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioServiceAnchorSO _audioService;
    [SerializeField] private AudioCueSO _music;
    [SerializeField, Min(0f)] private float _crossfadeSeconds = 2f;

    private void Start()
    {
        IAudioService service = _audioService != null ? _audioService.Value : null;
        if (service == null)
        {
            return;
        }

        if (_music != null)
        {
            service.PlayMusic(_music, _crossfadeSeconds);
        }
        else
        {
            service.StopMusic(_crossfadeSeconds);
        }
    }
}
