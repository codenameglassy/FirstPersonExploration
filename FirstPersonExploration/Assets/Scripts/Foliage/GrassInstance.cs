// GrassInstance
// Responsibility: Immutable baked placement of one grass clump: world position, yaw and uniform
// scale. Stored as five floats instead of a full matrix so field assets stay small in WebGL builds.
using System;
using UnityEngine;

namespace Game.Foliage
{
    [Serializable]
    public struct GrassInstance
    {
        [SerializeField] private Vector3 position;
        [SerializeField] private float yaw;
        [SerializeField] private float scale;

        public GrassInstance(Vector3 position, float yaw, float scale)
        {
            this.position = position;
            this.yaw = yaw;
            this.scale = scale;
        }

        public Vector3 Position => position;
        public float Yaw => yaw;
        public float Scale => scale;

        public Matrix4x4 ToMatrix()
        {
            return Matrix4x4.TRS(position, Quaternion.Euler(0f, yaw, 0f), new Vector3(scale, scale, scale));
        }
    }
}
