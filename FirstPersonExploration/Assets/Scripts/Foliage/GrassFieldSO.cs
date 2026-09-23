// GrassFieldSO
// Responsibility: Baked output for one grass type on one terrain, stored as world-space chunks of
// instances. Written only by the editor baker and read-only at runtime. Runtime matrices live on
// GrassFieldRenderer so this shared asset never holds mutable state.
using UnityEngine;

namespace Game.Foliage
{
    public sealed class GrassFieldSO : ScriptableObject
    {
        [SerializeField] private GrassTypeSO grassType;
        [SerializeField] private float chunkSize;
        [SerializeField] private int totalInstances;
        [SerializeField, HideInInspector] private GrassChunk[] chunks;

        public GrassTypeSO GrassType => grassType;
        public float ChunkSize => chunkSize;
        public int TotalInstances => totalInstances;
        public int ChunkCount => chunks != null ? chunks.Length : 0;

        public GrassChunk GetChunk(int index)
        {
            return chunks[index];
        }

#if UNITY_EDITOR
        public void SetBakedData(GrassTypeSO bakedType, float bakedChunkSize, GrassChunk[] bakedChunks)
        {
            grassType = bakedType;
            chunkSize = bakedChunkSize;
            chunks = bakedChunks;
            totalInstances = 0;

            if (bakedChunks == null)
            {
                return;
            }

            for (int i = 0; i < bakedChunks.Length; i++)
            {
                totalInstances += bakedChunks[i].InstanceCount;
            }
        }
#endif
    }
}
