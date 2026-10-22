using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// The live 3D dummy on the CUSTOMIZE screen: a dummy on a little turntable far below
    /// the world, filmed by its own camera into a texture the menu draws. Drag to spin it.
    /// </summary>
    public class CustomizePreview : MonoBehaviour
    {
        static CustomizePreview instance;
        static readonly Vector3 Stage = new Vector3(0f, -600f, 0f);

        public RenderTexture Texture { get; private set; }
        Camera cam;
        DummyVisual visual;
        Transform turntable;
        Color shownColor = new Color(-1f, 0f, 0f);
        int shownCharacter = -1;
        float yaw = 200f, spinVelocity;

        public static CustomizePreview Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("CustomizePreview");
            instance = go.AddComponent<CustomizePreview>();
            instance.Setup();
            return instance;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (Texture != null) Texture.Release();
        }

        void Setup()
        {
            transform.position = Stage;
            Texture = new RenderTexture(600, 800, 24) { name = "CustomizePreview", antiAliasing = 4 };

            turntable = new GameObject("Turntable").transform;
            turntable.SetParent(transform, false);
            LowPoly.Make(LowPoly.Cylinder(20), "Disc", turntable, new Vector3(0f, -0.1f, 0f), new Vector3(1.8f, 0.2f, 1.8f),
                Mats.Get("Preview_Disc", new Color(0.25f, 0.2f, 0.35f)));
            LowPoly.Make(LowPoly.Torus(24, 4, 0.08f), "Rim", turntable, new Vector3(0f, -0.02f, 0f), new Vector3(1.9f, 0.3f, 1.9f),
                Mats.Unlit("Preview_Rim", new Color(1f, 0.79f, 0.16f)));

            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.transform.SetParent(transform, false);
            key.transform.localPosition = new Vector3(1.5f, 2.5f, 2.5f);
            key.type = LightType.Point;
            key.range = 8f;
            key.intensity = 2.5f;
            key.color = new Color(1f, 0.95f, 0.88f);

            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.15f, 3.6f);
            camGo.transform.LookAt(Stage + new Vector3(0f, 0.95f, 0f));
            cam = camGo.AddComponent<Camera>();
            cam.targetTexture = Texture;
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.1f, 0.18f, 1f);
            cam.enabled = false;
        }

        /// <summary>Show this character in this color (call every frame the screen is open).</summary>
        public void ShowCharacter(int character, Color color)
        {
            if (visual == null || color != shownColor)
            {
                if (visual != null) Destroy(visual.gameObject);
                var go = new GameObject("PreviewDummy");
                go.transform.SetParent(turntable, false);
                visual = go.AddComponent<DummyVisual>();
                visual.Build(color, character);
                shownColor = color;
                shownCharacter = character;
            }
            else if (character != shownCharacter)
            {
                visual.SetCharacter(character);
                shownCharacter = character;
                Puffs.SmokeBurst(turntable.position + Vector3.up, 6, 1.2f, 0.5f, 0.5f);
            }
            cam.enabled = true;
        }

        /// <summary>Show this outfit on a dummy of this color (call every frame the screen is open).</summary>
        public void Show(Outfit outfit, Color color)
        {
            if (visual == null || color != shownColor)
            {
                if (visual != null) Destroy(visual.gameObject);
                var go = new GameObject("PreviewDummy");
                go.transform.SetParent(turntable, false);
                visual = go.AddComponent<DummyVisual>();
                visual.Build(color);
                shownColor = color;
                visual.ApplyOutfit(outfit);
            }
            else if (!SameOutfit(visual.CurrentOutfit, outfit))
            {
                visual.ApplyOutfit(outfit);
            }
            cam.enabled = true;
        }

        public void Hide()
        {
            if (cam != null) cam.enabled = false;
        }

        /// <summary>Stop rendering the preview if it exists (cheap to call every frame).</summary>
        public static void HideIfAny()
        {
            if (instance != null) instance.Hide();
        }

        /// <summary>Spin the turntable (drag on the preview).</summary>
        public void Spin(float degrees) => spinVelocity = degrees / Mathf.Max(Time.unscaledDeltaTime, 0.001f) * 0.2f + spinVelocity * 0.8f;

        static bool SameOutfit(Outfit a, Outfit b)
        {
            for (int i = 0; i < Outfit.SlotCount; i++) if (a.ids[i] != b.ids[i]) return false;
            return true;
        }

        void Update()
        {
            if (cam == null || !cam.enabled) return;
            spinVelocity = Mathf.MoveTowards(spinVelocity, 25f, Time.unscaledDeltaTime * 200f);
            yaw += spinVelocity * Time.unscaledDeltaTime;
            turntable.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
