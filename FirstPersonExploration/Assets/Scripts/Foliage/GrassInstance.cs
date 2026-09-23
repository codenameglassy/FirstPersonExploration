// GrassInstance
// Responsibility: Immutable baked placement of one grass clump: world position, rotation (yaw plus
// tilt toward the terrain normal) and uniform scale. Stored as eight floats instead of a full matrix
// so field assets stay small in WebGL builds.
using System;
using UnityEngine;

namespace Game.Foliage
{
    [Serializable]
    public struct GrassInstance
    {
        [SerializeField] private Vector3 position;
        [SerializeField] private Quaternion rotation;
        [SerializeField] private float scale;

        public GrassInstance(Vector3 position, Quaternion rotation, float scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public Vector3 Position => position;
        public Quaternion Rotation => rotation;
        public float Scale => scale;

        public Matrix4x4 ToMatrix()
        {
            return Matrix4x4.TRS(position, rotation, new Vector3(scale, scale, scale));
        }
    }
}
