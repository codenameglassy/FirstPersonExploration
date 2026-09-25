// PlayerFootstepAudio
// Responsibility: Plays a footstep sound on every headbob footfall, choosing the cue for the
// current gait (walk, jog, crouch) and playing it at the character's feet through the audio
// service. Timing comes entirely from HeadbobEffect, so sound always lands on the visual step.
using UnityEngine;

namespace Game.Player
{
    public sealed class PlayerFootstepAudio : MonoBehaviour
    {
        [SerializeField] private AudioServiceAnchorSO audioService;
        [SerializeField] private FirstPersonCharacter character;
        [SerializeField] private HeadbobEffect headbob;

        [Header("Cues")]
        [SerializeField] private AudioCueSO walkCue;
        [Tooltip("Optional. Falls back to Walk Cue when empty.")]
        [SerializeField] private AudioCueSO jogCue;
        [Tooltip("Optional. Falls back to Walk Cue when empty.")]
        [SerializeField] private AudioCueSO crouchCue;

        private Transform feet;
        private bool isConfigured;

        private void Awake()
        {
            isConfigured = character != null && headbob != null && walkCue != null;
            if (!isConfigured)
            {
                Debug.LogError("PlayerFootstepAudio: Character, Headbob or Walk Cue is not assigned.", this);
                enabled = false;
                return;
            }

            // The character's pivot sits at the feet.
            feet = character.transform;
        }

        private void OnEnable()
        {
            if (isConfigured)
            {
                headbob.Footstep += HandleFootstep;
            }
        }

        private void OnDisable()
        {
            if (headbob != null)
            {
                headbob.Footstep -= HandleFootstep;
            }
        }

        private void HandleFootstep()
        {
            IAudioService service = audioService != null ? audioService.Value : null;
            if (service == null)
            {
                return;
            }

            service.PlayAt(SelectCue(), feet.position);
        }

        private AudioCueSO SelectCue()
        {
            if (character.IsCrouching)
            {
                return crouchCue != null ? crouchCue : walkCue;
            }

            if (character.IsJogging && character.HasMoveInput)
            {
                return jogCue != null ? jogCue : walkCue;
            }

            return walkCue;
        }
    }
}