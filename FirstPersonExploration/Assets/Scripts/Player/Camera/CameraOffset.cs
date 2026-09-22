// CameraOffset
// Responsibility: Accumulator that camera effects add into each frame. Position is in the rig's
// level (yaw only) frame, Rotation is local Euler degrees, FieldOfView is a delta on the base FOV.
using UnityEngine;

namespace Game.Player
{
    public struct CameraOffset
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public float FieldOfView;
    }
}
