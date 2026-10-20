using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// A Payout Streak bomb: a red warning circle appears on the ground, then something
    /// falls out of the sky onto it (seagull "bomb" or a giant wrecking ball) and explodes.
    /// Dodge the circle!
    /// </summary>
    public class FallingBomb : MonoBehaviour
    {
        const float FallTime = 0.6f, DropHeight = 30f;

        GameObject owner;
        Vector3 target;
        float delay, age;
        bool ball, cosmetic, exploded, whistled;
        Transform marker, bomb;

        public static void Spawn(GameObject owner, Vector3 target, float delay, bool ball, bool cosmetic)
        {
            var go = new GameObject(ball ? "WreckingBallDrop" : "GaryBomb");
            go.transform.position = target;
            var b = go.AddComponent<FallingBomb>();
            b.owner = owner;
            b.target = target;
            b.delay = delay;
            b.ball = ball;
            b.cosmetic = cosmetic;

            float radius = ball ? 7f : 3.5f;
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
            float radius = ball ? 7f : 3.5f;

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
                bomb.localPosition = new Vector3(0f, Mathf.Lerp(DropHeight, ball ? 2f : 0.4f, t * t), 0f);
                if (ball) bomb.Rotate(200f * Time.deltaTime, 0f, 90f * Time.deltaTime, Space.Self);
            }

            if (!exploded && age >= delay)
            {
                exploded = true;
                Vector3 p = target + Vector3.up * 0.5f;
                if (cosmetic) Fx.Explosion(p, radius);
                else if (ball) Weapons.Explode(p, radius, 26f, 30f, owner, "Wrecking Ball", 0.45f);
                else Weapons.Explode(p, radius, 15f, 14f, owner, "Gary Strike", 0.4f);
                Destroy(gameObject, ball ? 0.15f : 0.05f);
            }
        }
    }
}
