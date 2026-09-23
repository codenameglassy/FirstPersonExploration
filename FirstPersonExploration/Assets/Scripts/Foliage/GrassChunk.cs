// GrassChunk
// Responsibility: One spatial cell of baked grass: its world-space culling bounds and the instances
// inside it. Chunks are the unit of culling at runtime.
using System;
using UnityEngine;

namespace Game.Foliage
{
    [Serializable]
    public struct GrassChunk
    {
        [SerializeField] private Bounds bounds;
        [SerializeField] private GrassInstance[] instances;

        public GrassChunk(Bounds bounds, GrassInstance[] instances)
        {
            this.bounds = bounds;
            this.instances = instances ?? Array.Empty<GrassInstance>();
        }

        public Bounds Bounds => bounds;
        public int InstanceCount => instances != null ? instances.Length : 0;

        public GrassInstance GetInstance(int index)
        {
            return instances[index];
        }
    }
}
