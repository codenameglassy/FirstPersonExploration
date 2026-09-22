// CameraEffectStack
// Responsibility: Sums every registered ICameraEffect once per frame and applies the result to the
// camera on top of the rig's pose. The only class that writes the camera's transform and FOV.
// Runs after FirstPersonCameraRig so effects always stack on this frame's eye point.
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    [DefaultExecutionOrder(ExecutionOrder)]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraEffectStack : MonoBehaviour
    {
        private const int ExecutionOrder = 32010;
        private const int InitialCapacity = 8;

        [SerializeField] private FirstPersonCameraRig rig;

        private readonly List<ICameraEffect> effects = new List<ICameraEffect>(InitialCapacity);

        private Camera targetCamera;
        private float baseFieldOfView;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            baseFieldOfView = targetCamera.fieldOfView;

            if (rig == null)
            {
                Debug.LogError("CameraEffectStack: FirstPersonCameraRig is not assigned.", this);
                enabled = false;
            }
        }

        public void Register(ICameraEffect effect)
        {
            if (effect == null || effects.Contains(effect))
            {
                return;
            }

            effects.Add(effect);
        }

        public void Unregister(ICameraEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            effects.Remove(effect);
        }

        private void LateUpdate()
        {
            CameraOffset offset = default;
            float deltaTime = Time.deltaTime;

            for (int i = 0; i < effects.Count; i++)
            {
                effects[i].Evaluate(deltaTime, ref offset);
            }

            Transform rigTransform = rig.transform;
            transform.SetPositionAndRotation(
                rigTransform.position + rig.YawRotation * offset.Position,
                rigTransform.rotation * Quaternion.Euler(offset.Rotation));

            targetCamera.fieldOfView = baseFieldOfView + offset.FieldOfView;
        }
    }
}
