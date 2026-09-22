// EchoIcosphere
// Responsibility: Builds and caches one unit radius icosphere mesh shared by every echo shell. Its
// near uniform vertex density keeps the shader's vertex wobble even, where a UV sphere pinches at the
// poles. Built once on first use; three subdivisions give 1280 triangles.
using System.Collections.Generic;
using UnityEngine;

namespace Game.Echo
{
    public static class EchoIcosphere
    {
        private const int Subdivisions = 3;

        // Headroom for the shader's vertex wobble so the shell is never frustum culled early.
        private const float BoundsSize = 2.4f;

        private static Mesh mesh;

        public static Mesh Get()
        {
            if (mesh == null)
            {
                mesh = Build(Subdivisions);
            }

            return mesh;
        }

        private static Mesh Build(int subdivisions)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;

            List<Vector3> vertices = new List<Vector3>(642)
            {
                new Vector3(-1f, t, 0f), new Vector3(1f, t, 0f), new Vector3(-1f, -t, 0f), new Vector3(1f, -t, 0f),
                new Vector3(0f, -1f, t), new Vector3(0f, 1f, t), new Vector3(0f, -1f, -t), new Vector3(0f, 1f, -t),
                new Vector3(t, 0f, -1f), new Vector3(t, 0f, 1f), new Vector3(-t, 0f, -1f), new Vector3(-t, 0f, 1f)
            };

            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i].normalized;
            }

            List<int> triangles = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };

            Dictionary<long, int> midpointCache = new Dictionary<long, int>(512);

            for (int s = 0; s < subdivisions; s++)
            {
                List<int> next = new List<int>(triangles.Count * 4);

                for (int i = 0; i < triangles.Count; i += 3)
                {
                    int a = triangles[i];
                    int b = triangles[i + 1];
                    int c = triangles[i + 2];

                    int ab = Midpoint(a, b, vertices, midpointCache);
                    int bc = Midpoint(b, c, vertices, midpointCache);
                    int ca = Midpoint(c, a, vertices, midpointCache);

                    next.Add(a); next.Add(ab); next.Add(ca);
                    next.Add(b); next.Add(bc); next.Add(ab);
                    next.Add(c); next.Add(ca); next.Add(bc);
                    next.Add(ab); next.Add(bc); next.Add(ca);
                }

                triangles = next;
            }

            Mesh built = new Mesh
            {
                name = "EchoIcosphere",
                hideFlags = HideFlags.HideAndDontSave
            };

            built.SetVertices(vertices);
            built.SetTriangles(triangles, 0);
            built.SetNormals(vertices); // Unit sphere: each position is its own normal.
            built.bounds = new Bounds(Vector3.zero, Vector3.one * BoundsSize);
            return built;
        }

        private static int Midpoint(int a, int b, List<Vector3> vertices, Dictionary<long, int> cache)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int existing))
            {
                return existing;
            }

            vertices.Add(((vertices[a] + vertices[b]) * 0.5f).normalized);
            int index = vertices.Count - 1;
            cache.Add(key, index);
            return index;
        }
    }
}
