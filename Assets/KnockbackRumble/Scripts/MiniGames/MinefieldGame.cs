using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// MINEFIELD: race across a field full of sticky mines to the finish. Shoves only - shove
    /// people onto the mines. Mines re-arm a couple of seconds after they go off.
    /// Mine positions come from the round's seed; each machine blasts its own dummies.
    /// </summary>
    public class MinefieldGame : MiniGame
    {
        const float Width = 24f, Length = 92f, FinishZ = 88f, Trigger = 1.15f, BlastR = 3.2f, Rearm = 2.5f;
        const int MineCount = 70;

        public override EventKind Kind => EventKind.Minefield;
        public override string Title => "MINEFIELD";
        public override string Goal => "Cross the minefield. Shove others onto the mines.";
        public override string Objective => "The field is packed with mines. Find a path to the finish - or just shove someone ahead of you and follow their crater.";
        public override string Blurb => "Race across a field packed with mines. Shoves only.";
        public override string[,] Cards => new[,]
        {
            { "$" + ShowManager.FinishPayout[0], "for 1st place" },
            { "MINES", "re-arm after 2 seconds" },
            { "SHOVE", "people onto them" },
        };
        public override float Duration => 120f;
        public override bool IsRace => true;
        public override bool WeaponsAllowed => false;

        Vector3[] mines;
        float[] firedAt;
        Renderer[] lamps;
        Transform[] views;
        Material lampOn, lampOff;

        public override void Build(Transform root, CourseInfo info)
        {
            var dirt = MiniKit.M("Mine_Dirt", new Color(0.5f, 0.42f, 0.3f));
            var grass = MiniKit.M("Mine_Grass", new Color(0.4f, 0.55f, 0.28f));
            MiniKit.Block(root, "Field", new Vector3(0f, -0.5f, Length * 0.5f - 6f), new Vector3(Width, 1f, Length + 12f), dirt);
            var rng = new System.Random(2727);
            for (int i = 0; i < 50; i++)
                MiniKit.Block(root, "Grass", new Vector3((float)rng.NextDouble() * (Width - 2f) - Width * 0.5f + 1f, 0.006f, (float)rng.NextDouble() * Length),
                    new Vector3(1f + (float)rng.NextDouble() * 2f, 0.01f, 1f + (float)rng.NextDouble() * 2f), grass, false, new Vector3(0f, (float)rng.NextDouble() * 90f, 0f));
            // Sandbag walls down both sides
            var bag = MiniKit.M("Mine_Sandbag", new Color(0.7f, 0.62f, 0.45f));
            for (float z = -5f; z < Length + 5f; z += 2f)
                foreach (float side in new[] { -1f, 1f })
                    MiniKit.Block(root, "Sandbag", new Vector3(side * (Width * 0.5f + 0.4f), 0.45f, z), new Vector3(0.8f, 0.9f, 1.9f), bag);
            // Warning signs on posts (pictures, not words)
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3((i % 2 == 0 ? -1f : 1f) * (Width * 0.5f + 1.5f), 0f, 10f + i * 14f);
                LowPoly.Make(LowPoly.Cylinder(6), "Post", root, p + Vector3.up * 1.2f, new Vector3(0.12f, 2.4f, 0.12f), MiniKit.M("Rodeo_Wood", new Color(0.55f, 0.36f, 0.2f)));
                MiniKit.Block(root, "Warn", p + Vector3.up * 2.3f, new Vector3(1f, 1f, 0.06f), MiniKit.M("Hazard_Red", Palette.Hazard), false, new Vector3(0f, 0f, 45f));
            }
            MiniKit.CheckpointGate(root, new Vector3(0f, 0.02f, 44f), Width - 1f, 0);
            MiniKit.FinishGate(root, new Vector3(0f, 0f, FinishZ), Width - 1f);

            info.spawnPoints = MiniKit.StartGrid(Origin, 0f, 18f);
            var wps = new Vector3[16];
            for (int i = 0; i < wps.Length; i++) wps[i] = W(0f, 0f, 3f + i * 6f);
            info.waypoints = wps;
            info.center = W(0f, 0f, Length * 0.5f);
            info.radius = Length * 0.5f;
            info.killY = Origin.y - 12f;
            var rng2 = new System.Random(2728);
            Props.Grandstand(root, new Vector3(-Width * 0.5f - 12f, -1f, Length * 0.5f), 90f, Length * 0.8f, 5, rng2, 13f);
            Props.Grandstand(root, new Vector3(Width * 0.5f + 12f, -1f, Length * 0.5f), -90f, Length * 0.8f, 5, rng2, 13f);
            MiniKit.Block(root, "Pit", new Vector3(0f, -14f, Length * 0.5f), new Vector3(160f, 1f, 200f), MiniKit.M("MiniPit_Mine", new Color(0.3f, 0.25f, 0.2f)), false);
        }

        public override void Begin()
        {
            End();
            mines = new Vector3[MineCount];
            firedAt = new float[MineCount];
            lamps = new Renderer[MineCount];
            views = new Transform[MineCount];
            lampOn = Mats.Unlit("MineLamp", new Color(1f, 0.25f, 0.2f));
            lampOff = MiniKit.M("Dummy_Black", Palette.Black);
            var dark = MiniKit.M("Gun_Dark", new Color(0.16f, 0.16f, 0.18f));
            var stripe = MiniKit.M("Hazard_Stripe", Palette.Stripe);
            for (int i = 0; i < MineCount; i++)
            {
                float z = 8f + Rand(i, 1) * (FinishZ - 12f);
                float x = (Rand(i, 2) - 0.5f) * (Width - 2f);
                mines[i] = W(x, 0f, z);
                firedAt[i] = -99f;
                var t = new GameObject("Mine").transform;
                t.SetParent(Course.transform, true);
                t.position = mines[i];
                LowPoly.Make(LowPoly.Cylinder(10), "Base", t, new Vector3(0f, 0.06f, 0f), new Vector3(0.9f, 0.12f, 0.9f), dark);
                LowPoly.Make(LowPoly.Torus(14, 4, 0.16f), "Rim", t, new Vector3(0f, 0.1f, 0f), new Vector3(0.95f, 0.5f, 0.95f), stripe);
                lamps[i] = LowPoly.Make(LowPoly.Ico(1), "Lamp", t, new Vector3(0f, 0.17f, 0f), Vector3.one * 0.2f, lampOff).GetComponent<Renderer>();
                views[i] = t;
            }
        }

        public override void End()
        {
            if (views != null) foreach (var v in views) if (v != null) Object.Destroy(v.gameObject);
            views = null;
            mines = null;
        }

        public override void Tick()
        {
            if (mines == null) return;
            bool blink = Mathf.Repeat(Time.time * 2f, 1f) < 0.5f;
            for (int i = 0; i < mines.Length; i++)
            {
                bool armed = Time.time - firedAt[i] > Rearm;
                if (views[i] != null) views[i].gameObject.SetActive(armed || Time.time - firedAt[i] > 0.4f);
                if (lamps[i] != null) lamps[i].sharedMaterial = armed && blink ? lampOn : lampOff;
                if (!armed || Clock <= 0f) continue;
                foreach (var body in DentBody.All)
                {
                    if (body == null) continue;
                    Vector3 p = body.transform.position;
                    if (Shapes.Flat(p - mines[i]).magnitude > Trigger || p.y > mines[i].y + 1.2f) continue;
                    // Everyone sees the boom; each machine only launches its own dummies.
                    firedAt[i] = Time.time;
                    MiniKit.BoomFx(mines[i] + Vector3.up * 0.3f, BlastR);
                    MiniKit.Blast(mines[i] + Vector3.up * 0.3f, BlastR, 17f, 14f, "Mine");
                    break;
                }
            }
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            // Head for the finish, steering around armed mines just ahead (good bots look further).
            Vector3 goal = new Vector3(pos.x, Origin.y, Mathf.Min(pos.z + 6f, Origin.z + FinishZ + 3f));
            if (mines == null) return goal;
            float look = 2.5f + skill * 3f;
            for (int i = 0; i < mines.Length; i++)
            {
                if (Time.time - firedAt[i] <= Rearm) continue;
                Vector3 d = mines[i] - pos;
                if (d.z < 0f || d.z > look || Mathf.Abs(d.x) > 1.8f) continue;
                float side = d.x > 0f ? -1f : 1f;
                if (Mathf.Abs(pos.x - Origin.x) > Width * 0.5f - 2.5f) side = pos.x > Origin.x ? -1f : 1f;
                goal.x = pos.x + side * 3f;
                break;
            }
            return goal;
        }
    }
}
