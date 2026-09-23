// GrassFieldRenderer
// Responsibility: Draws baked GrassFieldSO assets with GPU instancing. Culls whole chunks against
// the camera frustum and each grass type's draw distance, then issues RenderMeshInstanced calls.
// Runs after the camera rig (32000) and effect stack (32010) so culling uses the final camera pose.
// Owns all runtime matrix data so the shared field assets stay immutable. Allocation free per frame.
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Foliage
{
    [DefaultExecutionOrder(32100)]
    [DisallowMultipleComponent]
    public sealed class GrassFieldRenderer : MonoBehaviour
    {
        private const int MaxInstancesPerDraw = 1023;

        [SerializeField] private GrassFieldSO[] fields;
        [SerializeField] private Camera cullingCamera;

        private readonly Plane[] frustumPlanes = new Plane[6];

        private RenderParams[] layerRenderParams;
        private Mesh[] layerMeshes;
        private float[] layerSqrDrawDistances;

        private Matrix4x4[][] chunkMatrices;
        private Bounds[] chunkBounds;
        private int[] chunkLayers;
        private int chunkCount;

        private Transform cameraTransform;

        private void Awake()
        {
            if (cullingCamera == null)
            {
                Debug.LogWarning($"{nameof(GrassFieldRenderer)} on '{name}': no culling camera assigned, grass will not render.", this);
            }
            else
            {
                cameraTransform = cullingCamera.transform;
            }

            BuildRuntimeData();
        }

        private void LateUpdate()
        {
            if (cullingCamera == null || chunkCount == 0)
            {
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(cullingCamera, frustumPlanes);
            Vector3 cameraPosition = cameraTransform.position;

            for (int c = 0; c < chunkCount; c++)
            {
                int layer = chunkLayers[c];
                Bounds bounds = chunkBounds[c];

                if (bounds.SqrDistance(cameraPosition) > layerSqrDrawDistances[layer])
                {
                    continue;
                }

                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                {
                    continue;
                }

                DrawChunk(layer, chunkMatrices[c], bounds);
            }
        }

        private void DrawChunk(int layer, Matrix4x4[] matrices, Bounds bounds)
        {
            ref RenderParams renderParams = ref layerRenderParams[layer];
            renderParams.worldBounds = bounds;
            Mesh mesh = layerMeshes[layer];

            for (int start = 0; start < matrices.Length; start += MaxInstancesPerDraw)
            {
                int count = Mathf.Min(MaxInstancesPerDraw, matrices.Length - start);
                Graphics.RenderMeshInstanced(renderParams, mesh, 0, matrices, count, start);
            }
        }

        private void BuildRuntimeData()
        {
            int layerCount = fields != null ? fields.Length : 0;
            layerRenderParams = new RenderParams[layerCount];
            layerMeshes = new Mesh[layerCount];
            layerSqrDrawDistances = new float[layerCount];
            bool[] layerValid = new bool[layerCount];

            int totalChunks = 0;
            for (int i = 0; i < layerCount; i++)
            {
                layerValid[i] = Validate(fields[i], i);
                if (layerValid[i])
                {
                    totalChunks += fields[i].ChunkCount;
                }
            }

            chunkMatrices = new Matrix4x4[totalChunks][];
            chunkBounds = new Bounds[totalChunks];
            chunkLayers = new int[totalChunks];
            chunkCount = 0;

            for (int i = 0; i < layerCount; i++)
            {
                if (!layerValid[i])
                {
                    continue;
                }

                GrassFieldSO field = fields[i];
                GrassTypeSO type = field.GrassType;

                layerMeshes[i] = type.Mesh;
                layerSqrDrawDistances[i] = type.DrawDistance * type.DrawDistance;
                layerRenderParams[i] = new RenderParams(type.Material)
                {
                    shadowCastingMode = type.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = type.ReceiveShadows,
                    layer = gameObject.layer
                };

                for (int c = 0; c < field.ChunkCount; c++)
                {
                    GrassChunk chunk = field.GetChunk(c);
                    int count = chunk.InstanceCount;
                    if (count == 0)
                    {
                        continue;
                    }

                    Matrix4x4[] matrices = new Matrix4x4[count];
                    for (int j = 0; j < count; j++)
                    {
                        matrices[j] = chunk.GetInstance(j).ToMatrix();
                    }

                    chunkMatrices[chunkCount] = matrices;
                    chunkBounds[chunkCount] = chunk.Bounds;
                    chunkLayers[chunkCount] = i;
                    chunkCount++;
                }
            }
        }

        private bool Validate(GrassFieldSO field, int index)
        {
            string prefix = $"{nameof(GrassFieldRenderer)} on '{name}', field {index}:";

            if (field == null)
            {
                Debug.LogWarning($"{prefix} empty slot, skipped.", this);
                return false;
            }

            GrassTypeSO type = field.GrassType;
            if (type == null)
            {
                Debug.LogWarning($"{prefix} '{field.name}' has no grass type. Bake it first.", this);
                return false;
            }

            if (type.Mesh == null || type.Material == null)
            {
                Debug.LogWarning($"{prefix} grass type '{type.name}' is missing its mesh or material.", this);
                return false;
            }

            if (!type.Material.enableInstancing)
            {
                Debug.LogWarning($"{prefix} material '{type.Material.name}' needs Enable GPU Instancing ticked.", this);
                return false;
            }

            if (field.ChunkCount == 0)
            {
                Debug.LogWarning($"{prefix} '{field.name}' contains no baked chunks.", this);
                return false;
            }

            return true;
        }
    }
}
