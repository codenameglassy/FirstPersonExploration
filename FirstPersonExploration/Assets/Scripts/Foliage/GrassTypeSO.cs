// GrassTypeSO
// Responsibility: Shared, immutable definition of one grass variety (flyweight). Holds render data
// (mesh, material, draw distance, shadow flags) and bake rules (density, scale, mask threshold,
// normal alignment, clustering, soft edges, slope and altitude limits, collider exclusion).
// Never holds runtime state.
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

        [SerializeField, Min(1f), Tooltip("Chunks farther than this from the camera are not drawn. Match the material's Fade End.")]
        private float drawDistance = 35f;

        [SerializeField, Tooltip("Grass shadows are expensive. Leave off unless measured affordable.")]
        private bool castShadows;

        [SerializeField]
        private bool receiveShadows = true;

        [Header("Baking")]
        [SerializeField, Range(0.05f, 16f), Tooltip("Clumps per square metre where coverage is full.")]
        private float density = 2f;

        [SerializeField, Min(0.01f)]
        private float minScale = 0.8f;

        [SerializeField, Min(0.01f)]
        private float maxScale = 1.2f;

        [SerializeField, Range(0f, 1f), Tooltip("Terrain layer weight below which no grass is placed.")]
        private float maskThreshold = 0.3f;

        [SerializeField, Range(0f, 1f), Tooltip("0 keeps clumps vertical, 1 tilts them fully to the terrain normal.")]
        private float normalAlignment = 1f;

        [Header("Clustering")]
        [SerializeField, Min(0.5f), Tooltip("Size in metres of natural clumps and bare gaps.")]
        private float clusterScale = 6f;

        [SerializeField, Range(0f, 1f), Tooltip("0 gives an even carpet, 1 gives distinct clumps with bare gaps.")]
        private float clusterStrength = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("How much clumps shrink where coverage thins: mask borders, cluster gaps, slope and altitude fades.")]
        private float edgeShrink = 0.6f;

        [Header("Slope")]
        [SerializeField, Range(0f, 90f), Tooltip("Steepest slope in degrees that still grows this grass.")]
        private float maxSlope = 40f;

        [SerializeField, Range(0f, 30f), Tooltip("Degrees below Max Slope over which grass thins out.")]
        private float slopeFade = 8f;

        [Header("Altitude")]
        [SerializeField]
        private bool limitAltitude;

        [SerializeField, Tooltip("World height in metres.")]
        private float minAltitude;

        [SerializeField, Tooltip("World height in metres.")]
        private float maxAltitude = 100f;

        [SerializeField, Min(0f), Tooltip("Metres inside each limit over which grass thins out.")]
        private float altitudeFade = 5f;

        [Header("Exclusion")]
        [SerializeField, Tooltip("Colliders on these layers block grass, such as rocks, buildings and props. The terrain's own collider is always ignored.")]
        private LayerMask exclusionLayers;

        [SerializeField, Min(0.01f), Tooltip("Clear distance in metres kept around blocking colliders.")]
        private float exclusionClearance = 0.3f;

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
        public float ClusterScale => clusterScale;
        public float ClusterStrength => clusterStrength;
        public float EdgeShrink => edgeShrink;
        public float MaxSlope => maxSlope;
        public float SlopeFade => slopeFade;
        public bool LimitAltitude => limitAltitude;
        public float MinAltitude => minAltitude;
        public float MaxAltitude => maxAltitude;
        public float AltitudeFade => altitudeFade;
        public LayerMask ExclusionLayers => exclusionLayers;
        public float ExclusionClearance => exclusionClearance;

        private void OnValidate()
        {
            if (maxScale < minScale)
            {
                maxScale = minScale;
            }

            if (maxAltitude < minAltitude)
            {
                maxAltitude = minAltitude;
            }
        }
    }
}
