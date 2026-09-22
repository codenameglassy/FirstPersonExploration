// FirstPersonCharacter
// Responsibility: Movement brain for the KCC motor. Turns CharacterInputs into rotation and velocity
// (walk, jog, crouch with separate acceleration and deceleration, jump with buffer, coyote time and
// hold-to-extend height, ceiling bumps) and reports jumps, landings and ceiling bumps through plain
// C# events for components on the same prefab. Reads no devices and knows nothing about the camera.
using System;
using KinematicCharacterController;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public sealed class FirstPersonCharacter : MonoBehaviour, ICharacterController
    {
        private const int MaxProbedColliders = 8;

        // A hit counts as a ceiling when its normal is within 60 degrees of straight down.
        private const float CeilingNormalThreshold = 0.5f;

        // Upward speed below this never counts as a bump (filters resting contact and tiny drifts).
        private const float MinCeilingBumpSpeed = 0.5f;

        [SerializeField] private LocomotionProfileSO profile;

        private readonly Collider[] probedColliders = new Collider[MaxProbedColliders];

        private KinematicCharacterMotor motor;
        private bool isConfigured;

        private Vector3 moveInput;
        private Vector3 lookDirection;
        private bool jogHeld;

        private bool jumpRequested;
        private float timeSinceJumpRequested;
        private bool jumpHeld;
        private bool jumpConsumed;
        private bool jumpedThisTick;
        private bool isJumpAscending;
        private float timeSinceLastAbleToJump = float.PositiveInfinity;

        private bool shouldBeCrouching;
        private bool isCrouching;

        private float lastAirborneVerticalSpeed;
        private float verticalSpeedThisTick;
        private float pendingCeilingSpeed;

        // Raised on the physics tick the jump is applied.
        public event Action Jumped;

        // Raised on the physics tick the character becomes stably grounded. Payload is downward speed at impact.
        public event Action<float> Landed;

        // Raised on the physics tick after the head hits a ceiling. Payload is upward speed at impact.
        public event Action<float> CeilingBumped;

        public LocomotionProfileSO Profile => profile;
        public KinematicCharacterMotor Motor => motor;
        public bool IsGrounded => motor.GroundingStatus.IsStableOnGround;
        public bool IsCrouching => isCrouching;
        public bool IsJogging => jogHeld && !isCrouching;
        public bool HasMoveInput => moveInput.sqrMagnitude > 0.0001f;
        public Vector3 Velocity => motor.BaseVelocity;

        private void Awake()
        {
            motor = GetComponent<KinematicCharacterMotor>();
            motor.CharacterController = this;

            lookDirection = Vector3.ProjectOnPlane(transform.forward, transform.up).normalized;

            if (profile == null)
            {
                Debug.LogError("FirstPersonCharacter: No LocomotionProfileSO assigned. Motor disabled.", this);
                motor.enabled = false;
                return;
            }

            isConfigured = true;
        }

        private void Start()
        {
            if (isConfigured)
            {
                ApplyStandingCapsule();
            }
        }

        public void SetInputs(ref CharacterInputs inputs)
        {
            if (!isConfigured)
            {
                return;
            }

            Vector3 up = motor.CharacterUp;
            Vector3 planarForward = Vector3.ProjectOnPlane(inputs.LookRotation * Vector3.forward, up);
            if (planarForward.sqrMagnitude < 0.0001f)
            {
                planarForward = Vector3.ProjectOnPlane(inputs.LookRotation * Vector3.up, up);
            }

            planarForward.Normalize();
            lookDirection = planarForward;

            Vector2 move = Vector2.ClampMagnitude(inputs.Move, 1f);
            Quaternion planarRotation = Quaternion.LookRotation(planarForward, up);
            moveInput = planarRotation * new Vector3(move.x, 0f, move.y);

            jogHeld = inputs.JogHeld;
            jumpHeld = inputs.JumpHeld;

            if (inputs.JumpPressed)
            {
                jumpRequested = true;
                timeSinceJumpRequested = 0f;
            }

            shouldBeCrouching = inputs.CrouchHeld;
            if (shouldBeCrouching && !isCrouching)
            {
                isCrouching = true;
                ApplyCrouchCapsule();
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            jumpedThisTick = false;
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (lookDirection.sqrMagnitude > 0f)
            {
                currentRotation = Quaternion.LookRotation(lookDirection, motor.CharacterUp);
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (pendingCeilingSpeed > 0f)
            {
                ApplyCeilingBump(ref currentVelocity);
            }

            if (motor.GroundingStatus.IsStableOnGround)
            {
                UpdateGroundVelocity(ref currentVelocity, deltaTime);
            }
            else
            {
                UpdateAirVelocity(ref currentVelocity, deltaTime);
            }

            TryJump(ref currentVelocity);

            // Recorded after all velocity changes, so movement hits this tick know the upward speed they carried.
            verticalSpeedThisTick = Vector3.Dot(currentVelocity, motor.CharacterUp);
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            bool grounded = motor.GroundingStatus.IsStableOnGround;
            bool wasGrounded = motor.LastGroundingStatus.IsStableOnGround;

            if (grounded && !wasGrounded)
            {
                Landed?.Invoke(Mathf.Max(0f, -lastAirborneVerticalSpeed));
            }
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            if (jumpRequested)
            {
                timeSinceJumpRequested += deltaTime;
                if (timeSinceJumpRequested > profile.JumpBufferTime)
                {
                    jumpRequested = false;
                }
            }

            // Grounding here still reflects this tick's probe, even on the tick a jump ungrounds us,
            // so the jump itself must not immediately refund its own consumption.
            if (motor.GroundingStatus.IsStableOnGround)
            {
                if (!jumpedThisTick)
                {
                    jumpConsumed = false;
                    isJumpAscending = false;
                }

                timeSinceLastAbleToJump = 0f;
            }
            else
            {
                timeSinceLastAbleToJump += deltaTime;
            }

            if (isCrouching && !shouldBeCrouching)
            {
                TryStandUp();
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // Only while airborne or on the jump tick itself, where grounding still reads as stable.
            bool airborne = !motor.GroundingStatus.IsStableOnGround || jumpedThisTick;
            if (!airborne || verticalSpeedThisTick < MinCeilingBumpSpeed)
            {
                return;
            }

            if (Vector3.Dot(hitNormal, motor.CharacterUp) > -CeilingNormalThreshold)
            {
                return;
            }

            // Several hits can arrive in one tick; keep the strongest.
            pendingCeilingSpeed = Mathf.Max(pendingCeilingSpeed, verticalSpeedThisTick);
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        private void ApplyCeilingBump(ref Vector3 currentVelocity)
        {
            // Kill any upward speed that survived the collision, so sloped ceilings don't carry us along them.
            Vector3 up = motor.CharacterUp;
            float upwardSpeed = Vector3.Dot(currentVelocity, up);
            if (upwardSpeed > 0f)
            {
                currentVelocity -= up * upwardSpeed;
            }

            isJumpAscending = false;
            CeilingBumped?.Invoke(pendingCeilingSpeed);
            pendingCeilingSpeed = 0f;
        }

        private void UpdateGroundVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 groundNormal = motor.GroundingStatus.GroundNormal;

            // Keep speed but redirect along the ground so slopes don't bleed velocity.
            float speed = currentVelocity.magnitude;
            currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, groundNormal) * speed;

            // Reorient input onto the ground plane.
            Vector3 inputRight = Vector3.Cross(moveInput, motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(groundNormal, inputRight).normalized * moveInput.magnitude;
            Vector3 targetVelocity = reorientedInput * CurrentGroundSpeed();

            // Braking: stopping, dropping to a slower target, or reversing against current motion.
            // Everything else (starting, turning, speeding up) accelerates.
            bool braking = targetVelocity.sqrMagnitude < currentVelocity.sqrMagnitude
                || Vector3.Dot(targetVelocity, currentVelocity) < 0f;
            float sharpness = braking ? profile.GroundDeceleration : profile.GroundAcceleration;

            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-sharpness * deltaTime));
        }

        private void UpdateAirVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 up = motor.CharacterUp;

            if (moveInput.sqrMagnitude > 0f)
            {
                Vector3 addedVelocity = moveInput * (profile.AirAcceleration * deltaTime);
                Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, up);

                if (planarVelocity.magnitude < profile.MaxAirSpeed)
                {
                    // Accelerate, but never past max air speed.
                    Vector3 clampedPlanar = Vector3.ClampMagnitude(planarVelocity + addedVelocity, profile.MaxAirSpeed);
                    addedVelocity = clampedPlanar - planarVelocity;
                }
                else if (Vector3.Dot(planarVelocity, addedVelocity) > 0f)
                {
                    // Already over max: allow steering, not further acceleration.
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, planarVelocity.normalized);
                }

                // Prevent air-climbing steep slopes the character is touching but not stable on.
                if (motor.GroundingStatus.FoundAnyGround && Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                {
                    Vector3 obstructionNormal = Vector3.Cross(Vector3.Cross(up, motor.GroundingStatus.GroundNormal), up).normalized;
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, obstructionNormal);
                }

                currentVelocity += addedVelocity;
            }

            // Variable jump height: holding the button while rising lightens gravity, extending the apex.
            float gravityScale = isJumpAscending && jumpHeld ? profile.HeldJumpGravityScale : 1f;
            currentVelocity += -up * (profile.Gravity * gravityScale * deltaTime);
            currentVelocity *= 1f / (1f + profile.AirDrag * deltaTime);

            lastAirborneVerticalSpeed = Vector3.Dot(currentVelocity, up);
            if (isJumpAscending && lastAirborneVerticalSpeed <= 0f)
            {
                isJumpAscending = false;
            }
        }

        private void TryJump(ref Vector3 currentVelocity)
        {
            if (!jumpRequested || jumpConsumed)
            {
                return;
            }

            // Coyote time: a short grace window after leaving stable ground.
            bool canJump = motor.GroundingStatus.IsStableOnGround || timeSinceLastAbleToJump <= profile.CoyoteTime;
            if (!canJump)
            {
                return;
            }

            Vector3 up = motor.CharacterUp;
            motor.ForceUnground();
            currentVelocity += up * profile.JumpSpeed - Vector3.Project(currentVelocity, up);

            jumpRequested = false;
            jumpConsumed = true;
            jumpedThisTick = true;
            isJumpAscending = true;
            Jumped?.Invoke();
        }

        private float CurrentGroundSpeed()
        {
            if (isCrouching)
            {
                return profile.CrouchSpeed;
            }

            return jogHeld ? profile.JogSpeed : profile.WalkSpeed;
        }

        private void TryStandUp()
        {
            ApplyStandingCapsule();

            int overlaps = motor.CharacterOverlap(
                motor.TransientPosition,
                motor.TransientRotation,
                probedColliders,
                motor.CollidableLayers,
                QueryTriggerInteraction.Ignore);

            if (overlaps > 0)
            {
                ApplyCrouchCapsule();
            }
            else
            {
                isCrouching = false;
            }
        }

        private void ApplyStandingCapsule()
        {
            float height = profile.StandingHeight;
            motor.SetCapsuleDimensions(profile.CapsuleRadius, height, height * 0.5f);
        }

        private void ApplyCrouchCapsule()
        {
            float height = profile.CrouchHeight;
            motor.SetCapsuleDimensions(profile.CapsuleRadius, height, height * 0.5f);
        }
    }
}