using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// The downloaded gun models (Guns/Pistol, Shotgun, SniperRifle). Each pack model comes in at
    /// its own size and angle, so the first time one is used we measure it: the long side is the
    /// barrel, the thin end is the muzzle, the tall side is up. Then it's turned to point down +Z
    /// (like the built-in guns), scaled to a sensible length, and the grip put in the hand.
    /// Weapons without a model (the rocket launcher) keep the built-in look.
    /// </summary>
    public static class GunModels
    {
        class Part
        {
            public Mesh mesh;
            public Material[] mats;
        }

        class Fit
        {
            public Quaternion rot;
            public float scale;
            public Vector3 pos, muzzle;
            public readonly List<Part> parts = new List<Part>();
        }

        static readonly Dictionary<string, Fit> fits = new Dictionary<string, Fit>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => fits.Clear();

        static string PathFor(WeaponDef def)
        {
            if (def == WeaponDef.PopPistol) return "Guns/Pistol";
            if (def == WeaponDef.BoomStick) return "Guns/Shotgun";
            if (def == WeaponDef.LongShot) return "Guns/SniperRifle";
            if (def == WeaponDef.BonkRifle) return "Guns/Rifle";
            if (def == WeaponDef.SprayNPray) return "Guns/P90";
            if (def == WeaponDef.HandCannon) return "Guns/Revolver";
            return null;
        }

        /// <summary>Overall length in meters, next to a 1.85 m character.</summary>
        static float LengthFor(WeaponDef def) =>
            def == WeaponDef.LongShot ? 1.05f : def == WeaponDef.BoomStick ? 0.85f : def == WeaponDef.BonkRifle ? 0.85f
          : def == WeaponDef.SprayNPray ? 0.55f : def == WeaponDef.HandCannon ? 0.38f : 0.32f;

        /// <summary>The gun in the hand. muzzle = barrel tip in parent space (null for the built-in models).</summary>
        public static GameObject Build(WeaponDef def, Transform parent, out Vector3? muzzle)
        {
            muzzle = null;
            string path = PathFor(def);
            var prefab = path != null ? Art.Prefab(path) : null;
            if (prefab == null) return WeaponModels.Build(def, parent);

            if (!fits.TryGetValue(path, out var fit))
            {
                fit = Measure(prefab, LengthFor(def));
                fits[path] = fit;
            }
            if (fit == null) return WeaponModels.Build(def, parent);

            var root = new GameObject("Weapon_" + def.id).transform;
            root.SetParent(parent, false);
            var adjust = new GameObject("Fit").transform;
            adjust.SetParent(root, false);
            adjust.localRotation = fit.rot;
            adjust.localScale = Vector3.one * fit.scale;
            adjust.localPosition = fit.pos;
            // Draw the exact meshes we measured (baked once, no skinning), so the size is always right.
            foreach (var part in fit.parts)
            {
                var go = new GameObject("Part");
                go.transform.SetParent(adjust, false);
                go.AddComponent<MeshFilter>().sharedMesh = part.mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = part.mats;
            }
            muzzle = fit.muzzle;
            return root.gameObject;
        }

        /// <summary>
        /// Bake every mesh of the model (in its rest pose) into plain meshes in one shared space, then work
        /// out which way it points and how big it is from exactly those vertices.
        /// </summary>
        static Fit Measure(GameObject prefab, float targetLength)
        {
            var temp = new GameObject("GunMeasure");
            var inst = Object.Instantiate(prefab, temp.transform, false);
            foreach (var a in inst.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            var fit = new Fit();
            var pts = new List<Vector3>();
            var toRoot = temp.transform.worldToLocalMatrix;
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                if (IsFlash(r)) continue;
                Mesh src;
                Matrix4x4 m;
                bool baked = false;
                if (r is SkinnedMeshRenderer smr)
                {
                    if (smr.sharedMesh == null) continue;
                    src = new Mesh();
                    smr.BakeMesh(src, true); // in the renderer's space, its scale already applied
                    m = toRoot * Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                    baked = true;
                }
                else
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    src = mf.sharedMesh;
                    m = toRoot * r.transform.localToWorldMatrix;
                }
                var verts = src.vertices;
                var normals = src.normals;
                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i] = m.MultiplyPoint3x4(verts[i]);
                    pts.Add(verts[i]);
                }
                for (int i = 0; i < normals.Length; i++) normals[i] = m.MultiplyVector(normals[i]).normalized;
                var mesh = new Mesh { name = prefab.name + "_" + r.gameObject.name };
                if (verts.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.vertices = verts;
                if (normals.Length == verts.Length) mesh.normals = normals;
                var uv = src.uv;
                if (uv != null && uv.Length == verts.Length) mesh.uv = uv;
                mesh.subMeshCount = src.subMeshCount;
                for (int sm = 0; sm < src.subMeshCount; sm++) mesh.SetTriangles(src.GetTriangles(sm), sm);
                if (normals.Length != verts.Length) mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                fit.parts.Add(new Part { mesh = mesh, mats = r.sharedMaterials });
                if (baked) Object.Destroy(src);
            }
            Object.Destroy(temp);
            if (pts.Count < 8 || fit.parts.Count == 0) return null;

            Vector3 min = pts[0], max = pts[0];
            foreach (var p in pts) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            Vector3 ext = max - min;
            int L = ext.x >= ext.y && ext.x >= ext.z ? 0 : ext.y >= ext.z ? 1 : 2;
            int a1 = (L + 1) % 3, a2 = (L + 2) % 3;
            float len = ext[L];
            if (len < 0.0001f) return null;

            // Cross-section at each end: the thinner end is the muzzle.
            Bounds lo = EndBounds(pts, L, min[L], min[L] + len * 0.25f), hi = EndBounds(pts, L, max[L] - len * 0.25f, max[L]);
            float crossLo = lo.size[a1] + lo.size[a2], crossHi = hi.size[a1] + hi.size[a2];
            bool muzzleHigh = crossHi < crossLo;
            Vector3 f = Axis(L) * (muzzleHigh ? 1f : -1f);
            Bounds front = muzzleHigh ? hi : lo;

            // Up: the taller of the other two sides; the barrel (front end) sits at the top.
            int U = ext[a1] >= ext[a2] ? a1 : a2;
            float mid = (min[U] + max[U]) * 0.5f;
            Vector3 u = Axis(U) * (front.center[U] >= mid ? 1f : -1f);

            fit.rot = Quaternion.Inverse(Quaternion.LookRotation(f, u));
            fit.scale = targetLength / len;
            Vector3 center = (min + max) * 0.5f;
            float height = ext[U];
            // Hold it a bit behind the middle and below the barrel (where a grip is).
            Vector3 grip = center - f * (len * 0.12f) - u * (height * 0.25f);
            fit.pos = -(fit.rot * (grip * fit.scale));
            Vector3 tip = center + f * (len * 0.5f);
            tip += Vector3.Scale(Axis(U), front.center - center);
            fit.muzzle = fit.rot * (tip * fit.scale) + fit.pos;
            return fit;
        }

        static bool IsFlash(Renderer r) => r.gameObject.name.StartsWith("Muzzle");

        static Vector3 Axis(int i) => i == 0 ? Vector3.right : i == 1 ? Vector3.up : Vector3.forward;

        static Bounds EndBounds(List<Vector3> pts, int axis, float a, float b)
        {
            bool any = false;
            var bounds = new Bounds();
            foreach (var p in pts)
            {
                if (p[axis] < a || p[axis] > b) continue;
                if (!any) { bounds = new Bounds(p, Vector3.zero); any = true; }
                else bounds.Encapsulate(p);
            }
            return bounds;
        }
    }
}
