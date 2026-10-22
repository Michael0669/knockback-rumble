using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// BALLOON POP: a giant bounce house full of trampolines. Balloons float up everywhere; touch
    /// one to pop it for cash. The high ones are only reachable off the trampolines.
    /// </summary>
    public class BalloonPopGame : CollectGame
    {
        const float Half = 16f, TrampR = 1.9f, TrampTop = 0.35f, Bounce = 17f, Interval = 0.32f;
        const int NormalCash = 15, GoldCash = 60;

        public override EventKind Kind => EventKind.BalloonPop;
        public override string Title => "BALLOON POP";
        public override string Goal => "Pop the balloons. Bounce to reach the high ones.";
        public override string Objective => "Balloons float up all over the bounce house. Touch them to pop them for cash. Trampolines launch you up to the high ones!";
        public override string Blurb => "Bounce on trampolines and pop balloons for cash.";
        public override string[,] Cards => new[,]
        {
            { "$" + NormalCash, "per balloon" },
            { "$" + GoldCash, "gold balloons" },
            { "TRAMPOLINES", "launch you up high" },
        };
        public override float Duration => 80f;
        public override string PrizeLabel => "POP!";
        protected override float GrabRadius => 1.35f;

        static readonly Vector3[] Tramps =
        {
            new Vector3(-10f, 0f, -10f), new Vector3(0f, 0f, -10f), new Vector3(10f, 0f, -10f),
            new Vector3(-10f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f),
            new Vector3(-10f, 0f, 10f), new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 10f),
        };
        static readonly Color[] BalloonColors =
        {
            new Color(1f, 0.3f, 0.35f), new Color(0.3f, 0.7f, 1f), new Color(0.45f, 0.9f, 0.4f), new Color(0.85f, 0.45f, 1f), new Color(1f, 0.6f, 0.2f),
        };

        Transform[] trampMats;
        float[] trampSquash;
        readonly HitCooldown bounceCooldown = new HitCooldown();

        public override void Build(Transform root, CourseInfo info)
        {
            var floorA = MiniKit.M("Bounce_FloorA", new Color(0.95f, 0.45f, 0.55f));
            var floorB = MiniKit.M("Bounce_FloorB", new Color(0.45f, 0.75f, 0.95f));
            var wallA = MiniKit.M("Bounce_WallA", new Color(1f, 0.85f, 0.3f));
            var wallB = MiniKit.M("Bounce_WallB", new Color(0.4f, 0.85f, 0.45f));
            MiniKit.Block(root, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(Half * 2f, 1f, Half * 2f), floorA);
            for (int x = 0; x < 8; x++)
                for (int z = 0; z < 8; z++)
                    if ((x + z) % 2 == 0)
                        MiniKit.Block(root, "Pad", new Vector3(-Half + (x + 0.5f) * 4f, 0.006f, -Half + (z + 0.5f) * 4f), new Vector3(3.95f, 0.01f, 3.95f), floorB, false);
            // Inflatable walls (lumpy columns) and corner towers
            for (int side = 0; side < 4; side++)
            {
                var rot = Quaternion.Euler(0f, side * 90f, 0f);
                var size = side % 2 == 0 ? new Vector3(Half * 2f + 2.4f, 3f, 1.2f) : new Vector3(1.2f, 3f, Half * 2f + 2.4f);
                MiniKit.Block(root, "Wall", rot * new Vector3(0f, 1.5f, Half + 0.6f), size, side % 2 == 0 ? wallA : wallB);
                for (int k = 0; k < 9; k++)
                    LowPoly.Make(LowPoly.Ico(1), "Puff", root, rot * new Vector3(-Half + k * 4f, 3f, Half + 0.6f), new Vector3(2f, 1.2f, 1.6f), side % 2 == 0 ? wallB : wallA);
            }
            for (int i = 0; i < 4; i++)
            {
                var p = Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, 0f, (Half + 0.6f) * 1.414f);
                LowPoly.Make(LowPoly.Cylinder(12), "Tower", root, p + Vector3.up * 3f, new Vector3(3f, 6f, 3f), i % 2 == 0 ? wallA : wallB);
                LowPoly.Make(LowPoly.Cone(12), "TowerTop", root, p + Vector3.up * 7f, new Vector3(3.4f, 2.2f, 3.4f), floorA);
            }
            // Trampolines: a frame ring and a springy mat (squashes when used)
            for (int i = 0; i < Tramps.Length; i++)
            {
                var t = Tramps[i];
                MiniKit.Disc(root, "Tramp_" + i, t + new Vector3(0f, TrampTop * 0.5f, 0f), TrampR, TrampTop, MiniKit.M("Bounce_Tramp", new Color(0.12f, 0.12f, 0.14f)), 18);
                LowPoly.Make(LowPoly.Torus(18, 4, 0.18f), "TrampFrame", root, t + new Vector3(0f, TrampTop, 0f), new Vector3(TrampR * 2.1f, 0.6f, TrampR * 2.1f),
                    MiniKit.M("Bounce_Frame", new Color(0.2f, 0.5f, 0.95f)));
            }

            info.spawnPoints = MiniKit.TwoRings(Origin, 5f, 13f);
            info.center = Origin;
            info.radius = Half - 1f;
            MiniKit.Dress(root, 19f, 1616, "BALLOON POP", "no shoes. no refunds. no survivors.", "BOUNCY BOB'S", new Color(0.3f, 0.25f, 0.35f));
        }

        public override void Begin()
        {
            base.Begin();
            trampMats = new Transform[Tramps.Length];
            trampSquash = new float[Tramps.Length];
            for (int i = 0; i < Tramps.Length; i++) trampMats[i] = Course.transform.Find("Tramp_" + i);
        }

        public override void End()
        {
            base.End();
            if (trampMats == null) return;
            for (int i = 0; i < trampMats.Length; i++)
                if (trampMats[i] != null) trampMats[i].localScale = new Vector3(TrampR * 2f, TrampTop, TrampR * 2f);
        }

        // ------------------------------------------------------------------ balloons

        protected override int ItemCount => Mathf.FloorToInt((Duration - 2f) / Interval);

        protected override Item MakeItem(int i)
        {
            bool gold = Rand(i, 1) < 0.1f;
            bool high = Rand(i, 2) < 0.6f;
            Vector3 p;
            float h;
            if (high)
            {
                var t = Tramps[Mathf.FloorToInt(Rand(i, 3) * Tramps.Length) % Tramps.Length];
                float a = Rand(i, 4) * Mathf.PI * 2f, d = Rand(i, 5) * 2.2f;
                p = t + new Vector3(Mathf.Sin(a) * d, 0f, Mathf.Cos(a) * d);
                h = 3f + Rand(i, 6) * 3f;
            }
            else
            {
                p = new Vector3((Rand(i, 3) * 2f - 1f) * (Half - 2f), 0f, (Rand(i, 4) * 2f - 1f) * (Half - 2f));
                h = 1.2f + Rand(i, 6) * 1.4f;
            }
            return new Item { spawn = 1f + i * Interval + Rand(i, 7) * 0.25f, life = 9f, pos = W(p.x, h, p.z), type = (byte)(gold ? 1 : 0) };
        }

        protected override Vector3 PositionAt(int i, float t, out bool collectable)
        {
            float age = t - items[i].spawn;
            collectable = age > 0.3f;
            float grow = Mathf.Clamp01(age / 0.4f);
            return items[i].pos + new Vector3(Mathf.Sin(age * 1.3f + i) * 0.3f, age * 0.12f - (1f - grow) * 0.6f, Mathf.Cos(age * 1.1f + i) * 0.3f);
        }

        protected override int Value(int i) => items[i].type == 1 ? GoldCash : NormalCash;

        protected override GameObject MakeView(int i, Transform parent)
        {
            var root = new GameObject("Balloon");
            root.transform.SetParent(parent, true);
            bool gold = items[i].type == 1;
            var mat = gold ? Mats.Unlit("Balloon_Gold", new Color(1f, 0.85f, 0.3f))
                           : Mats.Get("Balloon_" + (i % BalloonColors.Length), BalloonColors[i % BalloonColors.Length]);
            LowPoly.Make(LowPoly.Ico(1), "Body", root.transform, Vector3.zero, new Vector3(0.8f, 1f, 0.8f), mat);
            LowPoly.Make(LowPoly.Cone(6), "Knot", root.transform, new Vector3(0f, -0.55f, 0f), new Vector3(0.18f, 0.16f, 0.18f), mat, new Vector3(180f, 0f, 0f));
            LowPoly.Make(LowPoly.Cylinder(4), "String", root.transform, new Vector3(0f, -1.1f, 0f), new Vector3(0.025f, 1f, 0.025f), Mats.Get("White", Palette.White));
            return root;
        }

        protected override void OnGrabbed(int i, Vector3 at, Contestant by)
        {
            Sfx.At("pop", at, 1f, 0.15f, 40f);
            Puffs.Confetti(at, items[i].type == 1 ? 26 : 10, 6f);
        }

        // ------------------------------------------------------------------ trampolines

        public override void Tick()
        {
            base.Tick();
            if (trampMats == null) return;
            for (int k = 0; k < Tramps.Length; k++)
            {
                Vector3 t = W(Tramps[k]) + Vector3.up * TrampTop;
                foreach (var c in LocalAlive())
                {
                    Vector3 p = c.transform.position;
                    if (Shapes.Flat(p - t).magnitude > TrampR - 0.1f) continue;
                    if (p.y < t.y - 0.25f || p.y > t.y + 0.45f) continue;
                    if (c.Motor.Velocity.y > 1f || c.Motor.Stunned) continue;
                    if (!bounceCooldown.TryHit(c.Dent, 0.35f)) continue;
                    c.Motor.Launch(Vector3.up * Bounce, 0f);
                    Sfx.At("boing", t, 0.8f, 0.15f, 35f);
                    BounceFx(k);
                }
                // Remote dummies bouncing: show the mat squash too.
                foreach (var c in Everyone())
                {
                    if (c.IsLocal) continue;
                    Vector3 p = c.transform.position;
                    if (Shapes.Flat(p - t).magnitude < TrampR && Mathf.Abs(p.y - t.y) < 0.4f && c.Motor.Velocity.y > 6f) BounceFx(k);
                }
                trampSquash[k] = Mathf.MoveTowards(trampSquash[k], 0f, Time.deltaTime * 4f);
                if (trampMats[k] != null)
                {
                    float sq = Mathf.Sin(trampSquash[k] * Mathf.PI) * 0.6f;
                    trampMats[k].localScale = new Vector3(TrampR * 2f * (1f + sq * 0.08f), TrampTop * (1f - sq * 0.5f), TrampR * 2f * (1f + sq * 0.08f));
                }
            }
        }

        void BounceFx(int k)
        {
            if (trampSquash[k] < 0.5f) trampSquash[k] = 1f;
        }

        // ------------------------------------------------------------------ bots

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            // In the air: steer at whatever is closest, at any height.
            if (!me.Motor.Grounded) return NearestItem(pos, 99f);
            var low = NearestItem(pos, 2.6f);
            if (low.HasValue && Shapes.Flat(low.Value - pos).magnitude < 10f) return low;
            // Otherwise head for the trampoline under the nearest high balloon.
            var high = NearestItem(pos, 99f);
            if (!high.HasValue) return low;
            Vector3 best = Origin;
            float bestD = float.MaxValue;
            foreach (var t in Tramps)
            {
                float d = Shapes.Flat(W(t) - high.Value).magnitude;
                if (d < bestD) { bestD = d; best = W(t); }
            }
            return best;
        }

        public override void BotExtra(Contestant me, float skill, float dt)
        {
            // Jump at low balloons that are just out of reach.
            var low = NearestItem(me.transform.position, 2.6f);
            if (!low.HasValue || !me.Motor.Grounded) return;
            Vector3 d = low.Value - me.transform.position;
            if (Shapes.Flat(d).magnitude < 1.2f && d.y > 1.6f) me.Motor.QueueJump();
        }
    }
}
