// CharacterInputs
// Responsibility: Plain data snapshot of one frame of movement intent. Any source (player input,
// AI, cutscene) fills this and passes it to FirstPersonCharacter.SetInputs, so the character never
// knows where its intent came from.
using UnityEngine;

namespace Game.Player
{
    public struct CharacterInputs
    {
        // x = strafe right, y = forward. Clamped to a magnitude of 1 by the character.
        public Vector2 Move;

        // Full view rotation. Only its yaw on the character plane is used for movement.
        public Quaternion LookRotation;

        public bool JogHeld;

        // True only on the frame the jump was pressed.
        public bool JumpPressed;

        // True every frame the jump button is held. Releasing early shortens the jump.
        public bool JumpHeld;

        public bool CrouchHeld;
    }
}