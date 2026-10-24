using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// SWIRLY: you're in a giant toilet bowl and someone just flushed. The floor swirls you round
    /// and pulls you toward the drain in the middle, and the drain keeps getting bigger as rings
    /// of the floor are flushed away. Shove people down the drain. Fall in and you're out.
    /// Ring timing comes from the shared clock; each machine pushes its own dummies.
    /// </summary>
    public class SwirlyGame : MiniGame
    {
        const int Rings = 6;
        const float Outer = 15f, RingWidth = 2f, Spin = 4.5f, Pull = 1.6f;

        public override EventKind Kind => EventKind.Swirly;
        public override string Title => "SWIRLY";
        public override string Goal => "Don't get flushed. Shove the others down the drain.";
        public override string Objective => "You're in a giant toilet and it's flushing. The water swirls you around and drags you to the drain, which keeps growing. Fall in and you're out!";
        public override string Blurb => "A giant flushing toilet bowl drags you to the drain. One life.";
        public override string[,] Cards => new[,]
        {
            { "FLUSH!", "the floor drags you in" },
            { "FALL = OUT", "one life only" },
            { "$5 / SEC", "while you're still in" },
        };
        public override float Duration => 100f;
        public override bool FallsEliminate => true;
        public override bool WeaponsAllowed => false;
        public override int StandingPerSecond => 5;

        GameObject[] ringParts;
        Transform swirl;
        int shownGone = -1;

        static float InnerOf(int ring) => Outer - (Rings - ring) * RingWidth;   // ring 0 = innermost
        float Drain => InnerOf(RingsGone());

        /// <summary>How many rings have been flushed (from the inside out).</summary>
        int RingsGone()
        {
            float t = Clock;
            int gone = 0;
            for (int i = 0; i < Rings - 1; i++)
                if (t > 12f + i * 15f) gone = i + 1;
            return gone;
        }

        public override void Build(Transform root, CourseInfo info)
        {
            var porcelain = MiniKit.M("Swirly_Porcelain", new Color(0.95f, 0.96f, 0.98f));
            var water = MiniKit.M("Swirly_Water", new Color(0.45f, 0.7f, 0.95f));
            var waterB = MiniKit.M("Swirly_WaterB", new Color(0.35f, 0.6f, 0.9f));
            // The floor: rings of segments, so the inner ones can be flushed away
            for (int r = 0; r < Rings; r++)
            {
                var ring = new GameObject("Ring" + r).transform;
                ring.SetParent(root, false);
                float inner = InnerOf(r), outer = inner + RingWidth, mid = (inner + outer) * 0.5f;
                int n = Mathf.Max(8, Mathf.RoundToInt(mid * 2.2f));
                float seg = 2f * Mathf.PI * mid / n;
                for (int i = 0; i < n; i++)
                {
                    float a = (i + 0.5f) / n * 360f;
                    MiniKit.Block(ring, "Seg", Quaternion.Euler(0f, a, 0f) * new Vector3(0f, -0.5f, mid), new Vector3(seg * 1.08f, 1f, RingWidth + 0.02f),
                        (r + i) % 2 == 0 ? water : waterB, true, new Vector3(0f, a, 0f));
                }
            }
            // The bowl's rim (outside the water) and the giant seat and tank behind
            for (int i = 0; i < 40; i++)
            {
                float a = i * 9f;
                MiniKit.Block(root, "Rim", Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.6f, Outer + 0.7f), new Vector3(2.5f, 2.2f, 1.4f), porcelain, true, new Vector3(0f, a, 0f));
            }
            LowPoly.Make(LowPoly.Torus(40, 6, 0.2f), "Seat", root, new Vector3(0f, 3f, 0f), new Vector3(Outer * 2.3f, 3f, Outer * 2.3f), porcelain);
            MiniKit.Block(root, "Tank", new Vector3(0f, 8f, Outer + 8f), new Vector3(22f, 14f, 6f), porcelain, false);
            MiniKit.Block(root, "Handle", new Vector3(-8f, 13f, Outer + 4.9f), new Vector3(4f, 1f, 1f), Kit.Steel, false);
            // Swirl lines on the water (spin at runtime, visual only)
            swirl = new GameObject("Swirl").transform;
            swirl.SetParent(root, false);
            for (int i = 0; i < 6; i++)
                MiniKit.Block(swirl, "SwirlLine", Quaternion.Euler(0f, i * 60f, 0f) * new Vector3(0f, 0.02f, 9f), new Vector3(0.25f, 0.01f, 9f),
                    MiniKit.M("White", Palette.White), false, new Vector3(0f, i * 60f + 25f, 0f));
            // The drain, deep below
            LowPoly.Make(LowPoly.Cylinder(24), "Drain", root, new Vector3(0f, -9f, 0f), new Vector3(10f, 1f, 10f), MiniKit.M("Swirly_Drain", new Color(0.1f, 0.15f, 0.25f)));

            info.spawnPoints = MiniKit.TwoRings(Origin, 9.5f, 12.5f);
            info.center = Origin;
            info.radius = Outer;
            info.killY = Origin.y - 6f;
            MiniKit.Lounge(root, info, new Vector3(0f, 10f, -26f), 180f);
            MiniKit.Dress(root, 18f, 2929, "SWIRLY", null, null, new Color(0.25f, 0.3f, 0.4f));
        }

        public override void Begin()
        {
            ringParts = new GameObject[Rings];
            for (int r = 0; r < Rings; r++)
            {
                var t = Course.transform.Find("Ring" + r);
                ringParts[r] = t != null ? t.gameObject : null;
                if (ringParts[r] != null) ringParts[r].SetActive(true);
            }
            swirl = Course.transform.Find("Swirl");
            shownGone = 0;
        }

        public override void End()
        {
            if (ringParts != null) foreach (var r in ringParts) if (r != null) r.SetActive(true);
        }

        public override void Tick()
        {
            if (ringParts == null) return;
            int gone = RingsGone();
            if (gone != shownGone)
            {
                for (int r = 0; r < Rings; r++) if (ringParts[r] != null) ringParts[r].SetActive(r >= gone);
                if (gone > shownGone && shownGone >= 0)
                {
                    Sfx.Ui("flush");
                    Announcer.ShowToast("FLUSH!");
                }
                shownGone = gone;
            }
            float t = Clock;
            if (swirl != null) swirl.localRotation = Quaternion.Euler(0f, -Time.time * 40f, 0f);
            if (t <= 0f) return;

            // The water drags you around and in (stronger as the game goes on).
            float strength = 1f + Mathf.Clamp01(t / Duration) * 0.8f;
            foreach (var c in LocalAlive())
            {
                Vector3 off = Shapes.Flat(c.transform.position - Origin);
                float d = off.magnitude;
                if (d < 0.1f || d > Outer + 0.5f) continue;
                Vector3 inward = -off / d;
                Vector3 around = Vector3.Cross(Vector3.up, inward);
                c.Motor.AddConveyor((around * Spin + inward * Pull) * strength);
            }
        }

        public override string Status(RaceManager race) => "STILL IN: " + race.AliveCount + "     DRAIN: " + Mathf.RoundToInt(Drain * 2f) + "m WIDE";

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            // Stay near the outside rim, walking against the swirl; shove anyone close.
            Contestant best = null;
            float bd = 3.5f;
            foreach (var c in Alive())
            {
                if (c == me) continue;
                float d = Vector3.Distance(c.transform.position, pos);
                if (d < bd) { bd = d; best = c; }
            }
            if (best != null) return best.transform.position;
            Vector3 off = Shapes.Flat(pos - Origin);
            if (off.sqrMagnitude < 0.01f) off = Vector3.forward;
            float safe = Mathf.Lerp(Drain + 1.5f, Outer - 1f, 0.6f + 0.3f * skill);
            // Aim a little "upstream" so the swirl carries you onto the spot.
            Vector3 dir = Quaternion.Euler(0f, 35f, 0f) * off.normalized;
            return Origin + dir * safe;
        }
    }
}
