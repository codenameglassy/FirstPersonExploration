// LegacyPlayerInput
// Responsibility: Player input source using the legacy Input Manager. Reads devices once per frame,
// sends look deltas to the camera rig and a CharacterInputs snapshot to the character, and manages
// cursor lock. Replacing this component is the only change needed for a different input source.
using Game.Core;
using UnityEngine;

namespace Game.Player
{
    public sealed class LegacyPlayerInput : MonoBehaviour, ITickable
    {
        private const string HorizontalAxis = "Horizontal";
        private const string VerticalAxis = "Vertical";
        private const string MouseXAxis = "Mouse X";
        private const string MouseYAxis = "Mouse Y";
        private const string JumpButton = "Jump";

        [SerializeField] private FirstPersonCharacter character;
        [SerializeField] private FirstPersonCameraRig cameraRig;

        [Header("Look")]
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertY;

        [Header("Keys")]
        [SerializeField] private KeyCode jogKey = KeyCode.LeftShift;
        [Tooltip("C by default. Ctrl combos like Ctrl+W close the browser tab in WebGL builds.")]
        [SerializeField] private KeyCode crouchKey = KeyCode.C;
        [SerializeField] private KeyCode releaseCursorKey = KeyCode.Escape;

        private TickManager tickManager;
        private CharacterInputs inputs;
        private bool lockedThisFrame;

        private void OnEnable()
        {
            if (ServiceRegistry.TryGet(out tickManager))
            {
                tickManager.Register(this);
            }
            else
            {
                Debug.LogError("LegacyPlayerInput: TickManager not found. Is a Bootstrapper in the scene?", this);
            }

            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            if (tickManager != null)
            {
                tickManager.Unregister(this);
                tickManager = null;
            }

            SendNeutralInputs();
            SetCursorLocked(false);
        }

        public void Tick(float deltaTime)
        {
            UpdateCursorLock();

            bool locked = Cursor.lockState == CursorLockMode.Locked;

            // Skip look on the lock frame to avoid the pointer-lock delta spike.
            if (locked && !lockedThisFrame && cameraRig != null)
            {
                float lookY = Input.GetAxisRaw(MouseYAxis) * mouseSensitivity;
                if (invertY)
                {
                    lookY = -lookY;
                }

                cameraRig.AddLookDelta(new Vector2(Input.GetAxisRaw(MouseXAxis) * mouseSensitivity, lookY));
            }

            if (character == null)
            {
                return;
            }

            inputs.Move = locked ? new Vector2(Input.GetAxisRaw(HorizontalAxis), Input.GetAxisRaw(VerticalAxis)) : Vector2.zero;
            inputs.LookRotation = CurrentLookRotation();
            inputs.JogHeld = locked && Input.GetKey(jogKey);
            inputs.JumpPressed = locked && Input.GetButtonDown(JumpButton);
            inputs.JumpHeld = locked && Input.GetButton(JumpButton);
            inputs.CrouchHeld = locked && Input.GetKey(crouchKey);

            character.SetInputs(ref inputs);
        }

        private void UpdateCursorLock()
        {
            lockedThisFrame = false;

            if (Input.GetKeyDown(releaseCursorKey))
            {
                SetCursorLocked(false);
            }
            else if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                SetCursorLocked(true);
                lockedThisFrame = true;
            }
        }

        private void SendNeutralInputs()
        {
            if (character == null)
            {
                return;
            }

            inputs = default;
            inputs.LookRotation = CurrentLookRotation();
            character.SetInputs(ref inputs);
        }

        private Quaternion CurrentLookRotation()
        {
            if (cameraRig != null)
            {
                return cameraRig.LookRotation;
            }

            return character != null ? character.transform.rotation : Quaternion.identity;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
