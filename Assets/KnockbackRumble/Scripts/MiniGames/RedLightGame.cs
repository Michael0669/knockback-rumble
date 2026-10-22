using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// RED LIGHT, GREEN LIGHT: a giant Lord Dookington stands at the end of the field. Run while his
    /// back is turned (green); freeze when he's looking (red), or get zapped back. First to the line
    /// wins. The light schedule comes from the round's seed; each machine zaps its own dummies.
    /// </summary>
    public class RedLightGame : MiniGame
    {
        const float HalfX = 13f, FinishZ = 72f, Grace = 0.28f, MoveLimit = 1.2f, YellowTime = 0.8f;

        enum Signal { Green, Yellow, Red }

        struct Seg
        {
            public float start, end;
            public Signal light;
        }

        public override EventKind Kind => EventKind.RedLight;
        public override string Title => "RED LIGHT, GREEN LIGHT";
        public override string Goal => "Run on green. FREEZE on red.";
        public override string Objective => "Run for the finish while Lord Dookington has his back turned. When he turns around (RED), freeze! Move and you get zapped back.";
        public override string Blurb => "Run on green, freeze on red, or get zapped back.";
        public override string[,] Cards => new[,]
        {
            { "GREEN", "run!" },
            { "RED", "freeze or get zapped" },
            { "$" + ShowManager.FinishPayout[0], "for 1st place" },
        };
        public override float Duration => 120f;
        public override bool IsRace => true;
        public override bool WeaponsAllowed => false;

        readonly List<Seg> segs = new List<Seg>();
        readonly Dictionary<Contestant, int> zappedIn = new Dictionary<Contestant, int>();
        Transform doll, eyes;
        Renderer[] lamps = new Renderer[6];
        Material lampOff;
        Signal shownLight = Signal.Green;
        float dollYaw = 180f;

        public override void Build(Transform root, CourseInfo info)
        {
            var grass = MiniKit.M("Red_Grass", new Color(0.4f, 0.62f, 0.3f));
            var grassB = MiniKit.M("Red_GrassB", new Color(0.36f, 0.56f, 0.27f));
            var chalk = MiniKit.M("White", Palette.White);
            var wall = MiniKit.M("Red_Wall", new Color(0.85f, 0.75f, 0.55f));
            MiniKit.Block(root, "Field", new Vector3(0f, -0.5f, 40f), new Vector3(HalfX * 2f, 1f, 92f), grass);
            for (int i = 0; i < 9; i++)
                MiniKit.Block(root, "Mow", new Vector3(0f, 0.004f, -6f + i * 10f + 5f), new Vector3(HalfX * 2f, 0.01f, 5f), grassB, false);
            for (int z = 10; z < FinishZ; z += 10)
            {
                MiniKit.Block(root, "Line", new Vector3(0f, 0.01f, z), new Vector3(HalfX * 2f, 0.01f, 0.25f), chalk, false);
                var mark = MiniKit.Sign(root, z.ToString(), new Vector3(-HalfX + 1.5f, 0.05f, z - 1f), 0.15f, Color.white);
                if (mark != null) mark.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            }
            // Walls all round (nobody falls off here: the danger is the zap)
            MiniKit.Block(root, "WallL", new Vector3(-HalfX - 0.3f, 0.6f, 40f), new Vector3(0.6f, 1.2f, 92f), wall);
            MiniKit.Block(root, "WallR", new Vector3(HalfX + 0.3f, 0.6f, 40f), new Vector3(0.6f, 1.2f, 92f), wall);
            MiniKit.Block(root, "WallBack", new Vector3(0f, 0.6f, -6.3f), new Vector3(HalfX * 2f + 1.2f, 1.2f, 0.6f), wall);
            MiniKit.Block(root, "WallFar", new Vector3(0f, 0.6f, 86.3f), new Vector3(HalfX * 2f + 1.2f, 1.2f, 0.6f), wall);

            MiniKit.FinishGate(root, new Vector3(0f, 0f, FinishZ), HalfX * 2f - 0.5f);

            // The doll: a giant Lord Dookington on a plinth past the finish
            MiniKit.Block(root, "Plinth", new Vector3(0f, 1f, 92f), new Vector3(9f, 2f, 9f), MiniKit.M("Marble_White", new Color(0.86f, 0.84f, 0.8f)));
            var d = PottyPanicGame.BuildDookington(root, new Vector3(0f, 2f, 92f), 4f);
            d.localEulerAngles = new Vector3(0f, 180f, 0f);
            var e = new GameObject("Eyes").transform;
            e.SetParent(d, false);
            var glare = Mats.Unlit("Red_Eye", new Color(1f, 0.1f, 0.05f));
            LowPoly.Make(LowPoly.Ico(1), "EyeL", e, new Vector3(-0.3f, 2.3f, -0.88f), Vector3.one * 0.22f, glare);
            LowPoly.Make(LowPoly.Ico(1), "EyeR", e, new Vector3(0.3f, 2.3f, -0.88f), Vector3.one * 0.22f, glare);

            // Traffic lights on both sides of the finish
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? -HalfX - 2f : HalfX + 2f;
                LowPoly.Make(LowPoly.Cylinder(8), "Pole", root, new Vector3(x, 4f, FinishZ + 2f), new Vector3(0.4f, 8f, 0.4f), Kit.Steel);
                MiniKit.Block(root, "LightBox", new Vector3(x, 7.5f, FinishZ + 2f), new Vector3(1.4f, 3.8f, 1f), MiniKit.M("Dummy_Black", Palette.Black), false);
                for (int k = 0; k < 3; k++)
                    LowPoly.Make(LowPoly.Ico(1), "Lamp" + s + k, root, new Vector3(x, 8.7f - k * 1.2f, FinishZ + 1.45f), Vector3.one * 0.9f, MiniKit.M("Red_LampOff", new Color(0.15f, 0.15f, 0.15f)));
            }

            info.spawnPoints = MiniKit.StartGrid(Origin, 2f, HalfX * 1.6f);
            var wps = new List<Vector3>();
            for (float z = 4f; z <= FinishZ + 6f; z += 6f) wps.Add(W(0f, 0f, z));
            info.waypoints = wps.ToArray();
            info.center = W(0f, 0f, 36f);
            info.radius = 36f;
            var rng = new System.Random(1515);
            Props.Grandstand(root, new Vector3(-HalfX - 10f, -1f, 36f), 90f, 80f, 5, rng, 10f);
            Props.Grandstand(root, new Vector3(HalfX + 10f, -1f, 36f), -90f, 80f, 5, rng, 10f);
            MiniKit.Block(root, "Ground", new Vector3(0f, -1.2f, 40f), new Vector3(140f, 1f, 200f), MiniKit.M("Red_Ground", new Color(0.32f, 0.45f, 0.25f)), false);
            Props.Backdrop(root, new Vector3(0f, 0f, 40f), 70f, 1515, "DON'T MOVE");
            MiniKit.Sign(root, "RED LIGHT, GREEN LIGHT", new Vector3(0f, 22f, 100f), 0.6f, Palette.Gold);
        }

        public override void Begin()
        {
            segs.Clear();
            float t = -10f;
            int i = 0;
            // The first green is long, so everyone gets going.
            segs.Add(new Seg { start = t, end = 5f + Rand(0, 1) * 2f, light = Signal.Green });
            t = segs[0].end;
            while (t < Duration + 5f)
            {
                i++;
                segs.Add(new Seg { start = t, end = t + YellowTime, light = Signal.Yellow });
                t += YellowTime;
                float red = 2f + Rand(i, 2) * 2.2f;
                segs.Add(new Seg { start = t, end = t + red, light = Signal.Red });
                t += red;
                float green = 2.2f + Rand(i, 3) * 3.5f;
                segs.Add(new Seg { start = t, end = t + green, light = Signal.Green });
                t += green;
            }
            zappedIn.Clear();
            doll = Course.transform.Find("LordDookington");
            eyes = doll != null ? doll.Find("Eyes") : null;
            lampOff = Mats.Get("Red_LampOff", new Color(0.15f, 0.15f, 0.15f));
            for (int s = 0; s < 2; s++)
                for (int k = 0; k < 3; k++)
                {
                    var lamp = Course.transform.Find("Lamp" + s + k);
                    lamps[s * 3 + k] = lamp != null ? lamp.GetComponent<Renderer>() : null;
                }
            shownLight = Signal.Red; // force a refresh
            dollYaw = 180f;
        }

        int SegAt(float t)
        {
            for (int i = 0; i < segs.Count; i++) if (t < segs[i].end) return i;
            return segs.Count - 1;
        }

        Signal LightAt(float t, out float since, out int index)
        {
            index = SegAt(t);
            var s = segs[index];
            since = t - s.start;
            return s.light;
        }

        public override void Tick()
        {
            if (segs.Count == 0) return;
            float t = Clock;
            var light = LightAt(t, out float since, out int index);

            // Turn the doll: back to us on green, turning on yellow, facing us on red.
            float want = light == Signal.Green ? 180f : light == Signal.Yellow ? Mathf.Lerp(180f, 0f, since / YellowTime) : 0f;
            dollYaw = light == Signal.Green ? Mathf.MoveTowards(dollYaw, want, 500f * Time.deltaTime) : want;
            if (doll != null) doll.localRotation = Quaternion.Euler(0f, dollYaw, 0f);
            if (eyes != null && eyes.gameObject.activeSelf != (light == Signal.Red)) eyes.gameObject.SetActive(light == Signal.Red);

            if (light != shownLight)
            {
                shownLight = light;
                var on = light == Signal.Red ? Mats.Unlit("Red_LampRed", new Color(1f, 0.15f, 0.1f))
                       : light == Signal.Yellow ? Mats.Unlit("Red_LampYellow", new Color(1f, 0.8f, 0.15f))
                       : Mats.Unlit("Red_LampGreen", new Color(0.2f, 1f, 0.3f));
                int lit = light == Signal.Red ? 0 : light == Signal.Yellow ? 1 : 2;
                for (int i = 0; i < lamps.Length; i++)
                    if (lamps[i] != null) lamps[i].sharedMaterial = i % 3 == lit ? on : lampOff;
                if (t > 0f && Race != null && Race.State == RaceManager.Phase.Racing)
                {
                    if (light == Signal.Red) Sfx.Ui("siren", 0.5f);
                    else if (light == Signal.Green) Sfx.Ui("go_horn", 0.5f);
                }
            }

            // Zap our own dummies that move on red.
            if (light != Signal.Red || since < Grace || !Racing) return;
            foreach (var c in LocalAlive())
            {
                if (zappedIn.TryGetValue(c, out int z) && z == index) continue;
                Vector3 v = c.Motor.Velocity;
                if (Shapes.Flat(v).magnitude < MoveLimit && Mathf.Abs(v.y) < 3f) continue;
                zappedIn[c] = index;
                Vector3 eye = doll != null ? doll.position + Vector3.up * 9f : W(0f, 10f, 90f);
                Fx.Tracer(eye, c.Dent.Center, new Color(1f, 0.2f, 0.1f), 0.25f, 0.25f);
                Fx.Tracer(eye + Vector3.right * 1.2f, c.Dent.Center, new Color(1f, 0.2f, 0.1f), 0.25f, 0.25f);
                Sfx.At("zap", c.transform.position, 1f, 0.1f, 60f);
                c.Dent.Hit(new Vector3(0f, 0.6f, -1f), 20f, 10f, null, "Red Light", 0.45f);
                if (c.IsPlayer) Announcer.ShowToast("ZAPPED! You moved on red!");
            }
        }

        public override string Status(RaceManager race)
        {
            var light = LightAt(Clock, out _, out _);
            string l = light == Signal.Red ? "RED LIGHT" : light == Signal.Yellow ? "HE'S TURNING..." : "GREEN LIGHT";
            return l + "     FINISHED " + race.Finishers.Count + " / " + race.FinishTarget;
        }

        public override string Banner(Contestant player, out Color color)
        {
            color = UiSkin.Green;
            if (player == null || player.Finished || segs.Count == 0) return null;
            var light = LightAt(Clock, out _, out _);
            if (light == Signal.Red) { color = UiSkin.Red; return "RED LIGHT!  FREEZE!"; }
            if (light == Signal.Yellow) { color = UiSkin.Gold; return "HE'S TURNING AROUND..."; }
            return "GREEN LIGHT!  GO GO GO!";
        }

        public override bool BotHold(Contestant me, float skill)
        {
            if (segs.Count == 0 || me.Finished) return false;
            var light = LightAt(Clock, out float since, out _);
            if (light == Signal.Red) return since > Mathf.Lerp(0.45f, 0.05f, skill);
            if (light == Signal.Yellow) return since > Mathf.Lerp(YellowTime + 0.1f, 0.35f, skill);
            return false;
        }
    }
}
