using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KnockbackRumble
{
    /// <summary>
    /// The game's overall look, applied when Play starts: a warm late-afternoon sun with soft
    /// shadows, three-color ambient light, distance fog, a gradient sky dome, and a
    /// post-processing stack (bloom so lamps and fire glow, punchier color, a light vignette).
    /// Tweak the colors here to change the mood of every arena at once.
    /// </summary>
    public class WorldLook : MonoBehaviour
    {
        public static readonly Color SkyTop = new Color(0.32f, 0.55f, 0.9f);
        public static readonly Color SkyHorizon = new Color(0.98f, 0.78f, 0.6f);
        public static readonly Color SkyBottom = new Color(0.55f, 0.45f, 0.45f);

        Transform dome;
        Camera cam;

        public static void Apply(Camera camera)
        {
            var go = new GameObject("WorldLook");
            var look = go.AddComponent<WorldLook>();
            look.cam = camera;
            look.Setup();
        }

        void Setup()
        {
            // Sun
            Light sun = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(42f, -40f, 0f);
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;

            // Ambient and fog
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.7f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.55f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.26f, 0.24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SkyHorizon;
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 420f;

            // Camera and sky
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = SkyHorizon;
                cam.farClipPlane = 1200f;
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null)
                {
                    data.renderPostProcessing = true;
                    data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                }
            }
            BuildDome();
            BuildVolume();
        }

        void BuildDome()
        {
            // An inside-out icosphere with vertex colors: blue overhead, warm at the horizon.
            var src = LowPoly.Ico(3);
            var mesh = Instantiate(src);
            var v = mesh.vertices;
            var colors = new Color[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                float y = v[i].y * 2f; // -1..1
                colors[i] = y >= 0f
                    ? Color.Lerp(SkyHorizon, SkyTop, Mathf.Pow(Mathf.Clamp01(y * 1.4f), 0.7f))
                    : Color.Lerp(SkyHorizon, SkyBottom, Mathf.Clamp01(-y * 2f));
            }
            mesh.colors = colors;
            var tris = mesh.triangles;
            for (int i = 0; i < tris.Length; i += 3) { int t = tris[i]; tris[i] = tris[i + 1]; tris[i + 1] = t; }
            mesh.triangles = tris;

            var go = new GameObject("SkyDome");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            // Drawn first (background queue) so everything else, including glowing particles, draws over it.
            var mat = new Material(Mats.Unlit("SkyDome", Color.white)) { renderQueue = 1000 };
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.transform.localScale = Vector3.one * 2000f;
            dome = go.transform;
        }

        void BuildVolume()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.65f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(18f);
            color.contrast.Override(12f);
            color.postExposure.Override(0.15f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.5f);

            var vol = gameObject.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            vol.sharedProfile = profile;
        }

        void LateUpdate()
        {
            var c = cam != null ? cam : Camera.main;
            if (dome != null && c != null) dome.position = c.transform.position;
        }
    }
}
