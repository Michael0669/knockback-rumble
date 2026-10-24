using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// SKY HIGH: race up a staircase of floating platforms into the clouds. The steps get
    /// narrower the higher you go, and guns are allowed, so expect to be shot off the top step.
    /// Fall and you go back to the last checkpoint.
    /// </summary>
    public class SkyHighGame : MiniGame
    {
        const int Steps = 18;
        const float StepZ = 4.6f, StepY = 1.1f, FirstZ = 9f, StepDepth = 3.4f;

        public override EventKind Kind => EventKind.SkyHigh;
        public override string Title => "SKY HIGH";
        public override string Goal => "Race up the floating steps. First to the top wins.";
        public override string Objective => "Jump up a staircase of floating platforms into the sky. Guns allowed - shoot people off the steps! Fall and you're back at the checkpoint.";
        public override string Blurb => "Race up floating steps into the clouds. Guns allowed.";
        public override string[,] Cards => new[,]
        {
            { "$" + ShowManager.FinishPayout[0], "for 1st place" },
            { "JUMP", "from step to step" },
            { "SHOOT", "people off the stairs" },
        };
        public override float Duration => 120f;
        public override bool IsRace => true;

        static Vector3 StepAt(int i, out float width)
        {
            width = Mathf.Lerp(9f, 4.5f, i / (float)(Steps - 1));
            float x = (i % 2 == 0 ? -1f : 1f) * Mathf.Lerp(3.2f, 1.6f, i / (float)(Steps - 1));
            return new Vector3(x, StepY * (i + 1), FirstZ + i * StepZ);
        }

        static float SummitZ => FirstZ + Steps * StepZ + 1f;
        static float SummitY => StepY * (Steps + 1);

        public override void Build(Transform root, CourseInfo info)
        {
            var cloud = MiniKit.M("Sky_Cloud", new Color(0.97f, 0.97f, 1f));
            var cloudB = MiniKit.M("Sky_CloudB", new Color(0.85f, 0.9f, 0.98f));
            var gold = MiniKit.M("Gold", Palette.Gold);
            MiniKit.Block(root, "StartPad", new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 14f), cloud);
            var wps = new List<Vector3> { W(0f, 0f, 4f) };
            for (int i = 0; i < Steps; i++)
            {
                var p = StepAt(i, out float w);
                MiniKit.Block(root, "Step" + i, p + Vector3.down * 0.5f, new Vector3(w, 1f, StepDepth), i % 3 == 2 ? cloudB : cloud);
                // Puffy cloud bits underneath
                LowPoly.Make(LowPoly.Ico(1), "Puff", root, p + new Vector3(-w * 0.3f, -1.1f, 0f), new Vector3(w * 0.6f, 1.4f, StepDepth * 0.9f), cloudB);
                LowPoly.Make(LowPoly.Ico(1), "Puff", root, p + new Vector3(w * 0.3f, -1.2f, 0.2f), new Vector3(w * 0.5f, 1.2f, StepDepth * 0.8f), cloud);
                wps.Add(W(p));
            }
            var summit = new Vector3(0f, SummitY, SummitZ + 4f);
            MiniKit.Block(root, "Summit", summit + Vector3.down * 0.5f, new Vector3(18f, 1f, 14f), cloud);
            MiniKit.Block(root, "SummitRug", summit + new Vector3(0f, 0.006f, 2f), new Vector3(8f, 0.01f, 6f), gold, false);
            MiniKit.FinishGate(root, new Vector3(0f, SummitY, SummitZ), 16f);
            wps.Add(W(0f, SummitY, SummitZ + 3f));

            // Checkpoints on steps 6 and 12
            for (int c = 0; c < 2; c++)
            {
                var p = StepAt(6 + c * 6, out float w);
                MiniKit.CheckpointGate(root, p + Vector3.up * 0.02f, w - 0.6f, c);
            }

            // Sky dressing: a sun and some big clouds far away
            LowPoly.Make(LowPoly.Ico(2), "Sun", root, new Vector3(60f, 60f, 150f), Vector3.one * 20f, MiniKit.Glow("Sky_Sun", new Color(1f, 0.9f, 0.5f)));
            var rng = new System.Random(2626);
            for (int i = 0; i < 18; i++)
                LowPoly.Make(LowPoly.Ico(1), "FarCloud", root,
                    new Vector3((float)rng.NextDouble() * 160f - 80f, -10f + (float)rng.NextDouble() * 40f, (float)rng.NextDouble() * 160f - 30f),
                    new Vector3(12f + (float)rng.NextDouble() * 14f, 4f + (float)rng.NextDouble() * 4f, 8f + (float)rng.NextDouble() * 8f), i % 2 == 0 ? cloud : cloudB);

            info.spawnPoints = MiniKit.StartGrid(Origin, 2f, 16f);
            info.waypoints = wps.ToArray();
            info.center = W(0f, SummitY * 0.5f, SummitZ * 0.5f);
            info.radius = 50f;
            info.killY = Origin.y - 14f;
        }

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            // The next step up from where you are.
            float z = pos.z - Origin.z;
            for (int i = 0; i < Steps; i++)
            {
                var p = StepAt(i, out _);
                if (p.z > z + 1f || (Mathf.Abs(p.z - z) < StepDepth * 0.5f && pos.y < Origin.y + p.y - 0.4f)) return W(p);
            }
            return W(0f, SummitY, SummitZ + 4f);
        }

        public override void BotExtra(Contestant me, float skill, float dt)
        {
            var motor = me.Motor;
            if (motor.Stunned || !motor.Grounded) return;
            var g = BotGoal(me, me.transform.position, skill);
            if (g == null) return;
            Vector3 to = g.Value - me.transform.position;
            Vector3 flat = Shapes.Flat(to);
            if (flat.magnitude < 0.5f) return;
            motor.MoveInput = flat.normalized;
            // Jump when the next step is above us and close enough.
            if (to.y > 0.4f && flat.magnitude < 3.2f + skill) motor.QueueJump();
        }
    }
}
