using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Big floating text over a dummy's head ("BOOOO!"), rising and fading. Always faces the camera.</summary>
    public class HecklePopup : MonoBehaviour
    {
        const float Life = 1.6f;
        Transform target;
        TextMesh text;
        Color color;
        float age, offset;

        public static void Spawn(Contestant target, string message, Color color)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null || target == null) return;
            var go = new GameObject("Heckle");
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = message;
            tm.fontSize = 64;
            tm.characterSize = 0.07f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            var p = go.AddComponent<HecklePopup>();
            p.target = target.transform;
            p.text = tm;
            p.color = color;
            p.offset = Random.Range(-0.4f, 0.4f);
            p.LateUpdate();
        }

        void LateUpdate()
        {
            age += Time.deltaTime;
            if (age >= Life || target == null) { Destroy(gameObject); return; }
            float t = age / Life;
            transform.position = target.position + new Vector3(offset, 2.6f + t * 1.2f, 0f);
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            float pop = t < 0.12f ? Mathf.Lerp(0.3f, 1.25f, t / 0.12f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.12f) * 5f));
            transform.localScale = Vector3.one * pop;
            var c = color;
            c.a = 1f - Mathf.Clamp01((t - 0.65f) / 0.35f);
            text.color = c;
        }
    }
}
