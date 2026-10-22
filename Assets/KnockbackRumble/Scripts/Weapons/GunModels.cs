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
        class Fit
        {
            public Quaternion rot;
            public float scale;
            public Vector3 pos, muzzle;
        }

        static readonly Dictionary<string, Fit> fits = new Dictionary<string, Fit>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => fits.Clear();

        static string PathFor(WeaponDef def)
        {
            if (def == WeaponDef.PopPistol) return "Guns/Pistol";
            if (def == WeaponDef.BoomStick) return "Guns/Shotgun";
            if (def == WeaponDef.LongShot) return "Guns/SniperRifle";
            return null;
        }

        static float LengthFor(WeaponDef def) => def == WeaponDef.LongShot ? 1.3f : def == WeaponDef.BoomStick ? 1.0f : 0.42f;

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
            var inst = Object.Instantiate(prefab, adjust, false);
            Quiet(inst);
            muzzle = fit.muzzle;
            return root.gameObject;
        }

        /// <summary>No colliders, no animator (the rest pose is the "holding it" pose), no shadows' worth of fuss.</summary>
        static void Quiet(GameObject inst)
        {
            foreach (var a in inst.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.updateWhenOffscreen = true;
            // The pistol comes with its muzzle flash as a mesh: hide it (we have our own flashes).
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true)) if (IsFlash(r)) r.gameObject.SetActive(false);
        }

        static Fit Measure(GameObject prefab, float targetLength)
        {
            var temp = new GameObject("GunMeasure");
            var inst = Object.Instantiate(prefab, temp.transform, false);
            foreach (var a in inst.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            var pts = new List<Vector3>();
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                if (IsFlash(r)) continue;
                if (r is SkinnedMeshRenderer smr)
                {
                    if (smr.sharedMesh == null) continue;
                    var baked = new Mesh();
                    smr.BakeMesh(baked, true);
                    var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                    foreach (var v in baked.vertices) pts.Add(m.MultiplyPoint3x4(v));
                    Object.Destroy(baked);
                }
                else
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    var m = r.transform.localToWorldMatrix;
                    foreach (var v in mf.sharedMesh.vertices) pts.Add(m.MultiplyPoint3x4(v));
                }
            }
            Object.Destroy(temp);
            if (pts.Count < 8) return null;

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

            var fit = new Fit { rot = Quaternion.Inverse(Quaternion.LookRotation(f, u)), scale = targetLength / len };
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
