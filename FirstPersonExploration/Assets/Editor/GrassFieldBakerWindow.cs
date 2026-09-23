// GrassFieldBakerWindow
// Responsibility: Editor tool that scatters one GrassTypeSO over a terrain wherever a chosen terrain
// layer is painted. Applies the type's placement rules (clustering, slope and altitude limits with
// soft fades, collider exclusion), shrinks clumps where coverage thins, tilts each clump toward the
// terrain normal, groups the result into spatial chunks and writes a GrassFieldSO. Deterministic for
// a given seed, and changing the mask or rules never reshuffles surviving clumps. Editor only.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Foliage.EditorTools
{
    public sealed class GrassFieldBakerWindow : EditorWindow
    {
        private const float ClusterContrastLow = 0.3f;
        private const float ClusterContrastHigh = 0.7f;

        private readonly Collider[] overlapBuffer = new Collider[16];

        private Terrain terrain;
        private int terrainLayerIndex;
        private GrassTypeSO grassType;
        private GrassFieldSO targetField;
        private float chunkSize = 16f;
        private int seed = 1234;

        [MenuItem("Game/Foliage/Grass Field Baker")]
        private static void Open()
        {
            GetWindow<GrassFieldBakerWindow>("Grass Field Baker");
        }

        private void OnGUI()
        {
            terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
            DrawLayerPopup();
            grassType = (GrassTypeSO)EditorGUILayout.ObjectField("Grass Type", grassType, typeof(GrassTypeSO), false);
            targetField = (GrassFieldSO)EditorGUILayout.ObjectField(
                new GUIContent("Target Field", "Leave empty to create a new asset."),
                targetField, typeof(GrassFieldSO), false);
            chunkSize = Mathf.Max(4f, EditorGUILayout.FloatField("Chunk Size", chunkSize));
            seed = EditorGUILayout.IntField(
                new GUIContent("Seed", "Use a different seed for each layered type so they don't share positions."),
                seed);

            EditorGUILayout.Space();

            string problem = GetProblem();
            if (problem != null)
            {
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(problem != null))
            {
                if (GUILayout.Button("Bake"))
                {
                    Bake();
                }
            }
        }

        private void DrawLayerPopup()
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            TerrainLayer[] layers = terrain.terrainData.terrainLayers;
            if (layers.Length == 0)
            {
                return;
            }

            string[] names = new string[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                names[i] = layers[i] != null ? layers[i].name : "Missing layer " + i;
            }

            terrainLayerIndex = Mathf.Clamp(terrainLayerIndex, 0, layers.Length - 1);
            terrainLayerIndex = EditorGUILayout.Popup("Mask Layer", terrainLayerIndex, names);
        }

        private string GetProblem()
        {
            if (terrain == null)
            {
                return "Assign a terrain.";
            }

            if (terrain.terrainData == null)
            {
                return "The terrain has no TerrainData.";
            }

            if (terrain.terrainData.alphamapLayers == 0)
            {
                return "The terrain has no painted layers to use as a mask.";
            }

            if (grassType == null)
            {
                return "Assign a grass type.";
            }

            if (grassType.Mesh == null)
            {
                return "The grass type has no mesh.";
            }

            return null;
        }

        private void Bake()
        {
            TerrainData data = terrain.terrainData;
            if (terrainLayerIndex < 0 || terrainLayerIndex >= data.alphamapLayers)
            {
                EditorUtility.DisplayDialog("Grass Field Baker", "The selected mask layer is out of range.", "OK");
                return;
            }

            string newAssetPath = null;
            if (targetField == null)
            {
                newAssetPath = EditorUtility.SaveFilePanelInProject(
                    "Save Grass Field", grassType.name + "_Field", "asset", "Choose where to save the baked grass field.");
                if (string.IsNullOrEmpty(newAssetPath))
                {
                    return;
                }
            }

            GrassChunk[] chunks;
            int excludedCount;
            try
            {
                chunks = Scatter(data, out excludedCount);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (chunks == null)
            {
                return;
            }

            GrassFieldSO field = targetField;
            if (field == null)
            {
                field = CreateInstance<GrassFieldSO>();
                AssetDatabase.CreateAsset(field, newAssetPath);
            }

            Undo.RecordObject(field, "Bake Grass Field");
            field.SetBakedData(grassType, chunkSize, chunks);
            EditorUtility.SetDirty(field);
            AssetDatabase.SaveAssets();
            targetField = field;

            Debug.Log($"Grass Field Baker: {field.TotalInstances} instances in {field.ChunkCount} chunks, " +
                      $"{excludedCount} skipped by exclusion colliders.", field);
        }

        private GrassChunk[] Scatter(TerrainData data, out int excludedCount)
        {
            excludedCount = 0;

            Vector3 origin = terrain.transform.position;
            Vector3 size = data.size;
            int alphaWidth = data.alphamapWidth;
            int alphaHeight = data.alphamapHeight;
            float[,,] alphamaps = data.GetAlphamaps(0, 0, alphaWidth, alphaHeight);

            float spacing = 1f / Mathf.Sqrt(grassType.Density);
            float threshold = grassType.MaskThreshold;
            float minScale = grassType.MinScale;
            float maxScale = grassType.MaxScale;
            float normalAlignment = grassType.NormalAlignment;
            float edgeShrink = grassType.EdgeShrink;
            Vector2 clusterOffset = GetClusterOffset(seed);
            Collider terrainCollider = terrain.GetComponent<TerrainCollider>();

            // Edit mode physics may hold stale transforms for colliders moved since the last sync.
            Physics.SyncTransforms();

            System.Random random = new System.Random(seed);
            Dictionary<Vector2Int, List<GrassInstance>> buckets = new Dictionary<Vector2Int, List<GrassInstance>>();

            int rows = Mathf.CeilToInt(size.z / spacing);
            int columns = Mathf.CeilToInt(size.x / spacing);

            for (int row = 0; row < rows; row++)
            {
                if ((row & 15) == 0 &&
                    EditorUtility.DisplayCancelableProgressBar("Grass Field Baker", "Scattering grass", (float)row / rows))
                {
                    return null;
                }

                for (int column = 0; column < columns; column++)
                {
                    // Every random value is drawn before any rejection so each grid cell always
                    // consumes the same numbers. Changing the mask or rules leaves other cells unchanged.
                    float localX = (column + (float)random.NextDouble()) * spacing;
                    float localZ = (row + (float)random.NextDouble()) * spacing;
                    float yaw = (float)random.NextDouble() * 360f;
                    float scale = Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());
                    float keepRoll = (float)random.NextDouble();

                    if (localX >= size.x || localZ >= size.z)
                    {
                        continue;
                    }

                    int alphaX = Mathf.Clamp(Mathf.RoundToInt(localX / size.x * (alphaWidth - 1)), 0, alphaWidth - 1);
                    int alphaZ = Mathf.Clamp(Mathf.RoundToInt(localZ / size.z * (alphaHeight - 1)), 0, alphaHeight - 1);
                    float mask = alphamaps[alphaZ, alphaX, terrainLayerIndex];
                    if (mask < threshold)
                    {
                        continue;
                    }

                    float worldX = origin.x + localX;
                    float worldZ = origin.z + localZ;

                    // Cheap rejection first. Later factors only lower the weight further.
                    float coverage = mask * GetClusterWeight(worldX, worldZ, clusterOffset);
                    if (keepRoll > coverage)
                    {
                        continue;
                    }

                    Vector3 world = new Vector3(worldX, 0f, worldZ);
                    world.y = origin.y + terrain.SampleHeight(world);

                    // Terrains cannot be rotated, so the terrain-space normal is already world space.
                    Vector3 terrainNormal = data.GetInterpolatedNormal(localX / size.x, localZ / size.z);

                    float weight = coverage * GetSlopeWeight(terrainNormal) * GetAltitudeWeight(world.y);
                    if (keepRoll > weight)
                    {
                        continue;
                    }

                    if (IsExcluded(world, terrainCollider))
                    {
                        excludedCount++;
                        continue;
                    }

                    Vector3 up = Vector3.Slerp(Vector3.up, terrainNormal, normalAlignment).normalized;
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0f, yaw, 0f);
                    float finalScale = scale * Mathf.Lerp(1f - edgeShrink, 1f, weight);

                    Vector2Int key = new Vector2Int(Mathf.FloorToInt(localX / chunkSize), Mathf.FloorToInt(localZ / chunkSize));
                    if (!buckets.TryGetValue(key, out List<GrassInstance> list))
                    {
                        list = new List<GrassInstance>();
                        buckets.Add(key, list);
                    }

                    list.Add(new GrassInstance(world, rotation, finalScale));
                }
            }

            return BuildChunks(buckets);
        }

        private float GetClusterWeight(float worldX, float worldZ, Vector2 offset)
        {
            float strength = grassType.ClusterStrength;
            if (strength <= 0f)
            {
                return 1f;
            }

            float clusterScale = grassType.ClusterScale;
            float noise = Mathf.PerlinNoise(worldX / clusterScale + offset.x, worldZ / clusterScale + offset.y);
            float shaped = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ClusterContrastLow, ClusterContrastHigh, noise));
            return Mathf.Lerp(1f, shaped, strength);
        }

        private float GetSlopeWeight(Vector3 terrainNormal)
        {
            float slope = Vector3.Angle(Vector3.up, terrainNormal);
            return FadeBelowLimit(slope, grassType.MaxSlope, grassType.SlopeFade);
        }

        private float GetAltitudeWeight(float height)
        {
            if (!grassType.LimitAltitude)
            {
                return 1f;
            }

            float fade = grassType.AltitudeFade;
            return FadeBelowLimit(height, grassType.MaxAltitude, fade) *
                   FadeBelowLimit(-height, -grassType.MinAltitude, fade);
        }

        private bool IsExcluded(Vector3 world, Collider terrainCollider)
        {
            int layers = grassType.ExclusionLayers.value;
            if (layers == 0)
            {
                return false;
            }

            int count = Physics.OverlapSphereNonAlloc(
                world, grassType.ExclusionClearance, overlapBuffer, layers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (overlapBuffer[i] != terrainCollider)
                {
                    return true;
                }
            }

            return false;
        }

        private GrassChunk[] BuildChunks(Dictionary<Vector2Int, List<GrassInstance>> buckets)
        {
            Bounds meshBounds = grassType.Mesh.bounds;
            float padding = (meshBounds.center.magnitude + meshBounds.extents.magnitude) * grassType.MaxScale;

            GrassChunk[] chunks = new GrassChunk[buckets.Count];
            int index = 0;

            foreach (List<GrassInstance> list in buckets.Values)
            {
                Bounds bounds = new Bounds(list[0].Position, Vector3.zero);
                for (int i = 1; i < list.Count; i++)
                {
                    bounds.Encapsulate(list[i].Position);
                }

                bounds.Expand(padding * 2f);
                chunks[index] = new GrassChunk(bounds, list.ToArray());
                index++;
            }

            return chunks;
        }

        // 1 at or below limit minus fade, falling to 0 at the limit, 0 beyond it.
        private static float FadeBelowLimit(float value, float limit, float fade)
        {
            if (value >= limit)
            {
                return 0f;
            }

            if (fade <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01((limit - value) / fade);
        }

        // Seed-derived offset so types baked with different seeds get independent cluster patterns.
        private static Vector2 GetClusterOffset(int bakeSeed)
        {
            System.Random offsetRandom = new System.Random(unchecked(bakeSeed * 486187739));
            return new Vector2((float)offsetRandom.NextDouble() * 200f, (float)offsetRandom.NextDouble() * 200f);
        }
    }
}
