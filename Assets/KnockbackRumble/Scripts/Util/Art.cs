using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// The downloaded art (Assets/ThirdParty/Resources/...): characters, guns, props and vehicles.
    /// Everything that uses it falls back to the built-in low-poly look if a model is missing,
    /// or if UseThirdParty is switched off.
    /// </summary>
    public static class Art
    {
        /// <summary>Switch to false to go back to the built-in placeholder art everywhere.</summary>
        public static bool UseThirdParty = true;

        static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
        static readonly HashSet<string> missing = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            cache.Clear();
            missing.Clear();
        }

        /// <summary>A model from Assets/ThirdParty/Resources, e.g. "Environment/Barrel" (null if missing or switched off).</summary>
        public static GameObject Prefab(string path)
        {
            if (!UseThirdParty || string.IsNullOrEmpty(path) || missing.Contains(path)) return null;
            if (cache.TryGetValue(path, out var p) && p != null) return p;
            p = Resources.Load<GameObject>(path);
            if (p == null)
            {
                missing.Add(path);
                Debug.LogWarning("[Knockback Rumble] Missing model Resources/" + path + " (using the built-in look instead)");
                return null;
            }
            cache[path] = p;
            return p;
        }

        /// <summary>Bounds of every mesh under root, in root's local space (bind pose for skinned meshes).</summary>
        public static Bounds LocalBounds(Transform root)
        {
            bool any = false;
            var b = new Bounds();
            var toRoot = root.worldToLocalMatrix;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh = null;
                if (r is SkinnedMeshRenderer smr) mesh = smr.sharedMesh;
                else
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null) mesh = mf.sharedMesh;
                }
                if (mesh == null || !r.gameObject.activeSelf) continue;
                var mb = mesh.bounds;
                var m = toRoot * r.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    var w = m.MultiplyPoint3x4(c);
                    if (!any) { b = new Bounds(w, Vector3.zero); any = true; }
                    else b.Encapsulate(w);
                }
            }
            return b;
        }

        public enum Fit { Height, Length, Largest }

        /// <summary>
        /// Place a prop: scaled uniformly so its height (or length, or largest side) is "size",
        /// sitting on localPos (bottom center), turned by yaw. Visual only (no colliders).
        /// Returns null if the model is missing (callers then build the old placeholder).
        /// </summary>
        public static GameObject Prop(string path, Transform parent, Vector3 localPos, float yaw, float size, Fit fit = Fit.Height)
        {
            var prefab = Prefab(path);
            if (prefab == null) return null;
            var holder = new GameObject(prefab.name).transform;
            if (parent != null) holder.SetParent(parent, false);
            holder.localPosition = localPos;
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var inst = Object.Instantiate(prefab, holder, false);
            inst.name = prefab.name + "_Model";
            foreach (var col in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            var b = LocalBounds(holder);
            float measure = fit == Fit.Height ? b.size.y : fit == Fit.Length ? Mathf.Max(b.size.x, b.size.z) : Mathf.Max(b.size.x, b.size.y, b.size.z);
            float s = measure > 0.0001f ? size / measure : 1f;
            inst.transform.localScale *= s;
            // Bottom center on the spot.
            inst.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z) * s;
            return holder.gameObject;
        }

        /// <summary>
        /// Place a prop stretched to fill a box (size = width, height, length), its long side along local Z,
        /// sitting on localPos, turned by yaw. Used where the old placeholder's size matters (containers you stand on).
        /// </summary>
        public static GameObject PropBox(string path, Transform parent, Vector3 localPos, float yaw, Vector3 size)
        {
            var prefab = Prefab(path);
            if (prefab == null) return null;
            var holder = new GameObject(prefab.name).transform;
            if (parent != null) holder.SetParent(parent, false);
            holder.localPosition = localPos;
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var turn = new GameObject("Turn").transform;
            turn.SetParent(holder, false);
            var inst = Object.Instantiate(prefab, turn, false);
            inst.name = prefab.name + "_Model";
            foreach (var col in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            var b = LocalBounds(turn);
            if (b.size.x > b.size.z) turn.localRotation = Quaternion.Euler(0f, 90f, 0f);
            b = LocalBounds(holder);
            var scale = new Vector3(size.x / Mathf.Max(0.0001f, b.size.x), size.y / Mathf.Max(0.0001f, b.size.y), size.z / Mathf.Max(0.0001f, b.size.z));
            var fit = new GameObject("Fit").transform;
            fit.SetParent(holder, false);
            turn.SetParent(fit, false);
            fit.localScale = scale;
            fit.localPosition = -Vector3.Scale(new Vector3(b.center.x, b.min.y, b.center.z), scale);
            return holder.gameObject;
        }
    }
}
