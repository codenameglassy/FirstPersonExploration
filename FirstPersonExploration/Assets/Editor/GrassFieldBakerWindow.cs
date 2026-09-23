// GrassFieldBakerWindow
// Responsibility: Editor tool that scatters one GrassTypeSO over a terrain wherever a chosen terrain
// layer is painted, groups the result into spatial chunks and writes a GrassFieldSO. Deterministic
// for a given seed, and repainting the mask does not reshuffle untouched areas. Editor only.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Foliage.EditorTools
{
    public sealed class GrassFieldBakerWindow : EditorWindow
    {
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
            seed = EditorGUILayout.IntField("Seed", seed);

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
            try
            {
                chunks = Scatter(data);
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

            Debug.Log($"Grass Field Baker: {field.TotalInstances} instances in {field.ChunkCount} chunks.", field);
        }

        private GrassChunk[] Scatter(TerrainData data)
        {
            Vector3 origin = terrain.transform.position;
            Vector3 size = data.size;
            int alphaWidth = data.alphamapWidth;
            int alphaHeight = data.alphamapHeight;
            float[,,] alphamaps = data.GetAlphamaps(0, 0, alphaWidth, alphaHeight);

            float spacing = 1f / Mathf.Sqrt(grassType.Density);
            float threshold = grassType.MaskThreshold;
            float minScale = grassType.MinScale;
            float maxScale = grassType.MaxScale;

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
                    // consumes the same numbers. Repainting the mask then leaves other cells unchanged.
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
                    float weight = alphamaps[alphaZ, alphaX, terrainLayerIndex];

                    if (weight < threshold || keepRoll > weight)
                    {
                        continue;
                    }

                    Vector3 world = new Vector3(origin.x + localX, 0f, origin.z + localZ);
                    world.y = origin.y + terrain.SampleHeight(world);

                    Vector2Int key = new Vector2Int(Mathf.FloorToInt(localX / chunkSize), Mathf.FloorToInt(localZ / chunkSize));
                    if (!buckets.TryGetValue(key, out List<GrassInstance> list))
                    {
                        list = new List<GrassInstance>();
                        buckets.Add(key, list);
                    }

                    list.Add(new GrassInstance(world, yaw, scale));
                }
            }

            return BuildChunks(buckets);
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
    }
}
