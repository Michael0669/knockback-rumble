using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Bounce Pad gadget: a trampoline on the floor. Anyone who steps on it (including the
    /// person who dropped it) gets flung high into the air. Every machine bounces its own
    /// dummies, so it needs no extra network messages.
    /// </summary>
    public class BouncePad : MonoBehaviour
    {
        const float Lifetime = 30f, Radius = 0.95f, Launch = 24f;

        ushort id;
        float age;
        Transform mat;
        readonly Dictionary<DentBody, float> lastBounce = new Dictionary<DentBody, float>();

        public static void Spawn(Contestant c, ushort id, Vector3 pos)
        {
            var go = new GameObject("BouncePad");
            go.transform.position = pos;
            WeaponModels.BouncePad(go.transform);
            var p = go.AddComponent<BouncePad>();
            p.id = id;
            p.mat = go.transform.Find("Mat");
            Gadgets.Register(id, go);
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age > Lifetime) { Gadgets.Unregister(id); Destroy(gameObject); return; }
            // Pops up out of the floor, wobbles when someone bounces.
            float grow = Mathf.Clamp01(age / 0.25f);
            transform.localScale = new Vector3(1f, grow, 1f);
            if (mat != null) mat.localScale = Vector3.Lerp(mat.localScale, new Vector3(1.35f, 0.03f, 1.35f), Time.deltaTime * 8f);
            if (age < 0.4f) return;

            Vector3 p = transform.position;
            foreach (var body in DentBody.All)
            {
                if (body == null || !body.IsLocal || body.Motor == null) continue;
                Vector3 feet = body.transform.position;
                Vector3 flat = new Vector3(feet.x - p.x, 0f, feet.z - p.z);
                if (flat.sqrMagnitude > Radius * Radius || feet.y < p.y - 0.3f || feet.y > p.y + 1.0f) continue;
                if (lastBounce.TryGetValue(body, out float t) && Time.time - t < 0.6f) continue;
                lastBounce[body] = Time.time;
                Vector3 v = body.Motor.Velocity;
                Vector3 push = Vector3.up * Launch + new Vector3(v.x, 0f, v.z) * 0.3f;
                if (v.y < 0f) push.y -= v.y; // cancel the fall so every bounce is the same height
                body.Motor.Launch(push, 0f);
                Sfx.At("spring", p, 1f, 0.1f, 40f);
                if (mat != null) mat.localScale = new Vector3(1.5f, 0.03f, 1.5f);
                Fx.Ring(p + Vector3.up * 0.25f, 1.6f, new Color(1f, 0.35f, 0.7f));
            }
        }
    }
}
