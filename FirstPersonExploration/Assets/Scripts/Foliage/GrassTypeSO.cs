// GrassTypeSO
// Responsibility: Shared, immutable definition of one grass variety (flyweight). Holds render data
// (mesh, material, draw distance, shadow flags) and bake rules (density, scale range, mask
// threshold, normal alignment). Never holds runtime state.
using UnityEngine;

namespace Game.Foliage
{
    [CreateAssetMenu(fileName = "GrassType", menuName = "Game/Foliage/Grass Type")]
    public sealed class GrassTypeSO : ScriptableObject
    {
        [Header("Rendering")]
        [SerializeField, Tooltip("Clump mesh with its pivot at the base and Y up.")]
        private Mesh mesh;

        [SerializeField, Tooltip("Must have Enable GPU Instancing ticked.")]
        private Material material;

        [SerializeField, Min(1f), Tooltip("Chunks farther than this from the camera are not drawn.")]
        private float drawDistance = 35f;

        [SerializeField, Tooltip("Grass shadows are expensive. Leave off unless measured affordable.")]
        private bool castShadows;

        [SerializeField]
        private bool receiveShadows = true;

        [Header("Baking")]
        [SerializeField, Range(0.05f, 16f), Tooltip("Clumps per square metre where the mask layer is fully painted.")]
        private float density = 2f;

        [SerializeField, Min(0.01f)]
        private float minScale = 0.8f;

        [SerializeField, Min(0.01f)]
        private float maxScale = 1.2f;

        [SerializeField, Range(0f, 1f), Tooltip("Terrain layer weight below which no grass is placed.")]
        private float maskThreshold = 0.3f;

        [SerializeField, Range(0f, 1f), Tooltip("0 keeps clumps vertical, 1 tilts them fully to the terrain normal.")]
        private float normalAlignment = 1f;

        public Mesh Mesh => mesh;
        public Material Material => material;
        public float DrawDistance => drawDistance;
        public bool CastShadows => castShadows;
        public bool ReceiveShadows => receiveShadows;
        public float Density => density;
        public float MinScale => minScale;
        public float MaxScale => maxScale;
        public float MaskThreshold => maskThreshold;
        public float NormalAlignment => normalAlignment;

        private void OnValidate()
        {
            if (maxScale < minScale)
            {
                maxScale = minScale;
            }
        }
    }
}
