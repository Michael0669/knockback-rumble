using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KnockbackRumble
{
    /// <summary>The shared low-poly color palette. Every greybox piece pulls from here.</summary>
    public static class Palette
    {
        public static readonly Color Floor = new Color(0.42f, 0.44f, 0.48f);
        public static readonly Color FloorDark = new Color(0.28f, 0.30f, 0.33f);
        public static readonly Color Belt = new Color(0.18f, 0.18f, 0.20f);
        public static readonly Color Stripe = new Color(0.95f, 0.78f, 0.10f);
        public static readonly Color Hazard = new Color(0.90f, 0.22f, 0.15f);
        public static readonly Color HazardWarn = new Color(1.00f, 0.55f, 0.10f);
        public static readonly Color Metal = new Color(0.55f, 0.58f, 0.62f);
        public static readonly Color Rust = new Color(0.55f, 0.30f, 0.16f);
        public static readonly Color Pit = new Color(0.25f, 0.20f, 0.15f);
        public static readonly Color Checkpoint = new Color(0.20f, 0.75f, 0.95f);
        public static readonly Color Gold = new Color(1.00f, 0.80f, 0.20f);
        public static readonly Color Shortcut = new Color(0.60f, 0.35f, 0.85f);
        public static readonly Color Bumper = new Color(0.95f, 0.35f, 0.65f);
        public static readonly Color DummyYellow = new Color(1.00f, 0.84f, 0.15f);
        public static readonly Color Black = new Color(0.08f, 0.08f, 0.09f);
        public static readonly Color White = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color Explosion = new Color(1.00f, 0.55f, 0.12f);
    }

    /// <summary>
    /// Creates and caches flat-colored URP materials. In edit mode (when the
    /// scene builder runs) they are saved as assets so the saved scene keeps them.
    /// </summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cache.Clear();

        public static Material Get(string name, Color color)
        {
            if (cache.TryGetValue(name, out var cached) && cached != null && IsUsable(cached))
                return cached;

            Material m;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                m = GetOrCreateAsset(name, color);
                cache[name] = m;
                return m;
            }
#endif
            m = Create(color);
            m.name = name;
            cache[name] = m;
            return m;
        }

        /// <summary>An unlit material for lines and trails.</summary>
        public static Material Unlit(string name, Color color)
        {
            string key = "unlit_" + name;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material m = shader != null ? new Material(shader) : Create(color);
            SetColor(m, color);
            m.name = key;
            cache[key] = m;
            return m;
        }

        static bool IsUsable(Material m)
        {
#if UNITY_EDITOR
            // In edit mode only asset materials can be saved into the scene.
            if (!Application.isPlaying) return AssetDatabase.Contains(m);
#endif
            return true;
        }

        static Material Create(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            Material m;
            if (shader != null)
            {
                m = new Material(shader);
            }
            else
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
                Object.DestroyImmediate(tmp);
            }
            SetColor(m, color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

#if UNITY_EDITOR
        const string Root = "Assets/KnockbackRumble";
        const string Folder = Root + "/Materials";

        static Material GetOrCreateAsset(string name, Color color)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Root, "Materials");
            string path = Folder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = Create(color);
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                SetColor(m, color);
                EditorUtility.SetDirty(m);
            }
            return m;
        }
#endif
    }

    /// <summary>Quick primitive builder used by the dummy, the level kit and FX.</summary>
    public static class Shapes
    {
        public static GameObject Make(PrimitiveType type, string name, Transform parent, Vector3 localPos,
            Vector3 scale, Material mat, bool keepCollider = true, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
