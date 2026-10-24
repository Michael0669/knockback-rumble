using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Gadgets (grenade, sticky mine) and Kill Streaks (Gary Strike, Wrecking Ball).
    /// The machine that owns the thrower simulates the real thing and applies hits;
    /// everyone else gets a look-alike that only does visuals. Explosions are announced
    /// with a Boom message so the look-alikes pop at the same moment.
    /// </summary>
    public static class Gadgets
    {
        // Balance numbers (tweak here)
        public const float GrenadeFuse = 1.6f, GrenadeSpeed = 17f, GrenadeRadius = 4.5f, GrenadeKnock = 17f, GrenadeDent = 16f;
        public const float MineRadius = 4f, MineTrigger = 2.2f, MineArm = 1f, MineKnock = 19f, MineDent = 18f;
        public const float StinkFuse = 1.3f, StinkRadius = 6.5f, StinkKnock = 9f, StinkDent = 26f;
        public const float BananaTrigger = 1.1f, BananaRadius = 2f, BananaKnock = 17f, BananaDent = 10f;
        public const float HornRadius = 7f, HornKnock = 19f, HornDent = 7f;
        public const float MoonBootsTime = 12f;

        static readonly Dictionary<ushort, GameObject> live = new Dictionary<ushort, GameObject>();
        static readonly NetWriter writer = new NetWriter();
        static int counter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { live.Clear(); counter = 0; }

        static ushort NextId(Contestant c) => (ushort)((c.NetId << 8) | (++counter & 0xFF));

        public static void Register(ushort id, GameObject go) { live[id] = go; }
        public static void Unregister(ushort id) { live.Remove(id); }

        /// <summary>Remove every gadget (new round).</summary>
        public static void ClearAll()
        {
            foreach (var go in live.Values) if (go != null) Object.Destroy(go);
            live.Clear();
        }

        // ---------- Gadgets ----------

        public static void Use(Contestant c, Gadget gadget, Vector3 origin, Vector3 aim)
        {
            ushort id = NextId(c);
            Vector3 vel = Vector3.zero;
            Vector3 pos;
            switch (gadget)
            {
                case Gadget.BouncerGrenade:
                case Gadget.StinkBomb:
                    pos = origin;
                    vel = (aim.normalized + Vector3.up * 0.35f).normalized * GrenadeSpeed + c.Motor.Velocity * 0.5f;
                    Grenade.Spawn(c, id, pos, vel, false, gadget == Gadget.StinkBomb);
                    break;
                case Gadget.StickyMine:
                case Gadget.BananaPeel:
                    pos = GroundBelow(c.transform.position + Vector3.up * 0.5f);
                    StickyMine.Spawn(c, id, pos, false, gadget == Gadget.BananaPeel);
                    break;
                case Gadget.BouncePad:
                    pos = GroundBelow(c.transform.position + Shapes.Flat(aim).normalized * 1.8f + Vector3.up * 1.5f);
                    BouncePad.Spawn(c, id, pos);
                    break;
                case Gadget.AirHorn:
                    pos = c.transform.position + Vector3.up * 0.9f;
                    Weapons.Explode(pos, HornRadius, HornKnock, HornDent, c.gameObject, "Air Horn", 0.35f, true, false);
                    HornFx(pos);
                    break;
                default:
                    return;
            }
            UseSound(gadget, pos);
            if (Net.Online)
                Net.Session.Send(writer.Begin(MsgType.Gadget).Byte(c.NetId).Byte((byte)gadget).UShort(id).Vec3(pos).Vec3(vel));
        }

        static void UseSound(Gadget gadget, Vector3 pos)
        {
            switch (gadget)
            {
                case Gadget.AirHorn: return; // HornFx plays it
                case Gadget.StickyMine:
                case Gadget.BananaPeel:
                case Gadget.BouncePad:
                    Sfx.At("mine_drop", pos, 1f, 0.08f, 35f);
                    break;
                default:
                    Sfx.At("throw", pos, 1f, 0.08f, 35f);
                    break;
            }
        }

        /// <summary>BWAAAAH: the horn and a shockwave ring.</summary>
        static void HornFx(Vector3 pos)
        {
            Sfx.At("airhorn", pos, 1f, 0.05f, 80f);
            Fx.Ring(pos - Vector3.up * 0.8f, HornRadius, new Color(0.95f, 0.3f, 0.3f));
            Fx.Ring(pos - Vector3.up * 0.4f, HornRadius * 0.6f, Palette.White);
            Fx.Burst(pos, Palette.White, 18, 12f, 0.2f);
        }

        /// <summary>Someone else threw a gadget: spawn the look-alike.</summary>
        public static void OnRemoteGadget(Contestant c, Gadget gadget, ushort id, Vector3 pos, Vector3 vel)
        {
            if (c == null || c.IsLocal) return;
            UseSound(gadget, pos);
            switch (gadget)
            {
                case Gadget.BouncerGrenade: Grenade.Spawn(c, id, pos, vel, true); break;
                case Gadget.StinkBomb: Grenade.Spawn(c, id, pos, vel, true, true); break;
                case Gadget.StickyMine: StickyMine.Spawn(c, id, pos, true); break;
                case Gadget.BananaPeel: StickyMine.Spawn(c, id, pos, true, true); break;
                case Gadget.BouncePad: BouncePad.Spawn(c, id, pos); break;
                case Gadget.AirHorn: HornFx(pos); break;
            }
        }

        /// <summary>Owner side: a gadget went off. Applies hits and tells everyone.</summary>
        public static void Detonate(ushort id, Vector3 pos, float radius, float knock, float dent, GameObject owner, string name,
            bool boomFx = true, float minLift = 0.4f)
        {
            Weapons.Explode(pos, radius, knock, dent, owner, name, minLift, false, boomFx);
            Unregister(id);
            if (Net.Online) Net.Session.Send(writer.Begin(MsgType.Boom).UShort(id).Vec3(pos).Float(radius));
        }

        /// <summary>Remote side: show the explosion and remove the look-alike.</summary>
        public static void OnRemoteBoom(ushort id, Vector3 pos, float radius)
        {
            bool boom = true;
            if (live.TryGetValue(id, out var go) && go != null)
            {
                // Stink bombs and banana peels have their own (non-explosion) effects.
                var g = go.GetComponent<Grenade>();
                var m = go.GetComponent<StickyMine>();
                if (g != null && g.Stink) { Grenade.StinkFx(pos); boom = false; }
                else if (m != null && m.Banana) { StickyMine.SlipFx(pos); boom = false; }
                Object.Destroy(go);
            }
            live.Remove(id);
            if (boom) Fx.Explosion(pos, radius);
        }

        public static Vector3 GroundBelow(Vector3 from)
        {
            float best = float.MaxValue;
            Vector3 point = from - Vector3.up * 0.5f;
            foreach (var hit in Physics.RaycastAll(from, Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<DentBody>() != null) continue;
                if (hit.distance < best) { best = hit.distance; point = hit.point; }
            }
            return point;
        }

        // ---------- Kill Streaks ----------

        /// <summary>Owner side: call in a streak. Picks targets, drops bombs, tells everyone.</summary>
        public static void CallStreak(Contestant c, Streak streak)
        {
            var targets = new List<Vector3>();
            var delays = new List<float>();
            var others = new List<Contestant>();
            if (RaceManager.Instance != null)
                foreach (var o in RaceManager.Instance.Contestants)
                    if (o != null && o != c && !o.Eliminated) others.Add(o);

            switch (streak)
            {
                case Streak.GaryStrike:
                {
                    // Six bombs spread across rivals, each landing where they were standing.
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 p = others.Count > 0
                            ? others[i % others.Count].transform.position + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f))
                            : c.transform.position + new Vector3(Random.Range(-12f, 12f), 0f, Random.Range(6f, 20f));
                        targets.Add(GroundBelow(p + Vector3.up * 3f));
                        delays.Add(0.9f + i * 0.35f);
                    }
                    break;
                }
                case Streak.WreckingBall:
                {
                    Contestant victim = ShowManager.Instance != null ? ShowManager.Instance.Leader(c) : null;
                    if (victim == null && others.Count > 0) victim = others[0];
                    Vector3 p = victim != null ? victim.transform.position : c.transform.position + c.transform.forward * 10f;
                    targets.Add(GroundBelow(p + Vector3.up * 3f));
                    delays.Add(1.6f);
                    break;
                }
                case Streak.CarpetBomb:
                {
                    // A line of bombs marching away from you, the way you're facing.
                    Vector3 f = Shapes.Flat(c.transform.forward).normalized;
                    if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 p = c.transform.position + f * (5f + i * 3.2f);
                        targets.Add(GroundBelow(p + Vector3.up * 6f));
                        delays.Add(0.8f + i * 0.18f);
                    }
                    break;
                }
                case Streak.AnvilDrop:
                {
                    // The two closest rivals.
                    others.Sort((a, b) => (a.transform.position - c.transform.position).sqrMagnitude
                        .CompareTo((b.transform.position - c.transform.position).sqrMagnitude));
                    for (int i = 0; i < Mathf.Min(2, others.Count); i++)
                    {
                        targets.Add(GroundBelow(others[i].transform.position + Vector3.up * 3f));
                        delays.Add(1.2f + i * 0.3f);
                    }
                    if (targets.Count == 0) { targets.Add(GroundBelow(c.transform.position + c.transform.forward * 8f + Vector3.up * 3f)); delays.Add(1.2f); }
                    break;
                }
                case Streak.TacticalToilet:
                {
                    foreach (var o in others)
                    {
                        targets.Add(GroundBelow(o.transform.position + Vector3.up * 3f));
                        delays.Add(1.8f + targets.Count * 0.15f);
                    }
                    if (targets.Count == 0) { targets.Add(GroundBelow(c.transform.position + c.transform.forward * 8f + Vector3.up * 3f)); delays.Add(1.8f); }
                    break;
                }
                case Streak.MoonBoots:
                    // No bombs: you just get floaty.
                    break;
                default:
                    return;
            }

            SpawnStreak(c, streak, targets, delays, false);
            if (c.IsPlayer) Announcer.ShowToast(Loadouts.StreakInfo(streak).name.ToUpperInvariant() + (streak == Streak.MoonBoots ? "!" : " INCOMING!"));

            if (Net.Online)
            {
                var w = writer.Begin(MsgType.StreakCall).Byte(c.NetId).Byte((byte)streak).Byte((byte)targets.Count);
                for (int i = 0; i < targets.Count; i++) w.Vec3(targets[i]).Float(delays[i]);
                Net.Session.Send(w);
            }
        }

        public static void OnRemoteStreak(Contestant c, Streak streak, List<Vector3> targets, List<float> delays)
        {
            if (c == null || c.IsLocal) return;
            SpawnStreak(c, streak, targets, delays, true);
        }

        static void SpawnStreak(Contestant c, Streak streak, List<Vector3> targets, List<float> delays, bool cosmetic)
        {
            var kind = streak == Streak.WreckingBall ? FallingBomb.Kind.Ball
                     : streak == Streak.AnvilDrop ? FallingBomb.Kind.Anvil
                     : streak == Streak.TacticalToilet ? FallingBomb.Kind.Toilet
                     : streak == Streak.CarpetBomb ? FallingBomb.Kind.Carpet
                     : FallingBomb.Kind.Gary;
            for (int i = 0; i < targets.Count; i++)
                FallingBomb.Spawn(c.gameObject, targets[i], delays[i], kind, cosmetic);
            if (streak == Streak.GaryStrike && targets.Count > 0) FlyBy.Spawn(targets[0]);
            if (streak == Streak.MoonBoots)
            {
                // Movement belongs to the owner, so only their machine changes gravity. Everyone sees the sparkle.
                if (!cosmetic) c.Motor.MoonBootsUntil = Time.time + MoonBootsTime;
                Sfx.At("double_jump", c.transform.position, 1f, 0f, 40f);
                Fx.Burst(c.transform.position + Vector3.up, new Color(0.7f, 0.75f, 1f), 24, 6f, 0.25f);
                c.gameObject.AddComponent<MoonBootsGlow>();
            }
            if (RaceManager.Instance != null) RaceManager.Instance.StreakCalled(c, streak);
        }
    }
}
