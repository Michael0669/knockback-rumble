using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// HOT POTATO: one dummy carries a ticking bomb. Touch or shove someone to pass it on.
    /// Whoever holds it when it blows is out; everyone else gets paid for surviving the blast.
    /// The host decides who has it (from positions and shove hits) and tells everyone.
    /// </summary>
    public class HotPotatoGame : MiniGame
    {
        const int SurviveCash = 40;
        const byte OpNewBomb = 1, OpPass = 2, OpBoom = 3;
        const float PassRange = 1.5f, PassCooldown = 0.6f, TagBack = 1.2f;

        public override EventKind Kind => EventKind.HotPotato;
        public override string Title => "HOT POTATO";
        public override string Goal => "Don't be holding the bomb when it blows.";
        public override string Objective => "Someone has a ticking bomb. Touch or shove another dummy to pass it on. Holding it when it blows = you're out!";
        public override string Blurb => "Pass the ticking bomb. Don't be holding it when it blows.";
        public override string[,] Cards => new[,]
        {
            { "TAG", "someone to pass the bomb" },
            { "BOOM = OUT", "if you're holding it" },
            { "+$" + SurviveCash, "every blast you survive" },
        };
        public override float Duration => 150f;
        public override bool WeaponsAllowed => false;
        public override bool HasEliminations => true;
        public override string PrizeLabel => "SURVIVED A BLAST";

        Contestant holder, lastHolder;
        float fuseEnd, fuseLength, passReadyAt, tagBackUntil, nextBombAt, nextTick;
        Transform bomb, spark;
        readonly Dictionary<Contestant, float> baseSpeed = new Dictionary<Contestant, float>();

        // ------------------------------------------------------------------ arena

        public override void Build(Transform root, CourseInfo info)
        {
            var deck = MiniKit.M("Potato_Deck", new Color(0.78f, 0.55f, 0.32f));
            var deckDark = MiniKit.M("Potato_DeckDark", new Color(0.6f, 0.4f, 0.22f));
            var steel = Kit.Steel;
            MiniKit.Disc(root, "Floor", new Vector3(0f, -0.5f, 0f), 15f, 1f, deck, 36);
            for (int i = 0; i < 6; i++)
                MiniKit.Visual(root, LowPoly.Torus(36, 3, 0.04f), "Ring", new Vector3(0f, 0.01f, 0f), new Vector3(5f + i * 4.6f, 0.1f, 5f + i * 4.6f), deckDark);
            MiniKit.StripeRim(root, Vector3.zero, 15f, 0.015f, 40);
            LowPoly.Make(LowPoly.Cone(24), "Underside", root, new Vector3(0f, -4f, 0f), new Vector3(30f, 6f, 30f), deckDark, new Vector3(180f, 0f, 0f));

            // Something to run around: four crates and a big potato sack in the middle
            var crate = MiniKit.M("Potato_Crate", new Color(0.55f, 0.38f, 0.2f));
            for (int i = 0; i < 4; i++)
            {
                var p = Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, 0.75f, 7.5f);
                var box = MiniKit.Block(root, "Crate", p, new Vector3(1.6f, 1.5f, 1.6f), crate, true, new Vector3(0f, i * 23f, 0f));
                Shapes.Make(PrimitiveType.Cube, "Slat", box.transform, new Vector3(0f, 0f, 0.51f), new Vector3(1.02f, 0.18f, 0.02f), deckDark, false);
                Shapes.Make(PrimitiveType.Cube, "Slat", box.transform, new Vector3(0f, 0f, -0.51f), new Vector3(1.02f, 0.18f, 0.02f), deckDark, false);
            }
            LowPoly.MakeSolid(LowPoly.Rock(2), "PotatoSack", root, new Vector3(0f, 1f, 0f), new Vector3(2.6f, 2.2f, 2.4f),
                MiniKit.M("Potato_Sack", new Color(0.72f, 0.62f, 0.42f)));
            MiniKit.Sign(root, "SPUDS", new Vector3(0f, 1.1f, -1.3f), 0.1f, new Color(0.35f, 0.2f, 0.1f));
            for (int i = 0; i < 4; i++)
            {
                var b = Shapes.Make(PrimitiveType.Cylinder, "Bumper", root, Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(0f, 1f, 11.5f),
                    new Vector3(1.6f, 1f, 1.6f), MiniKit.M("Bumper", Palette.Bumper));
                b.AddComponent<Bumper>();
                Props.DressBumper(b.transform);
            }

            info.spawnPoints = MiniKit.TwoRings(Origin, 9.5f, 12.5f);
            info.center = Origin;
            info.radius = 13f;
            MiniKit.Lounge(root, info, new Vector3(0f, 9f, 25f));
            MiniKit.Dress(root, 18f, 301, "HOT POTATO", "don't be the one holding it", "SPUD-U-LIKE", new Color(0.3f, 0.22f, 0.15f));
        }

        // ------------------------------------------------------------------ flow

        public override void Begin()
        {
            holder = lastHolder = null;
            fuseEnd = 0f;
            nextBombAt = 0f;
            baseSpeed.Clear();
            foreach (var c in Everyone())
                if (c.IsLocal) baseSpeed[c] = c.Motor.SpeedMultiplier;

            bomb = new GameObject("HotPotatoBomb").transform;
            bomb.SetParent(Course.transform, true);
            spark = WeaponModels.CartoonBomb(bomb, new Color(1f, 0.3f, 0.15f));
            bomb.localScale = Vector3.one * 0.9f;
            bomb.gameObject.SetActive(false);
        }

        public override void End()
        {
            SetHolder(null, 0f);
            foreach (var kv in baseSpeed)
                if (kv.Key != null) kv.Key.Motor.SpeedMultiplier = kv.Value;
            baseSpeed.Clear();
            if (bomb != null) Object.Destroy(bomb.gameObject);
            bomb = null;
        }

        void SetHolder(Contestant c, float end)
        {
            if (holder != null && holder != c) lastHolder = holder;
            holder = c;
            fuseEnd = end;
            foreach (var kv in baseSpeed)
                if (kv.Key != null) kv.Key.Motor.SpeedMultiplier = kv.Value * (kv.Key == c ? 1.15f : 1f);
            if (c != null)
            {
                Sfx.At("throw", c.transform.position + Vector3.up * 2f, 0.9f, 0.1f, 40f);
                if (c.IsPlayer)
                {
                    Sfx.Ui("siren", 0.6f);
                    Announcer.ShowToast("YOU HAVE THE BOMB!");
                }
            }
        }

        public override void Tick()
        {
            if (bomb == null) return;
            bool show = holder != null && !holder.Eliminated && Race != null && Race.State == RaceManager.Phase.Racing;
            if (bomb.gameObject.activeSelf != show) bomb.gameObject.SetActive(show);
            if (!show) return;

            // The bomb bobs over the holder's head, swelling and shaking as the fuse runs out.
            float left = Mathf.Max(0f, fuseEnd - Net.Time);
            float urgency = 1f - Mathf.Clamp01(left / Mathf.Max(1f, fuseLength));
            bomb.position = holder.transform.position + Vector3.up * (2.55f + Mathf.Sin(Time.time * 6f) * 0.1f);
            bomb.localScale = Vector3.one * (0.9f + urgency * 0.35f + Mathf.Sin(Time.time * (8f + urgency * 30f)) * 0.05f * urgency);
            bomb.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
            if (spark != null) Puffs.Spark(spark.position, Random.onUnitSphere * 2f + Vector3.up, 0.08f, 0.25f, new Color(1f, 0.8f, 0.3f));

            if (Time.time >= nextTick)
            {
                nextTick = Time.time + Mathf.Lerp(0.6f, 0.12f, urgency);
                Sfx.At("tick", bomb.position, 0.5f + urgency * 0.5f, 0.02f, 35f);
            }
        }

        public override void HostFrame(RaceManager race)
        {
            if (Clock < 1.5f) return;

            // Need a bomb?
            if (holder == null || holder.Eliminated)
            {
                if (holder != null) SetHolder(null, 0f);
                if (Time.time < nextBombAt) return;
                var alive = new List<Contestant>(Alive());
                if (alive.Count < 2) return;
                var pick = alive[Random.Range(0, alive.Count)];
                float progress = Mathf.Clamp01(Clock / Duration);
                fuseLength = Mathf.Lerp(16f, 9f, progress) + Random.Range(-2f, 3f);
                float end = Net.Time + fuseLength;
                SetHolder(pick, end);
                passReadyAt = Time.time + 1f;
                Send(Msg(OpNewBomb).Byte(pick.NetId).Float(end).Float(fuseLength));
                return;
            }

            // Boom?
            if (Net.Time >= fuseEnd)
            {
                var victim = holder;
                Vector3 at = victim.transform.position + Vector3.up;
                Boom(victim, at);
                Send(Msg(OpBoom).Byte(victim.NetId).Vec3(at));
                foreach (var c in Alive())
                    if (c != victim) Award(c, SurviveCash);
                MiniGames.HostEliminate(victim);
                nextBombAt = Time.time + 2.5f;
                return;
            }

            // Pass by touch
            if (Time.time < passReadyAt) return;
            Vector3 hp = holder.transform.position;
            foreach (var c in Alive())
            {
                if (c == holder) continue;
                if (c == lastHolder && Time.time < tagBackUntil) continue;
                Vector3 d = c.transform.position - hp;
                if (Shapes.Flat(d).magnitude > PassRange || Mathf.Abs(d.y) > 1.8f) continue;
                HostPass(c);
                break;
            }
        }

        public override void HostOnLaunched(Contestant victim, Contestant attacker, float knock)
        {
            // A shove from the holder passes it too.
            if (attacker != null && attacker == holder && victim != null && !victim.Eliminated && Time.time >= passReadyAt - 0.4f)
                HostPass(victim);
        }

        void HostPass(Contestant to)
        {
            var from = holder;
            SetHolder(to, fuseEnd);
            lastHolder = from;
            tagBackUntil = Time.time + TagBack;
            passReadyAt = Time.time + PassCooldown;
            Send(Msg(OpPass).Byte(to.NetId));
        }

        void Boom(Contestant victim, Vector3 at)
        {
            MiniKit.BoomFx(at, 3.2f);
            Puffs.Confetti(at + Vector3.up, 30);
            SetHolder(null, 0f);
            if (victim != null && victim.IsPlayer) ThirdPersonCamera.Shake(0.8f);
        }

        public override void OnMessage(byte op, NetReader r)
        {
            switch (op)
            {
                case OpNewBomb:
                {
                    var c = ById(r.Byte());
                    float end = r.Float();
                    fuseLength = r.Float();
                    SetHolder(c, end);
                    break;
                }
                case OpPass:
                    SetHolder(ById(r.Byte()), fuseEnd);
                    break;
                case OpBoom:
                {
                    var c = ById(r.Byte());
                    Boom(c, r.Vec3());
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ HUD

        public override string Status(RaceManager race) =>
            holder != null && !holder.Eliminated ? "BOMB: " + holder.DisplayName.ToUpperInvariant() + "     STILL IN: " + race.AliveCount
                                                 : "STILL IN: " + race.AliveCount;

        public override string Banner(Contestant player, out Color color)
        {
            color = UiSkin.Red;
            if (player == holder && holder != null && !player.Eliminated)
                return Mathf.Repeat(Time.time * 3f, 1f) < 0.6f ? "YOU HAVE THE BOMB! TAG SOMEONE!" : "";
            return null;
        }

        public override string StatusFor(Contestant c) => c == holder ? "HAS THE BOMB" : null;

        // ------------------------------------------------------------------ bots

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            if (holder == null || holder.Eliminated) return null;
            if (me == holder)
            {
                // Chase the nearest dummy that isn't the one we just got it from.
                Contestant best = null;
                float bestD = float.MaxValue;
                foreach (var c in Alive())
                {
                    if (c == me || (c == lastHolder && Time.time < tagBackUntil)) continue;
                    float d = Vector3.Distance(pos, c.transform.position);
                    if (d < bestD) { bestD = d; best = c; }
                }
                return best != null ? best.transform.position : (Vector3?)null;
            }
            // Run away from the holder (but not off the edge).
            Vector3 away = Shapes.Flat(pos - holder.transform.position);
            if (away.magnitude > 10f) return null;
            Vector3 goal = pos + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.right) * 6f;
            Vector3 off = Shapes.Flat(goal - Origin);
            if (off.magnitude > 11f) goal = Origin + Quaternion.Euler(0f, 70f, 0f) * off.normalized * 9f;
            return goal;
        }
    }
}
