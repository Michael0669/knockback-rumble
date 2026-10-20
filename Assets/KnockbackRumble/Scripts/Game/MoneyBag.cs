using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// A Cash Grab money bag. Sits on the floor under a gold beam until someone touches
    /// it, then rides on their head paying $10 a second until they get hit. The host
    /// decides who has it (ShowManager); this only does the visuals.
    /// </summary>
    public class MoneyBag : MonoBehaviour
    {
        const float ArcTime = 0.55f;

        public int Index;
        public Contestant Carrier { get; private set; }
        public bool Carried { get; private set; }
        /// <summary>Who dropped it last and when (they can't grab it straight back).</summary>
        public Contestant LastDropper { get; private set; }
        public float DropTime { get; private set; } = -99f;
        /// <summary>On the ground and done bouncing, so it can be picked up.</summary>
        public bool Landed => !Carried && Time.time >= landAt;

        Transform model;
        GameObject beam;
        Vector3 from, to;
        float dropStart, landAt;

        public static MoneyBag Spawn(int index, Vector3 pos)
        {
            var go = new GameObject("MoneyBag_" + index);
            go.transform.position = pos;
            var b = go.AddComponent<MoneyBag>();
            b.Index = index;
            b.from = b.to = pos;

            var sack = Mats.Get("MoneyBag", new Color(0.42f, 0.62f, 0.3f));
            var gold = Mats.Get("Gold", Palette.Gold);
            var black = Mats.Get("Dummy_Black", Palette.Black);

            b.model = new GameObject("Model").transform;
            b.model.SetParent(go.transform, false);
            Shapes.Make(PrimitiveType.Sphere, "Sack", b.model, new Vector3(0f, 0.45f, 0f), new Vector3(0.95f, 0.85f, 0.95f), sack, false);
            Shapes.Make(PrimitiveType.Cylinder, "Tie", b.model, new Vector3(0f, 0.92f, 0f), new Vector3(0.32f, 0.07f, 0.32f), black, false);
            Shapes.Make(PrimitiveType.Sphere, "Tuft", b.model, new Vector3(0f, 1.05f, 0f), new Vector3(0.38f, 0.26f, 0.38f), sack, false);
            Dollar(b.model, new Vector3(0f, 0.45f, 0.49f), 0f, gold);
            Dollar(b.model, new Vector3(0f, 0.45f, -0.49f), 180f, gold);

            // A tall gold beam so you can spot loose bags from anywhere.
            b.beam = Shapes.Make(PrimitiveType.Cylinder, "Beam", go.transform, new Vector3(0f, 6f, 0f), new Vector3(0.12f, 6f, 0.12f),
                Mats.Unlit("BagBeam", Palette.Gold), false);

            var light = new GameObject("Glow").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            light.type = LightType.Point;
            light.color = Palette.Gold;
            light.range = 5f;
            light.intensity = 2f;
            return b;
        }

        static void Dollar(Transform parent, Vector3 pos, float yaw, Material mat)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return;
            var go = new GameObject("Dollar");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(0f, yaw + 180f, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = "$";
            tm.fontSize = 64;
            tm.characterSize = 0.09f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Palette.Gold;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        public void SetCarrier(Contestant c)
        {
            Carrier = c;
            Carried = c != null;
            if (c != null) Sfx.At("bag_grab", transform.position, 1f, 0.05f, 40f);
            if (beam != null) beam.SetActive(!Carried);
            if (c != null && c.IsPlayer) Announcer.ShowToast("YOU GOT A BAG!  $" + ShowManager.BagPerSecond + " a second. Don't get hit!");
        }

        /// <summary>Pop out of the carrier's hands and arc over to "target".</summary>
        public void Drop(Vector3 target, Contestant dropper)
        {
            if (Carrier != null && Carrier.IsPlayer) Announcer.ShowToast("BAG DROPPED!");
            if (Carried) Sfx.At("bag_drop", transform.position, 1f, 0.05f, 40f);
            from = transform.position;
            to = target;
            dropStart = Time.time;
            landAt = Time.time + ArcTime;
            LastDropper = dropper;
            DropTime = Time.time;
            Carrier = null;
            Carried = false;
            if (beam != null) beam.SetActive(true);
        }

        void LateUpdate()
        {
            if (Carried)
            {
                if (Carrier == null)
                {
                    // The carrier left the game. The host sends the bag back to a spawn spot.
                    Carried = false;
                    if (Net.IsHost && ShowManager.Instance != null) ShowManager.Instance.HostDropBag(this, null, true);
                    return;
                }
                transform.SetPositionAndRotation(Carrier.transform.position + Vector3.up * 2.3f, Carrier.transform.rotation);
                if (model != null) model.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.08f, 0f);
                return;
            }

            float t = landAt > dropStart ? Mathf.Clamp01((Time.time - dropStart) / ArcTime) : 1f;
            transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.5f;
            if (model != null && t >= 1f)
            {
                model.localPosition = new Vector3(0f, 0.15f + Mathf.Sin(Time.time * 2.5f) * 0.12f, 0f);
                model.Rotate(0f, 70f * Time.deltaTime, 0f, Space.Self);
            }
        }

        /// <summary>The floor under a point (ignores dummies and triggers). False if there's nothing below.</summary>
        public static bool TryGround(Vector3 from, out Vector3 point)
        {
            point = from;
            float best = float.MaxValue;
            bool found = false;
            foreach (var hit in Physics.RaycastAll(from, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<DentBody>() != null) continue;
                if (hit.distance < best) { best = hit.distance; point = hit.point; found = true; }
            }
            return found;
        }
    }
}
