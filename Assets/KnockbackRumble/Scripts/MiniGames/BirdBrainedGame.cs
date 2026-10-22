using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// BIRD BRAINED: Gary the seagull and his mates bomb the stage. A red circle shows where each
    /// "present" will land; get out of it. The drops come from the round's seed and the shared
    /// clock, so they land at the same time and place everywhere. One life.
    /// </summary>
    public class BirdBrainedGame : MiniGame
    {
        const float Half = 14f, Warn = 1.6f, FallTime = 0.7f, BlastR = 2.9f, DropY = 13f;

        struct Drop
        {
            public float impact;
            public Vector3 pos;
        }

        class DropView
        {
            public Transform marker, fill, bomb, splat;
            public bool exploded;
        }

        public override EventKind Kind => EventKind.BirdBrained;
        public override string Title => "BIRD BRAINED";
        public override string Goal => "Dodge the seagull bombs.";
        public override string Objective => "Gary and his mates are dropping 'presents' on the stage. Red circles show where. Get out of them! Fall off and you're out.";
        public override string Blurb => "Seagulls bomb the stage. Stay out of the red circles.";
        public override string[,] Cards => new[,]
        {
            { "RED CIRCLE", "= incoming. move!" },
            { "FALL = OUT", "one life only" },
            { "$5 / SEC", "while you're still in" },
        };
        public override float Duration => 100f;
        public override bool FallsEliminate => true;
        public override bool WeaponsAllowed => false;
        public override int StandingPerSecond => 5;

        Drop[] drops;
        DropView[] views;
        int first;
        float nextGull;

        public override void Build(Transform root, CourseInfo info)
        {
            var boards = MiniKit.M("Bird_Boards", new Color(0.62f, 0.48f, 0.32f));
            var boardsB = MiniKit.M("Bird_BoardsB", new Color(0.55f, 0.42f, 0.28f));
            MiniKit.Block(root, "Pier", new Vector3(0f, -0.5f, 0f), new Vector3(Half * 2f, 1f, Half * 2f), boards);
            for (int i = 0; i < 14; i++)
                if (i % 2 == 0)
                    MiniKit.Block(root, "Plank", new Vector3(-Half + (i + 0.5f) * 2f, 0.006f, 0f), new Vector3(1.9f, 0.01f, Half * 2f), boardsB, false);
            // Pier posts down into the sea, and the sea
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    LowPoly.Make(LowPoly.Cylinder(10), "Post", root, new Vector3(x * (Half - 1f), -7f, z * (Half - 1f)), new Vector3(1.2f, 14f, 1.2f), boardsB);
            MiniKit.Block(root, "Sea", new Vector3(0f, -10f, 0f), new Vector3(400f, 1f, 400f), Mats.Unlit("Bird_Sea", new Color(0.18f, 0.45f, 0.62f)), false);
            // Some crates and a fish stall for cover
            var crate = MiniKit.M("Potato_Crate", new Color(0.55f, 0.38f, 0.2f));
            Vector3[] crates = { new Vector3(-7f, 0.6f, 6f), new Vector3(8f, 0.6f, -5f), new Vector3(3f, 0.6f, 9f), new Vector3(-9f, 0.6f, -8f) };
            foreach (var c in crates) MiniKit.Block(root, "Crate", c, new Vector3(1.2f, 1.2f, 1.2f), crate, true, new Vector3(0f, c.x * 7f, 0f));
            MiniKit.Block(root, "FishStall", new Vector3(0f, 1f, Half + 2f), new Vector3(6f, 2f, 2f), MiniKit.M("Bird_Stall", new Color(0.85f, 0.3f, 0.3f)), false);
            MiniKit.Sign(root, "FRESH FISH (ish)", new Vector3(0f, 2.6f, Half + 0.95f), 0.12f, Color.white);
            // A lighthouse in the distance
            LowPoly.Make(LowPoly.Cylinder(12), "Lighthouse", root, new Vector3(-40f, 6f, 55f), new Vector3(6f, 32f, 6f), MiniKit.M("White", Palette.White));
            LowPoly.Make(LowPoly.Cylinder(12), "LighthouseBand", root, new Vector3(-40f, 10f, 55f), new Vector3(6.1f, 4f, 6.1f), MiniKit.M("Hazard_Red", Palette.Hazard));
            LowPoly.Make(LowPoly.Cone(12), "LighthouseTop", root, new Vector3(-40f, 24f, 55f), new Vector3(5f, 4f, 5f), MiniKit.M("Hazard_Red", Palette.Hazard));

            info.spawnPoints = MiniKit.TwoRings(Origin, 5f, 9.5f);
            info.center = Origin;
            info.radius = Half;
            info.killY = Origin.y - 8f;
            MiniKit.Lounge(root, info, new Vector3(0f, 8f, -24f), 180f);
            MiniKit.Dress(root, 18f, 1414, "BIRD BRAINED", "Gary has been eating chips again", "GARY'S CHIPPY", new Color(0.18f, 0.45f, 0.62f));
        }

        public override void Begin()
        {
            var list = new List<Drop>();
            for (int w = 0; ; w++)
            {
                float t = 3f + w * 1.7f;
                if (t + Warn > Duration) break;
                int n = 1 + Mathf.Min(5, w / 4);
                for (int j = 0; j < n; j++)
                {
                    int key = w * 16 + j;
                    list.Add(new Drop
                    {
                        impact = t + Warn + j * 0.12f,
                        pos = W((Rand(key, 1) * 2f - 1f) * (Half - 1.5f), 0f, (Rand(key, 2) * 2f - 1f) * (Half - 1.5f)),
                    });
                }
            }
            list.Sort((a, b) => a.impact.CompareTo(b.impact));
            drops = list.ToArray();
            views = new DropView[drops.Length];
            first = 0;
            nextGull = 0f;
        }

        public override void End()
        {
            if (views != null)
                foreach (var v in views) Kill(v);
            views = null;
        }

        static void Kill(DropView v)
        {
            if (v == null) return;
            if (v.marker != null) Object.Destroy(v.marker.gameObject);
            if (v.bomb != null) Object.Destroy(v.bomb.gameObject);
            if (v.splat != null) Object.Destroy(v.splat.gameObject);
        }

        DropView MakeView(int i)
        {
            var v = new DropView();
            var red = Mats.Unlit("Bird_Marker", new Color(1f, 0.15f, 0.1f));
            v.marker = MiniKit.FlatRing(Course.transform, "Marker", Vector3.zero, BlastR, red, 0.1f).transform;
            v.marker.position = drops[i].pos + Vector3.up * 0.03f;
            v.fill = LowPoly.Make(LowPoly.Cylinder(20), "Fill", v.marker, Vector3.zero, Vector3.zero, Mats.Unlit("Bird_MarkerFill", new Color(1f, 0.2f, 0.1f, 0.35f))).transform;
            v.bomb = new GameObject("GullBomb").transform;
            v.bomb.SetParent(Course.transform, true);
            WeaponModels.GaryBomb(v.bomb);
            v.bomb.localScale = Vector3.one * 0.9f;
            v.bomb.gameObject.SetActive(false);
            return v;
        }

        public override void Tick()
        {
            if (drops == null) return;
            float t = Clock;
            if (Racing && Time.time >= nextGull)
            {
                nextGull = Time.time + 7f;
                FlyBy.Spawn(Origin);
                Sfx.Ui("seagull", 0.7f);
            }
            while (first < drops.Length && t > drops[first].impact + 6f)
            {
                Kill(views[first]);
                views[first] = null;
                first++;
            }
            for (int i = first; i < drops.Length; i++)
            {
                var d = drops[i];
                if (t < d.impact - Warn) break;
                if (views[i] == null) views[i] = MakeView(i);
                var v = views[i];
                float left = d.impact - t;
                if (left > 0f)
                {
                    // Warning: the circle fills up; the bomb drops for the last bit.
                    float fill = 1f - left / Warn;
                    float pulse = 1f + 0.05f * Mathf.Sin(Time.time * 18f);
                    if (v.fill != null) v.fill.localScale = new Vector3(fill * pulse, 0.05f, fill * pulse);
                    if (v.bomb != null)
                    {
                        bool falling = left < FallTime;
                        if (v.bomb.gameObject.activeSelf != falling) v.bomb.gameObject.SetActive(falling);
                        if (falling) v.bomb.position = d.pos + Vector3.up * (DropY * (left / FallTime) * (left / FallTime) + 0.3f);
                    }
                }
                else if (!v.exploded)
                {
                    v.exploded = true;
                    if (v.marker != null) v.marker.gameObject.SetActive(false);
                    if (v.bomb != null) v.bomb.gameObject.SetActive(false);
                    Fx.Explosion(d.pos + Vector3.up * 0.3f, 1.8f);
                    Sfx.At("splat", d.pos, 1f, 0.15f, 50f);
                    MiniKit.Blast(d.pos, BlastR, 16f, 12f, "Gary's Present");
                    // A white splat stays on the boards for a bit.
                    v.splat = LowPoly.Make(LowPoly.Rock(i % 3), "Splat", Course.transform, Vector3.zero, new Vector3(2.4f, 0.05f, 2f),
                        Mats.Get("Bird_Splat", new Color(0.96f, 0.96f, 0.9f)), new Vector3(0f, i * 37f, 0f)).transform;
                    v.splat.position = d.pos + Vector3.up * 0.02f;
                }
            }
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            if (drops != null)
            {
                float t = Clock;
                float notice = Mathf.Lerp(0.6f, Warn, skill);
                for (int i = first; i < drops.Length; i++)
                {
                    float left = drops[i].impact - t;
                    if (left > Warn) break;
                    if (left < 0f || left > notice) continue;
                    Vector3 away = Shapes.Flat(pos - drops[i].pos);
                    if (away.magnitude > BlastR + 1f) continue;
                    Vector3 goal = pos + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.right) * 4.5f;
                    // ...but not off the pier.
                    Vector3 off = Shapes.Flat(goal - Origin);
                    if (Mathf.Abs(off.x) > Half - 2f || Mathf.Abs(off.z) > Half - 2f) goal = Origin + off * 0.5f;
                    return goal;
                }
            }
            int bucket = Mathf.FloorToInt(Time.time / 2.5f) + me.NetId * 11;
            float a = (bucket * 2.399f) % (Mathf.PI * 2f);
            return Origin + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (3f + 6f * Mathf.Repeat(bucket * 0.618f, 1f));
        }
    }
}
