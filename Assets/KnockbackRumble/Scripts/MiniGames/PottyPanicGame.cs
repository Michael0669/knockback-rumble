using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// PORTA-POTTY PANIC: musical chairs with porta-potties. Dance while the music plays; when it
    /// stops, run into a potty. There's always one too few. No potty = you're out.
    /// The host runs the rounds (when the music stops, who got which potty, who's out).
    /// </summary>
    public class PottyPanicGame : MiniGame
    {
        const byte OpRound = 1, OpClaim = 2, OpReveal = 3;
        const int SafeCash = 60, MaxPotties = 7;
        const float ScrambleMax = 7f, ClaimRange = 1.5f, ArenaR = 13f;

        enum Phase { Idle, Music, Scramble, Reveal }

        class Potty
        {
            public Transform root, door;
            public Collider doorCol;
            public Contestant owner;
            public bool used;
            public Vector3 Front => root.position + root.forward * 1.25f;
            public Vector3 Inside => root.position + Vector3.up * 0.1f;
        }

        public override EventKind Kind => EventKind.PottyPanic;
        public override string Title => "PORTA-POTTY PANIC";
        public override string Goal => "When the music stops, grab a porta-potty!";
        public override string Objective => "Dance around while the music plays. When it stops, run into a porta-potty. There's always one too few. No potty = you're out!";
        public override string Blurb => "Musical chairs with porta-potties. One too few, every round.";
        public override string[,] Cards => new[,]
        {
            { "MUSIC STOPS", "run into a potty" },
            { "NO POTTY", "and you're out" },
            { "+$" + SafeCash, "every round you survive" },
        };
        public override float Duration => 200f;
        public override bool WeaponsAllowed => false;
        public override bool HasEliminations => true;
        public override string PrizeLabel => "MADE IT TO A POTTY";

        readonly List<Potty> potties = new List<Potty>();
        Phase phase;
        int round, activeCount;
        float stopAt, nextRoundAt;
        readonly Dictionary<Contestant, float> botSeen = new Dictionary<Contestant, float>();

        // ------------------------------------------------------------------ arena

        public override void Build(Transform root, CourseInfo info)
        {
            var floor = MiniKit.M("Potty_Floor", new Color(0.18f, 0.16f, 0.22f));
            MiniKit.Disc(root, "DanceFloor", new Vector3(0f, -0.5f, 0f), ArenaR + 1f, 1f, floor, 36);
            // Light-up dance floor tiles
            Color[] lights = { new Color(1f, 0.3f, 0.6f), new Color(0.3f, 0.8f, 1f), new Color(1f, 0.85f, 0.25f), new Color(0.5f, 1f, 0.4f) };
            for (int x = -6; x <= 6; x++)
                for (int z = -6; z <= 6; z++)
                {
                    var p = new Vector3(x * 2f, 0.006f, z * 2f);
                    if (p.magnitude > ArenaR - 0.6f) continue;
                    MiniKit.Block(root, "DanceTile", p, new Vector3(1.9f, 0.01f, 1.9f), Mats.Unlit("Potty_Light" + ((x + z) & 3), lights[(x * 3 + z * 7 + 40) % 4] * 0.8f), false);
                }
            MiniKit.StripeRim(root, Vector3.zero, ArenaR + 1f, 0.015f, 44);
            LowPoly.Make(LowPoly.Cone(30), "Underside", root, new Vector3(0f, -5f, 0f), new Vector3(ArenaR * 2f + 2f, 8f, ArenaR * 2f + 2f), floor, new Vector3(180f, 0f, 0f));

            // The DJ: Lord Dookington himself, on a booth at the back, with speaker stacks
            BuildDookington(root, new Vector3(0f, 1.2f, ArenaR + 4f), 1.6f);
            MiniKit.Block(root, "DJBooth", new Vector3(0f, 0.6f, ArenaR + 2.5f), new Vector3(5f, 1.2f, 1.5f), MiniKit.M("Potty_Booth", new Color(0.12f, 0.12f, 0.15f)));
            for (int s = -1; s <= 1; s += 2)
            {
                MiniKit.Block(root, "Speaker", new Vector3(s * 5.5f, 1.8f, ArenaR + 3f), new Vector3(2.2f, 3.6f, 1.8f), MiniKit.M("Potty_Speaker", new Color(0.1f, 0.1f, 0.1f)));
                LowPoly.Make(LowPoly.Cylinder(16), "Cone", root, new Vector3(s * 5.5f, 2.6f, ArenaR + 2.08f), new Vector3(1.4f, 0.1f, 1.4f), Kit.Steel, new Vector3(90f, 0f, 0f));
                LowPoly.Make(LowPoly.Cylinder(16), "Cone", root, new Vector3(s * 5.5f, 0.9f, ArenaR + 2.08f), new Vector3(1f, 0.1f, 1f), Kit.Steel, new Vector3(90f, 0f, 0f));
            }
            // A disco ball
            LowPoly.Make(LowPoly.Ico(1), "DiscoBall", root, new Vector3(0f, 12f, 0f), Vector3.one * 2.2f, Mats.Unlit("Potty_Disco", new Color(0.9f, 0.9f, 1f)));
            LowPoly.Make(LowPoly.Cylinder(6), "DiscoString", root, new Vector3(0f, 18f, 0f), new Vector3(0.08f, 12f, 0.08f), Kit.Steel);

            info.spawnPoints = MiniKit.TwoRings(Origin, 5f, 9f);
            info.center = Origin;
            info.radius = ArenaR;
            MiniKit.Lounge(root, info, new Vector3(0f, 9f, 25f));
            MiniKit.Dress(root, 18f, 1313, "PORTA-POTTY PANIC", "hold it in", "LORD DOOKINGTON'S DISCO", new Color(0.25f, 0.2f, 0.3f));
        }

        /// <summary>Lord Dookington III (the show's porta-potty mascot), scaled. Door faces -Z.</summary>
        public static Transform BuildDookington(Transform g, Vector3 p, float s)
        {
            var root = new GameObject("LordDookington").transform;
            root.SetParent(g, false);
            root.localPosition = p;
            root.localScale = Vector3.one * s;
            var body = Mats.Get("Potty", new Color(0.25f, 0.45f, 0.85f));
            var dark = Mats.Get("Potty_Dark", new Color(0.18f, 0.32f, 0.65f));
            var gold = Mats.Get("Gold", Palette.Gold);
            var black = Mats.Get("Dummy_Black", Palette.Black);
            Shapes.Make(PrimitiveType.Cube, "Body", root, new Vector3(0f, 1.4f, 0f), new Vector3(1.6f, 2.8f, 1.6f), body, false);
            Shapes.Make(PrimitiveType.Cube, "Roof", root, new Vector3(0f, 2.9f, 0f), new Vector3(1.75f, 0.2f, 1.75f), dark, false);
            Shapes.Make(PrimitiveType.Cube, "Door", root, new Vector3(0f, 1.3f, -0.81f), new Vector3(1.2f, 2.3f, 0.03f), dark, false);
            Shapes.Make(PrimitiveType.Cube, "Moon", root, new Vector3(0f, 2.15f, -0.83f), new Vector3(0.28f, 0.28f, 0.02f), gold, false, new Vector3(0f, 0f, 45f));
            LowPoly.Make(LowPoly.Cylinder(10), "HatBrim", root, new Vector3(0f, 3.05f, 0f), new Vector3(1.7f, 0.1f, 1.7f), black);
            LowPoly.Make(LowPoly.Cylinder(10), "Hat", root, new Vector3(0f, 3.55f, 0f), new Vector3(1.05f, 1f, 1.05f), black);
            LowPoly.Make(LowPoly.Cylinder(10), "HatBand", root, new Vector3(0f, 3.2f, 0f), new Vector3(1.08f, 0.2f, 1.08f), Mats.Get("Hazard_Red", Palette.Hazard));
            LowPoly.Make(LowPoly.Torus(12, 4, 0.2f), "Monocle", root, new Vector3(-0.35f, 2.15f, -0.86f), new Vector3(0.4f, 0.4f, 0.4f), gold, new Vector3(90f, 0f, 0f));
            return root;
        }

        Potty MakePotty(int i)
        {
            Color[] colors = { new Color(0.25f, 0.45f, 0.85f), new Color(0.3f, 0.7f, 0.35f), new Color(0.9f, 0.5f, 0.2f), new Color(0.6f, 0.35f, 0.75f) };
            var body = Mats.Get("PottyBody" + (i % 4), colors[i % 4]);
            var dark = Mats.Get("PottyDoor" + (i % 4), colors[i % 4] * 0.7f);
            var root = new GameObject("Potty_" + i).transform;
            root.SetParent(Course.transform, false);
            // Walls you can't walk through (back and sides); the front is the door.
            Shapes.Make(PrimitiveType.Cube, "Back", root, new Vector3(0f, 1.3f, -0.75f), new Vector3(1.6f, 2.6f, 0.12f), body);
            Shapes.Make(PrimitiveType.Cube, "SideL", root, new Vector3(-0.78f, 1.3f, 0f), new Vector3(0.12f, 2.6f, 1.6f), body);
            Shapes.Make(PrimitiveType.Cube, "SideR", root, new Vector3(0.78f, 1.3f, 0f), new Vector3(0.12f, 2.6f, 1.6f), body);
            Shapes.Make(PrimitiveType.Cube, "Roof", root, new Vector3(0f, 2.7f, 0f), new Vector3(1.8f, 0.2f, 1.8f), dark, false);
            LowPoly.Make(LowPoly.Cylinder(8), "Vent", root, new Vector3(0.4f, 2.9f, -0.3f), new Vector3(0.25f, 0.3f, 0.25f), Kit.Steel);
            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(root, false);
            hinge.localPosition = new Vector3(-0.72f, 0f, 0.8f);
            var door = Shapes.Make(PrimitiveType.Cube, "Door", hinge, new Vector3(0.72f, 1.25f, 0f), new Vector3(1.42f, 2.45f, 0.08f), dark);
            Shapes.Make(PrimitiveType.Cube, "Moon", door.transform, new Vector3(0f, 0.3f, 0.6f), new Vector3(0.18f, 0.1f, 0.3f), Mats.Get("Gold", Palette.Gold), false,
                new Vector3(0f, 0f, 45f));
            var p = new Potty { root = root, door = hinge, doorCol = door.GetComponent<Collider>() };
            root.gameObject.SetActive(false);
            return p;
        }

        // ------------------------------------------------------------------ flow

        public override void Begin()
        {
            foreach (var p in potties) if (p.root != null) Object.Destroy(p.root.gameObject);
            potties.Clear();
            for (int i = 0; i < MaxPotties; i++) potties.Add(MakePotty(i));
            phase = Phase.Idle;
            round = 0;
            activeCount = 0;
            nextRoundAt = 0f;
            botSeen.Clear();
        }

        public override void End()
        {
            ReleaseAll();
            foreach (var p in potties) if (p.root != null) Object.Destroy(p.root.gameObject);
            potties.Clear();
            SfxRunner.MuteGameMusic = false;
        }

        void ReleaseAll()
        {
            foreach (var p in potties)
            {
                if (p.owner != null && p.owner.IsLocal)
                {
                    p.owner.Motor.InputLocked = false;
                    if (p.root != null && Racing) p.owner.Motor.Teleport(p.Front + p.root.forward * 0.8f, p.root.rotation);
                }
                p.owner = null;
            }
        }

        void StartRound(int r, int count, float stop, int layout)
        {
            ReleaseAll();
            round = r;
            activeCount = Mathf.Clamp(count, 1, MaxPotties);
            stopAt = stop;
            phase = Phase.Music;
            SfxRunner.MuteGameMusic = false;
            var rng = new System.Random(layout);
            var placed = new List<Vector3>();
            for (int i = 0; i < potties.Count; i++)
            {
                var p = potties[i];
                bool on = i < activeCount;
                p.root.gameObject.SetActive(on);
                p.used = false;
                if (!on) continue;
                Vector3 local = Vector3.zero;
                for (int tries = 0; tries < 30; tries++)
                {
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f, d = 3.5f + (float)rng.NextDouble() * (ArenaR - 5.5f);
                    local = new Vector3(Mathf.Sin(a) * d, 0f, Mathf.Cos(a) * d);
                    bool ok = true;
                    foreach (var q in placed) if ((q - local).magnitude < 4f) ok = false;
                    if (ok) break;
                }
                placed.Add(local);
                p.root.position = W(local);
                Vector3 toCenter = -Shapes.Flat(local);
                p.root.rotation = Quaternion.LookRotation(toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector3.forward);
                SetDoor(p, false);
            }
            if (round > 1) Sfx.Ui("streak_ready", 0.6f);
        }

        void SetDoor(Potty p, bool open)
        {
            if (p.door != null) p.door.localRotation = Quaternion.Euler(0f, open ? -110f : 0f, 0f);
            if (p.doorCol != null) p.doorCol.enabled = !open;
        }

        void Stop()
        {
            phase = Phase.Scramble;
            SfxRunner.MuteGameMusic = true;
            Sfx.Ui("scratch", 0.9f);
            for (int i = 0; i < activeCount; i++) SetDoor(potties[i], true);
            if (Race != null && Race.Player != null && !Race.Player.Eliminated) Announcer.ShowToast("THE MUSIC STOPPED! FIND A POTTY!");
        }

        void Claim(int index, Contestant c)
        {
            if (index < 0 || index >= potties.Count || c == null) return;
            var p = potties[index];
            if (p.owner != null) return;
            p.owner = c;
            SetDoor(p, false);
            Sfx.At("slam", p.root.position, 1f, 0.1f, 40f);
            if (c.IsLocal)
            {
                c.Motor.Teleport(p.Inside, Quaternion.LookRotation(p.root.forward));
                c.Motor.InputLocked = true;
            }
            if (c.IsPlayer) Announcer.ShowToast("SAFE! ...but it smells in here");
        }

        void Reveal()
        {
            phase = Phase.Reveal;
            Sfx.Ui("flush", 0.9f);
            for (int i = 0; i < activeCount; i++)
                if (potties[i].owner != null) Puffs.SmokeBurst(potties[i].root.position + Vector3.up * 2.8f, 4, 1f, 0.6f, 1.2f);
        }

        Potty ClaimedBy(Contestant c)
        {
            foreach (var p in potties) if (p.owner == c) return p;
            return null;
        }

        public override void Tick()
        {
            if (phase == Phase.Music && Net.Time >= stopAt) Stop();
            // The disco ball spins and the dance floor pulses while the music plays.
            var ball = Course != null ? Course.transform.Find("DiscoBall") : null;
            if (ball != null && phase == Phase.Music) ball.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
        }

        public override void HostFrame(RaceManager race)
        {
            if (Clock < 1f) return;
            switch (phase)
            {
                case Phase.Idle:
                case Phase.Reveal:
                {
                    if (Time.time < nextRoundAt) return;
                    int alive = race.AliveCount;
                    if (alive < 2) return;
                    int count = Mathf.Min(MaxPotties, alive - 1);
                    float music = Mathf.Lerp(12f, 6f, Mathf.Clamp01(round / 6f)) + Random.Range(-2f, 3f);
                    float stop = Net.Time + music;
                    int layout = Random.Range(1, int.MaxValue);
                    StartRound(round + 1, count, stop, layout);
                    Send(Msg(OpRound).Byte((byte)round).Byte((byte)count).Float(stop).Int(layout));
                    break;
                }
                case Phase.Scramble:
                {
                    int free = 0;
                    for (int i = 0; i < activeCount; i++)
                    {
                        var p = potties[i];
                        if (p.owner != null) continue;
                        free++;
                        foreach (var c in Alive())
                        {
                            if (ClaimedBy(c) != null) continue;
                            if ((c.transform.position - p.Front).magnitude > ClaimRange && (c.transform.position - p.Inside).magnitude > 1.1f) continue;
                            Claim(i, c);
                            Send(Msg(OpClaim).Byte((byte)i).Byte(c.NetId));
                            free--;
                            break;
                        }
                    }
                    if (free == 0 || Net.Time > stopAt + ScrambleMax)
                    {
                        Reveal();
                        Send(Msg(OpReveal));
                        var losers = new List<Contestant>();
                        foreach (var c in Alive())
                        {
                            if (ClaimedBy(c) != null) Award(c, SafeCash);
                            else losers.Add(c);
                        }
                        foreach (var c in losers) MiniGames.HostEliminate(c);
                        nextRoundAt = Time.time + 3.5f;
                    }
                    break;
                }
            }
        }

        public override void OnMessage(byte op, NetReader r)
        {
            switch (op)
            {
                case OpRound:
                {
                    int rnd = r.Byte();
                    int count = r.Byte();
                    float stop = r.Float();
                    int layout = r.Int();
                    StartRound(rnd, count, stop, layout);
                    break;
                }
                case OpClaim:
                {
                    int i = r.Byte();
                    Claim(i, ById(r.Byte()));
                    break;
                }
                case OpReveal:
                    Reveal();
                    break;
            }
        }

        // ------------------------------------------------------------------ HUD

        public override string Status(RaceManager race)
        {
            switch (phase)
            {
                case Phase.Music: return "ROUND " + round + "   -   " + activeCount + " POTTIES FOR " + race.AliveCount + " DUMMIES";
                case Phase.Scramble:
                {
                    int free = 0;
                    for (int i = 0; i < activeCount; i++) if (potties[i].owner == null) free++;
                    return "GRAB A POTTY!   " + free + " LEFT";
                }
                case Phase.Reveal: return "ROUND " + round + " OVER";
                default: return "GET READY TO DANCE";
            }
        }

        public override string Banner(Contestant player, out Color color)
        {
            color = UiSkin.Red;
            if (player == null || player.Eliminated) return null;
            bool safe = ClaimedBy(player) != null;
            if (phase == Phase.Scramble && !safe) return Mathf.Repeat(Time.time * 3f, 1f) < 0.65f ? "FIND A POTTY!!" : "";
            if ((phase == Phase.Scramble || phase == Phase.Reveal) && safe)
            {
                color = UiSkin.Green;
                return "SAFE!  ENJOY THE SMELL";
            }
            if (phase == Phase.Music)
            {
                color = UiSkin.Cream;
                return "DANCE!  (wait for the music to stop)";
            }
            return null;
        }

        public override string StatusFor(Contestant c) => ClaimedBy(c) != null ? "IN A POTTY" : null;

        // ------------------------------------------------------------------ bots

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            if (phase == Phase.Scramble && ClaimedBy(me) == null)
            {
                // Bots notice the music stopped after a moment (better bots notice sooner).
                if (!botSeen.TryGetValue(me, out float seen) || seen < stopAt)
                {
                    seen = Net.Time + Mathf.Lerp(0.9f, 0.15f, skill) + Random.Range(0f, 0.3f);
                    botSeen[me] = seen;
                }
                if (Net.Time >= seen)
                {
                    Potty best = null;
                    float bestD = float.MaxValue;
                    for (int i = 0; i < activeCount; i++)
                    {
                        var p = potties[i];
                        if (p.owner != null) continue;
                        float d = (p.Front - pos).magnitude;
                        if (d < bestD) { bestD = d; best = p; }
                    }
                    if (best != null) return bestD < 1.6f ? best.Inside : best.Front;
                }
            }
            // Dance: drift around a ring.
            float a = Time.time * 0.4f + me.NetId * 0.8f;
            float r = 5f + 3f * Mathf.Sin(Time.time * 0.3f + me.NetId);
            return Origin + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
        }
    }
}
