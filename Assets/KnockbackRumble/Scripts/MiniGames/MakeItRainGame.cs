using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// MAKE IT RAIN: coins, cash stacks and gold bars fall from the sky. Grab them. Bombs fall too.
    /// </summary>
    public class MakeItRainGame : CollectGame
    {
        const byte Coin = 0, Stack = 1, Bar = 2, Bomb = 3;
        const float FallTime = 1.3f, DropHeight = 18f, Interval = 0.3f;
        static readonly int[] Values = { 15, 35, 75, 0 };

        public override EventKind Kind => EventKind.MakeItRain;
        public override string Title => "MAKE IT RAIN";
        public override string Goal => "Grab the cash falling from the sky.";
        public override string Objective => "Coins, cash stacks and gold bars rain down. Grab as much as you can. Bombs fall too, so watch the shadows!";
        public override string Blurb => "Cash rains from the sky. So do bombs.";
        public override string[,] Cards => new[,]
        {
            { "$" + Values[Coin] + " - $" + Values[Bar], "coins, stacks, gold bars" },
            { "SHADOWS", "show where it lands" },
            { "BOMBS", "fall too. don't catch those" },
        };
        public override float Duration => 80f;
        public override string PrizeLabel => "MAKING IT RAIN";

        bool[] exploded;

        public override void Build(Transform root, CourseInfo info)
        {
            var floor = MiniKit.M("Rain_Floor", new Color(0.24f, 0.42f, 0.3f));
            var floorB = MiniKit.M("Rain_FloorB", new Color(0.3f, 0.5f, 0.36f));
            var gold = MiniKit.M("Gold", Palette.Gold);
            MiniKit.Block(root, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(34f, 1f, 34f), floor);
            for (int x = 0; x < 6; x++)
                for (int z = 0; z < 6; z++)
                    if ((x + z) % 2 == 0)
                        MiniKit.Block(root, "Tile", new Vector3(-14.1667f + x * 5.6667f, 0.004f, -14.1667f + z * 5.6667f), new Vector3(5.6f, 0.01f, 5.6f), floorB, false);
            MiniKit.Block(root, "Lip", new Vector3(0f, -0.12f, 0f), new Vector3(34.05f, 0.2f, 34.05f), gold, false);
            MiniKit.Block(root, "EdgeN", new Vector3(0f, 0.012f, 16.6f), new Vector3(34f, 0.01f, 0.8f), MiniKit.M("Stripe", Palette.Stripe), false);
            MiniKit.Block(root, "EdgeS", new Vector3(0f, 0.012f, -16.6f), new Vector3(34f, 0.01f, 0.8f), MiniKit.M("Stripe", Palette.Stripe), false);
            MiniKit.Block(root, "EdgeE", new Vector3(16.6f, 0.012f, 0f), new Vector3(0.8f, 0.01f, 34f), MiniKit.M("Stripe", Palette.Stripe), false);
            MiniKit.Block(root, "EdgeW", new Vector3(-16.6f, 0.012f, 0f), new Vector3(0.8f, 0.01f, 34f), MiniKit.M("Stripe", Palette.Stripe), false);
            for (int i = 0; i < 4; i++)
                Kit.IBeam(root, Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(-16f, -1.5f, -16f), Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(16f, -1.5f, -16f),
                    1f, Kit.Girder);

            // Money printers on the corners, spewing (decoration) and a few cash bales for cover
            var printer = MiniKit.M("Rain_Printer", new Color(0.3f, 0.3f, 0.34f));
            var cash = MiniKit.M("Cash_Green", new Color(0.35f, 0.7f, 0.35f));
            for (int i = 0; i < 4; i++)
            {
                var p = Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, 0f, 21f);
                MiniKit.Block(root, "Printer", p + new Vector3(0f, -1f, 0f), new Vector3(5f, 6f, 5f), printer, false, new Vector3(0f, 45f + i * 90f, 0f));
                MiniKit.Block(root, "PrinterSlot", p + new Vector3(0f, 1.2f, 0f), new Vector3(3.6f, 0.4f, 5.1f), cash, false, new Vector3(0f, 45f + i * 90f, 0f));
                MiniKit.Sign(root, "$$$", p + new Vector3(0f, 3f, 0f), 0.2f, new Color(0.6f, 1f, 0.6f));
            }
            for (int i = 0; i < 4; i++)
            {
                var p = Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(0f, 0.6f, 7.5f);
                MiniKit.Block(root, "Bale", p, new Vector3(2.4f, 1.2f, 1.2f), cash, true, new Vector3(0f, i * 90f + 90f, 0f));
            }
            MiniKit.Pickup(root, new Vector3(12f, 0f, 12f));
            MiniKit.Pickup(root, new Vector3(-12f, 0f, -12f));

            info.spawnPoints = MiniKit.TwoRings(Origin, 10f, 13f);
            info.center = Origin;
            info.radius = 15f;
            MiniKit.Dress(root, 18f, 505, "MAKE IT RAIN", "money really does grow on trees here. falling ones.", "QUANTITATIVE EASING", new Color(0.2f, 0.3f, 0.2f));
        }

        // ------------------------------------------------------------------ items

        protected override int ItemCount => Mathf.FloorToInt((Duration - 3f) / Interval);

        protected override Item MakeItem(int i)
        {
            float r = Rand(i, 1);
            byte type = r < 0.11f ? Bomb : r < 0.19f ? Bar : r < 0.36f ? Stack : Coin;
            // More bombs as the game goes on.
            if (type == Coin && Rand(i, 9) < Mathf.Clamp01(i / (float)ItemCount) * 0.1f) type = Bomb;
            var p = W((Rand(i, 2) * 2f - 1f) * 15f, 0f, (Rand(i, 3) * 2f - 1f) * 15f);
            return new Item
            {
                spawn = 1f + i * Interval + Rand(i, 4) * 0.25f,
                life = type == Bomb ? FallTime + 0.6f : FallTime + 8f,
                pos = p,
                type = type,
            };
        }

        public override void Begin()
        {
            base.Begin();
            exploded = new bool[items.Length];
        }

        float GroundY(int i) => Origin.y + (items[i].type == Coin ? 0.35f : items[i].type == Bar ? 0.18f : 0.3f);

        protected override Vector3 PositionAt(int i, float t, out bool collectable)
        {
            float u = Mathf.Clamp01((t - items[i].spawn) / FallTime);
            float ground = GroundY(i);
            float y = Mathf.Lerp(ground + DropHeight, ground, u * u);
            collectable = y < ground + 2.6f;
            return new Vector3(items[i].pos.x, y, items[i].pos.z);
        }

        protected override int Value(int i) => Values[items[i].type];
        protected override bool Grabbable(int i) => items[i].type != Bomb;

        protected override GameObject MakeView(int i, Transform parent)
        {
            var root = new GameObject("RainItem");
            root.transform.SetParent(parent, true);
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            switch (items[i].type)
            {
                case Coin:
                    LowPoly.Make(LowPoly.Cylinder(12), "Coin", model, Vector3.zero, new Vector3(0.7f, 0.12f, 0.7f), Mats.Get("Gold", Palette.Gold),
                        new Vector3(90f, 0f, 0f));
                    break;
                case Stack:
                    Shapes.Make(PrimitiveType.Cube, "Stack", model, Vector3.zero, new Vector3(0.9f, 0.45f, 0.5f), Mats.Get("Cash_Green", new Color(0.35f, 0.7f, 0.35f)), false);
                    Shapes.Make(PrimitiveType.Cube, "Band", model, Vector3.zero, new Vector3(0.18f, 0.47f, 0.52f), Mats.Get("Cash_Band", new Color(0.95f, 0.92f, 0.8f)), false);
                    break;
                case Bar:
                    LowPoly.Make(LowPoly.Cylinder(4), "Bar", model, Vector3.zero, new Vector3(1.1f, 0.32f, 0.55f), Mats.Unlit("Rain_GoldGlow", new Color(1f, 0.85f, 0.3f)),
                        new Vector3(0f, 45f, 0f));
                    break;
                default:
                    WeaponModels.CartoonBomb(model, new Color(0.9f, 0.15f, 0.15f));
                    model.localScale = Vector3.one * 0.9f;
                    break;
            }
            // A shadow on the ground under it, so you can see where it lands.
            var shadow = LowPoly.Make(LowPoly.Cylinder(16), "Shadow", root.transform, Vector3.zero, Vector3.one,
                Mats.Unlit(items[i].type == Bomb ? "Rain_ShadowBomb" : "Rain_Shadow", items[i].type == Bomb ? new Color(0.9f, 0.1f, 0.1f, 0.55f) : new Color(0f, 0f, 0f, 0.35f)));
            return root;
        }

        protected override void ItemTick(int i, float t, GameObject view)
        {
            var model = view.transform.Find("Model");
            if (model != null) model.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
            var shadow = view.transform.Find("Shadow");
            float u = Mathf.Clamp01((t - items[i].spawn) / FallTime);
            if (shadow != null)
            {
                shadow.position = new Vector3(view.transform.position.x, Origin.y + 0.02f, view.transform.position.z);
                float s = Mathf.Lerp(0.4f, 1.1f, u) * (items[i].type == Bomb ? 2.4f : 1f);
                shadow.localScale = new Vector3(s, 0.02f, s);
                shadow.gameObject.SetActive(u < 1f || items[i].type == Bomb);
            }

            if (items[i].type == Bomb && u >= 1f && !exploded[i])
            {
                exploded[i] = true;
                Vector3 p = view.transform.position;
                MiniKit.BoomFx(p, 2.6f);
                MiniKit.Blast(p, 3.4f, 17f, 10f, "Bomb");
                taken[i] = true;
            }
        }

        protected override void OnGrabbed(int i, Vector3 at, Contestant by)
        {
            Sfx.At(items[i].type == Bar ? "cash" : "coin", at, 0.9f, 0.1f, 30f);
            for (int k = 0; k < (items[i].type == Bar ? 10 : 5); k++)
                Puffs.Spark(at, Random.onUnitSphere * 4f + Vector3.up * 3f, 0.12f, 0.4f, new Color(1f, 0.85f, 0.3f));
        }

        // ------------------------------------------------------------------ bots

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            // Dodge bombs about to land nearby.
            if (items != null)
            {
                float t = Clock;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].spawn > t) break;
                    if (items[i].type != Bomb || exploded == null || exploded[i]) continue;
                    float left = items[i].spawn + FallTime - t;
                    if (left < 0f || left > 1.2f) continue;
                    Vector3 away = Shapes.Flat(pos - items[i].pos);
                    if (away.magnitude > 3.6f) continue;
                    if (Random.value > skill + 0.3f) break; // not everyone notices
                    return pos + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.right) * 5f;
                }
            }
            return NearestItem(pos, 3f);
        }
    }
}
