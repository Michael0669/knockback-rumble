using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// One player or bot in the show: identity, voice pack, race progress, and
    /// references to the components that make up a dummy.
    /// </summary>
    public class Contestant : MonoBehaviour
    {
        public string DisplayName;
        public Color Color;
        /// <summary>The human playing on THIS machine.</summary>
        public bool IsPlayer;
        /// <summary>Any human (local or remote), as opposed to a bot.</summary>
        public bool IsHuman;
        public Persona Persona;

        // Networking: every dummy has a small id, and exactly one machine simulates it.
        public byte NetId;
        public ulong OwnerPeer;
        public bool IsLocal = true;

        public DummyMotor Motor { get; private set; }
        public DentBody Dent { get; private set; }
        public WeaponHolder Weapons { get; private set; }
        public DummyVisual Visual { get; private set; }

        // Race state
        public int CheckpointIndex = -1;
        public Vector3 RespawnPoint;
        public bool Finished;
        public bool FinishRequested;
        public int FinishPlace;
        public float FinishTime;

        // Stats for the results screen
        public int Falls;
        public int Knockoffs;

        // Loadout and Payout Streaks
        public Loadout Loadout = new Loadout();
        /// <summary>Knock-offs since this dummy last fell.</summary>
        public int StreakCount;
        /// <summary>Streaks earned but not called in yet (oldest first).</summary>
        public readonly System.Collections.Generic.List<Streak> ReadyStreaks = new System.Collections.Generic.List<Streak>();
        /// <summary>Out of the current round (Crumble Plaza): parked in the Losers' Lounge.</summary>
        public bool Eliminated;

        /// <summary>Arm this dummy with a loadout and apply its perks. Owner only.</summary>
        public void ApplyLoadout(Loadout loadout)
        {
            Loadout = loadout.Copy();
            Weapons.Equip(Loadout);
            Dent.KnockTakenMultiplier = Loadout.Has(Perk.SteelToes) ? 0.75f : 1f;
            Dent.StunMultiplier = Loadout.Has(Perk.GreaseMonkey) ? 0.6f : 1f;
            Motor.ExtraJumps = Loadout.Has(Perk.DoubleJump) ? 1 : 0;
            Motor.SpeedMultiplier = Loadout.Has(Perk.Lightweight) ? 1.12f : 1f;
        }

        /// <summary>A knock-off counted toward this dummy's streak. Returns a newly earned streak, if any.</summary>
        public Streak AddStreakKnockoff()
        {
            StreakCount++;
            foreach (var s in new[] { Loadout.streak1, Loadout.streak2 })
            {
                var info = Loadouts.StreakInfo(s);
                if (info != null && info.cost == StreakCount) { ReadyStreaks.Add(s); return s; }
            }
            return Streak.None;
        }

        /// <summary>Call in the oldest earned streak. Owner only. False if there's nothing to call (or it's not the time).</summary>
        public bool CallReadyStreak()
        {
            var race = RaceManager.Instance;
            if (ReadyStreaks.Count == 0 || Eliminated || race == null || race.State != RaceManager.Phase.Racing) return false;
            var s = ReadyStreaks[0];
            ReadyStreaks.RemoveAt(0);
            Gadgets.CallStreak(this, s);
            return true;
        }

        /// <summary>Knock-offs still needed for the next streak in the loadout (0 if none left to earn).</summary>
        public int KnockoffsToNextStreak(out Streak next)
        {
            next = Streak.None;
            int best = int.MaxValue;
            foreach (var s in new[] { Loadout.streak1, Loadout.streak2 })
            {
                var info = Loadouts.StreakInfo(s);
                if (info == null || info.cost <= StreakCount) continue;
                if (info.cost < best) { best = info.cost; next = s; }
            }
            return next == Streak.None ? 0 : best - StreakCount;
        }

        // Speech bubble
        public string BarkText { get; private set; }
        public float BarkTime { get; private set; } = -99f;

        public Vector3 HeadPosition => transform.position + Vector3.up * 2.1f;

        public static Contestant Create(string name, Color color, bool isPlayer, Persona persona, Vector3 position, Quaternion rotation,
            byte netId = 0, ulong ownerPeer = 1, bool isLocal = true, bool isHuman = false)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, rotation);

            var motor = go.AddComponent<DummyMotor>();   // also adds Rigidbody + CapsuleCollider
            var dent = go.AddComponent<DentBody>();
            var visual = go.AddComponent<DummyVisual>();
            visual.Build(color);
            var weapons = go.AddComponent<WeaponHolder>();

            var c = go.AddComponent<Contestant>();
            c.DisplayName = name;
            c.Color = color;
            c.IsPlayer = isPlayer;
            c.IsHuman = isHuman || isPlayer;
            c.Persona = persona;
            c.NetId = netId;
            c.OwnerPeer = ownerPeer;
            c.IsLocal = isLocal;
            dent.IsLocal = isLocal;
            c.Motor = motor;
            c.Dent = dent;
            c.Visual = visual;
            c.Weapons = weapons;
            c.RespawnPoint = position;
            if (!isLocal) go.AddComponent<RemoteDummy>().Init(c);
            return c;
        }

        AudioSource voice;
        float lastSpoke = -99f;

        /// <summary>Yell a voice-pack line out loud (3D audio, so you hear it near them).</summary>
        public void Bark(VoiceLine line)
        {
            if (line == null) return;
            if (Time.time - lastSpoke < 12f) return;           // each bot only pipes up now and then
            if (!VoiceDirector.MayBark(transform.position)) return;
            lastSpoke = Time.time;
            BarkText = line.text;
            BarkTime = Time.time;

            var clip = line.Clip;
            if (clip == null) return;
            if (voice == null)
            {
                voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.spatialBlend = 1f;
                voice.rolloffMode = AudioRolloffMode.Linear;
                voice.minDistance = 4f;
                voice.maxDistance = 40f;
                voice.dopplerLevel = 0f;
            }
            VoiceDirector.Play(voice, clip, false);
        }

        public void ResetForRace(Vector3 spawn)
        {
            Finished = false;
            FinishRequested = false;
            FinishPlace = 0;
            FinishTime = 0f;
            CheckpointIndex = -1;
            RespawnPoint = spawn;
            Falls = 0;
            Knockoffs = 0;
            BarkTime = -99f;
            lastSpoke = -99f;
            Motor.Teleport(spawn, Quaternion.identity);
            Dent.ResetDent();
            Weapons.Clear();
        }
    }
}
