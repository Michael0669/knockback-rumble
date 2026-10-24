using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// A Kill Streak bomb: a red warning circle appears on the ground, then something
    /// falls out of the sky onto it (seagull "bomb" or a giant wrecking ball) and explodes.
    /// Dodge the circle!
    /// </summary>
    public class FallingBomb : MonoBehaviour
    {
        const float FallTime = 0.6f, DropHeight = 30f;

        public enum Kind { Gary, Ball, Carpet, Anvil, Toilet }

        GameObject owner;
        Vector3 target;
        float delay, age;
        Kind kind;
        bool ball, cosmetic, exploded, whistled;
        Transform marker, bomb;

        float Radius => kind == Kind.Ball ? 7f : kind == Kind.Toilet ? 5.5f : kind == Kind.Anvil ? 3.2f : kind == Kind.Carpet ? 3f : 3.5f;

        public static void Spawn(GameObject owner, Vector3 target, float delay, Kind kind, bool cosmetic)
        {
            bool ball = kind == Kind.Ball;
            var go = new GameObject(kind + "Drop");
            go.transform.position = target;
            var b = go.AddComponent<FallingBomb>();
            b.owner = owner;
            b.target = target;
            b.delay = delay;
            b.ball = ball;
            b.kind = kind;
            b.cosmetic = cosmetic;

            float radius = b.Radius;
            b.marker = Shapes.Make(PrimitiveType.Cylinder, "Warning", go.transform, new Vector3(0f, 0.06f, 0f),
                new Vector3(radius * 2f, 0.02f, radius * 2f), Mats.Get("Hazard_Red", Palette.Hazard), false).transform;
            b.marker.localScale = new Vector3(0.1f, 0.02f, 0.1f);

            // Gary drops... what seagulls drop. The streak drops a giant spiked wrecking ball.
            b.bomb = new GameObject("Bomb").transform;
            b.bomb.SetParent(go.transform, false);
            b.bomb.localPosition = new Vector3(0f, DropHeight, 0f);
            if (ball)
            {
                b.bomb.localScale = Vector3.one * 4.5f;
                WeaponModels.SpikeBall(b.bomb, Mats.Get("SpikeBall_Body", new Color(0.22f, 0.22f, 0.25f)),
                    Mats.Get("Gun_Steel", new Color(0.55f, 0.57f, 0.62f)));
            }
            else if (kind == Kind.Anvil)
            {
                b.bomb.localScale = Vector3.one * 2.2f;
                b.bomb.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                WeaponModels.Anvil(b.bomb);
            }
            else if (kind == Kind.Toilet)
            {
                b.bomb.localScale = Vector3.one * 3.2f;
                b.bomb.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                WeaponModels.Toilet(b.bomb);
            }
            else if (kind == Kind.Carpet)
            {
                b.bomb.localScale = Vector3.one * 0.55f;
                WeaponModels.CartoonBomb(b.bomb, new Color(0.95f, 0.6f, 0.2f));
            }
            else
            {
                b.bomb.localScale = Vector3.one * 0.9f;
                WeaponModels.GaryBomb(b.bomb);
            }
            b.bomb.gameObject.SetActive(false);
        }

        void Update()
        {
            age += Time.deltaTime;
            float radius = Radius;

            // Warning circle grows in, then pulses.
            float grow = Mathf.Clamp01(age / 0.4f);
            float pulse = 1f + 0.06f * Mathf.Sin(age * 18f);
            if (marker != null) marker.localScale = new Vector3(radius * 2f * grow * pulse, 0.02f, radius * 2f * grow * pulse);

            float fallStart = delay - FallTime;
            if (age >= fallStart && bomb != null)
            {
                bomb.gameObject.SetActive(true);
                if (!whistled) { whistled = true; Sfx.At("bomb_whistle", target + Vector3.up * 6f, 1f, 0.1f, 80f); }
                float t = Mathf.Clamp01((age - fallStart) / FallTime);
                bomb.localPosition = new Vector3(0f, Mathf.Lerp(DropHeight, ball ? 2f : kind == Kind.Carpet ? 0.4f : kind == Kind.Gary ? 0.4f : 0f, t * t), 0f);
                if (ball) bomb.Rotate(200f * Time.deltaTime, 0f, 90f * Time.deltaTime, Space.Self);
            }

            if (!exploded && age >= delay)
            {
                exploded = true;
                Vector3 p = target + Vector3.up * 0.5f;
                // Anvils and toilets land with a clang/flush and stay a moment; everything else goes boom.
                bool heavy = kind == Kind.Anvil || kind == Kind.Toilet;
                if (kind == Kind.Anvil) Sfx.At("anvil", p, 1f, 0.05f, 90f);
                if (kind == Kind.Toilet) { Sfx.At("flush", p, 1f, 0.05f, 90f); Fx.Burst(p + Vector3.up, new Color(0.35f, 0.6f, 0.9f), 30, 10f, 0.35f); }
                if (cosmetic) Fx.Explosion(p, radius);
                else switch (kind)
                {
                    case Kind.Ball: Weapons.Explode(p, radius, 26f, 30f, owner, "Wrecking Ball", 0.45f); break;
                    case Kind.Anvil: Weapons.Explode(p, radius, 30f, 34f, owner, "Anvil Drop", 0.5f); break;
                    case Kind.Toilet: Weapons.Explode(p, radius, 27f, 30f, owner, "Tactical Toilet", 0.5f); break;
                    case Kind.Carpet: Weapons.Explode(p, radius, 13f, 12f, owner, "Carpet Bomb", 0.4f); break;
                    default: Weapons.Explode(p, radius, 15f, 14f, owner, "Gary Strike", 0.4f); break;
                }
                if (marker != null) marker.gameObject.SetActive(false);
                Destroy(gameObject, heavy ? 1.2f : ball ? 0.15f : 0.05f);
            }
        }
    }
}
