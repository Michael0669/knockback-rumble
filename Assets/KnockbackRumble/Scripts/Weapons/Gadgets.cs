using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Gadgets (grenade, sticky mine) and Payout Streaks (Gary Strike, Wrecking Ball).
    /// The machine that owns the thrower simulates the real thing and applies hits;
    /// everyone else gets a look-alike that only does visuals. Explosions are announced
    /// with a Boom message so the look-alikes pop at the same moment.
    /// </summary>
    public static class Gadgets
    {
        // Balance numbers (tweak here)
        public const float GrenadeFuse = 1.6f, GrenadeSpeed = 17f, GrenadeRadius = 4.5f, GrenadeKnock = 17f, GrenadeDent = 16f;
        public const float MineRadius = 4f, MineTrigger = 2.2f, MineArm = 1f, MineKnock = 19f, MineDent = 18f;

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
            Vector3 vel;
            Vector3 pos;
            switch (gadget)
            {
                case Gadget.BouncerGrenade:
                    pos = origin;
                    vel = (aim.normalized + Vector3.up * 0.35f).normalized * GrenadeSpeed + c.Motor.Velocity * 0.5f;
                    Grenade.Spawn(c, id, pos, vel, false);
                    break;
                case Gadget.StickyMine:
                    pos = GroundBelow(c.transform.position + Vector3.up * 0.5f);
                    vel = Vector3.zero;
                    StickyMine.Spawn(c, id, pos, false);
                    break;
                default:
                    return;
            }
            if (Net.Online)
                Net.Session.Send(writer.Begin(MsgType.Gadget).Byte(c.NetId).Byte((byte)gadget).UShort(id).Vec3(pos).Vec3(vel));
        }

        /// <summary>Someone else threw a gadget: spawn the look-alike.</summary>
        public static void OnRemoteGadget(Contestant c, Gadget gadget, ushort id, Vector3 pos, Vector3 vel)
        {
            if (c == null || c.IsLocal) return;
            if (gadget == Gadget.BouncerGrenade) Grenade.Spawn(c, id, pos, vel, true);
            else if (gadget == Gadget.StickyMine) StickyMine.Spawn(c, id, pos, true);
        }

        /// <summary>Owner side: a gadget went off. Applies hits and tells everyone.</summary>
        public static void Detonate(ushort id, Vector3 pos, float radius, float knock, float dent, GameObject owner, string name)
        {
            Weapons.Explode(pos, radius, knock, dent, owner, name, 0.4f);
            Unregister(id);
            if (Net.Online) Net.Session.Send(writer.Begin(MsgType.Boom).UShort(id).Vec3(pos).Float(radius));
        }

        /// <summary>Remote side: show the explosion and remove the look-alike.</summary>
        public static void OnRemoteBoom(ushort id, Vector3 pos, float radius)
        {
            if (live.TryGetValue(id, out var go) && go != null) Object.Destroy(go);
            live.Remove(id);
            Fx.Explosion(pos, radius);
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

        // ---------- Payout Streaks ----------

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
                default:
                    return;
            }

            SpawnStreak(c, streak, targets, delays, false);
            if (c.IsPlayer) Announcer.ShowToast(Loadouts.StreakInfo(streak).name.ToUpperInvariant() + " INCOMING!");

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
            bool ball = streak == Streak.WreckingBall;
            for (int i = 0; i < targets.Count; i++)
                FallingBomb.Spawn(c.gameObject, targets[i], delays[i], ball, cosmetic);
            if (streak == Streak.GaryStrike && targets.Count > 0) FlyBy.Spawn(targets[0]);
            if (RaceManager.Instance != null) RaceManager.Instance.StreakCalled(c, streak);
        }
    }
}
