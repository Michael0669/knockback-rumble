using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KnockbackRumble
{
    /// <summary>
    /// Flat-shaded procedural meshes for the low-poly look: icospheres, cones, cylinders,
    /// a torus and lumpy rocks. Every mesh fits a 1x1x1 box centered on the origin
    /// (cylinders and cones run along Y, height 1, radius 0.5).
    /// In edit mode (when the scene builder runs) meshes are saved as assets in
    /// Assets/KnockbackRumble/Meshes so the saved scene keeps them.
    /// </summary>
    public static class LowPoly
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cache.Clear();

        public static Mesh Ico(int subdivisions = 1) => Get("Ico" + subdivisions, () => BuildIco(subdivisions, 0f, 0));
        public static Mesh Rock(int variant = 0) => Get("Rock" + variant, () => BuildIco(1, 0.22f, 17 + variant * 31));
        public static Mesh Cone(int sides = 8) => Get("Cone" + sides, () => BuildCone(sides));
        public static Mesh Cylinder(int sides = 10) => Get("Cyl" + sides, () => BuildCylinder(sides));
        public static Mesh Torus(int ring = 14, int tube = 6, float thickness = 0.3f) =>
            Get("Torus" + ring + "_" + tube + "_" + Mathf.RoundToInt(thickness * 100), () => BuildTorus(ring, tube, thickness));

        /// <summary>A visual-only object (no collider) with one of these meshes.</summary>
        public static GameObject Make(Mesh mesh, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat,
            Vector3? euler = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Like Make, plus a convex mesh collider so you can stand on it.</summary>
        public static GameObject MakeSolid(Mesh mesh, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat,
            Vector3? euler = null)
        {
            var go = Make(mesh, name, parent, localPos, scale, mat, euler);
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
            col.convex = true;
            return go;
        }

        // ---------------------------------------------------------------- cache / assets

        static Mesh Get(string key, Func<Mesh> build)
        {
            if (cache.TryGetValue(key, out var m) && m != null) return m;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                const string folder = "Assets/KnockbackRumble/Meshes";
                string path = folder + "/LP_" + key + ".asset";
                m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (m == null)
                {
                    if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/KnockbackRumble", "Meshes");
                    m = build();
                    AssetDatabase.CreateAsset(m, path);
                }
                cache[key] = m;
                return m;
            }
#endif
            m = build();
            cache[key] = m;
            return m;
        }

        /// <summary>One triangle list -> flat-shaded mesh. Triangles are flipped as needed to face away from "inside".</summary>
        static Mesh Flat(string name, List<Vector3> tris, Func<Vector3, Vector3> inside = null)
        {
            var verts = new List<Vector3>(tris.Count);
            var idx = new List<int>(tris.Count);
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                Vector3 a = tris[i], b = tris[i + 1], c = tris[i + 2];
                Vector3 n = Vector3.Cross(b - a, c - a);
                Vector3 centroid = (a + b + c) / 3f;
                Vector3 inner = inside != null ? inside(centroid) : Vector3.zero;
                if (Vector3.Dot(n, centroid - inner) < 0f) { var t = b; b = c; c = t; }
                verts.Add(a); verts.Add(b); verts.Add(c);
                idx.Add(verts.Count - 3); idx.Add(verts.Count - 2); idx.Add(verts.Count - 1);
            }
            var mesh = new Mesh { name = "LP_" + name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------------------------------------------------------- shapes

        static Mesh BuildIco(int subdivisions, float jitter, int seed)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            int[] f =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            var faces = new List<int>(f);
            for (int s = 0; s < subdivisions; s++)
            {
                var mids = new Dictionary<long, int>();
                var next = new List<int>();
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (mids.TryGetValue(key, out int i)) return i;
                    v.Add((v[a] + v[b]) * 0.5f);
                    mids[key] = v.Count - 1;
                    return v.Count - 1;
                }
                for (int i = 0; i < faces.Count; i += 3)
                {
                    int a = faces[i], b = faces[i + 1], c = faces[i + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                faces = next;
            }
            var rng = new System.Random(seed);
            var pts = new Vector3[v.Count];
            for (int i = 0; i < v.Count; i++)
            {
                float r = 0.5f * (1f + (jitter > 0f ? ((float)rng.NextDouble() * 2f - 1f) * jitter : 0f));
                pts[i] = v[i].normalized * r;
            }
            var tris = new List<Vector3>(faces.Count);
            foreach (int i in faces) tris.Add(pts[i]);
            return Flat("Ico", tris);
        }

        static Mesh BuildCone(int sides)
        {
            var tris = new List<Vector3>();
            var tip = new Vector3(0f, 0.5f, 0f);
            var baseC = new Vector3(0f, -0.5f, 0f);
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = Ring(i, sides, 0.5f, -0.5f), b = Ring(i + 1, sides, 0.5f, -0.5f);
                tris.Add(a); tris.Add(b); tris.Add(tip);
                tris.Add(baseC); tris.Add(b); tris.Add(a);
            }
            return Flat("Cone", tris, c => new Vector3(0f, c.y < -0.49f ? 0f : -0.25f, 0f));
        }

        static Mesh BuildCylinder(int sides)
        {
            var tris = new List<Vector3>();
            var top = new Vector3(0f, 0.5f, 0f);
            var bottom = new Vector3(0f, -0.5f, 0f);
            for (int i = 0; i < sides; i++)
            {
                Vector3 a0 = Ring(i, sides, 0.5f, -0.5f), b0 = Ring(i + 1, sides, 0.5f, -0.5f);
                Vector3 a1 = Ring(i, sides, 0.5f, 0.5f), b1 = Ring(i + 1, sides, 0.5f, 0.5f);
                tris.Add(a0); tris.Add(b0); tris.Add(b1);
                tris.Add(a0); tris.Add(b1); tris.Add(a1);
                tris.Add(top); tris.Add(a1); tris.Add(b1);
                tris.Add(bottom); tris.Add(b0); tris.Add(a0);
            }
            return Flat("Cylinder", tris, c => new Vector3(0f, Mathf.Abs(c.y) > 0.49f ? 0f : c.y, 0f));
        }

        static Mesh BuildTorus(int ring, int tube, float thickness)
        {
            float R = 0.5f - thickness * 0.5f, r = thickness * 0.5f;
            Vector3 P(int i, int j)
            {
                float u = i * Mathf.PI * 2f / ring, w = j * Mathf.PI * 2f / tube;
                float d = R + r * Mathf.Cos(w);
                return new Vector3(Mathf.Cos(u) * d, r * Mathf.Sin(w), Mathf.Sin(u) * d);
            }
            var tris = new List<Vector3>();
            for (int i = 0; i < ring; i++)
                for (int j = 0; j < tube; j++)
                {
                    Vector3 a = P(i, j), b = P(i + 1, j), c = P(i + 1, j + 1), d = P(i, j + 1);
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
            return Flat("Torus", tris, c =>
            {
                var flat = new Vector3(c.x, 0f, c.z);
                return flat.sqrMagnitude < 1e-6f ? Vector3.zero : flat.normalized * R;
            });
        }

        static Vector3 Ring(int i, int sides, float radius, float y)
        {
            float a = (i % sides) * Mathf.PI * 2f / sides;
            return new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
        }
    }
}
