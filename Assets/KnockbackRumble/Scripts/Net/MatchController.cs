using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KnockbackRumble
{
    /// <summary>
    /// Sets up and tears down shows and their events, offline or online, and is the bridge
    /// between gameplay and the network:
    ///  - keeps the show's roster (you, other players, bots) and spawns it for each event,
    ///    with the right owner for each dummy and the right loadout
    ///  - streams our dummies' positions ~20 times a second and feeds others' to RemoteDummy
    ///  - forwards shots, hits, gadgets, streaks, crate grabs, bags, launches, falls and finishes
    /// Who simulates what: every machine simulates its own player's dummy (so controls feel
    /// instant); the host also simulates the bots, owns the crates and bags, pays out cash
    /// and decides results.
    /// </summary>
    public class MatchController : MonoBehaviour
    {
        public static MatchController Instance { get; private set; }

        public const int RaceSize = 8;
        const float StateInterval = 0.05f;

        public static readonly Color[] HumanColors =
        {
            new Color(1f, 0.5f, 0.1f),      // orange
            new Color(0.15f, 0.8f, 0.95f),  // cyan
            new Color(0.55f, 0.95f, 0.2f),  // lime
            new Color(0.95f, 0.25f, 0.85f), // magenta
            new Color(1f, 0.92f, 0.2f),     // yellow
            new Color(0.95f, 0.95f, 0.95f), // white
            new Color(0.1f, 0.65f, 0.55f),  // teal
            new Color(0.9f, 0.15f, 0.2f),   // red
        };

        /// <summary>Fill empty spots with bots in online races (the setting lives on the session so everyone sees it).</summary>
        public bool FillWithBots
        {
            get => NetSession.Instance == null || NetSession.Instance.FillBots;
            set { if (NetSession.Instance != null) NetSession.Instance.SetFillBots(value); }
        }
        public int OfflineBots = 7;
        public float BotSkillMin = 0.35f, BotSkillMax = 0.85f;
        public string OfflineName = "Dummy 69";

        public bool InMatch => race != null && race.State != RaceManager.Phase.Idle;
        public Contestant LocalPlayer { get; private set; }

        struct RosterEntry
        {
            public byte netId;
            public ulong owner;
            public bool bot;
            public byte style;   // persona index (bots) or color index (humans)
            public string name;
        }

        readonly Dictionary<byte, Contestant> byId = new Dictionary<byte, Contestant>();
        /// <summary>Who is in the current show. Kept between events so ids (and cash) stay the same.</summary>
        readonly List<RosterEntry> showRoster = new List<RosterEntry>();
        ShowManager show;
        readonly List<WeaponPickup> crates = new List<WeaponPickup>();
        readonly NetWriter writer = new NetWriter();
        RaceManager race;
        CourseInfo course;
        ThirdPersonCamera cam;
        float nextStateSend;
        bool hooked;

        void Awake()
        {
            Instance = this;
            race = GetComponent<RaceManager>();
            if (race == null) race = gameObject.AddComponent<RaceManager>();
            show = GetComponent<ShowManager>();
            if (show == null) show = gameObject.AddComponent<ShowManager>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            var s = NetSession.Instance;
            if (s != null && hooked)
            {
                s.GameMessage -= OnGameMessage;
                s.PeerLeft -= OnPeerLeft;
                s.SessionEnded -= OnSessionEnded;
                s.OwnerOf = null;
            }
        }

        public void Init(CourseInfo courseInfo, ThirdPersonCamera camera)
        {
            course = courseInfo;
            cam = camera;

            // Number the crates by position so every machine agrees which is which.
            crates.Clear();
            crates.AddRange(WeaponPickup.All);
            crates.Sort((a, b) =>
            {
                int z = a.transform.position.z.CompareTo(b.transform.position.z);
                return z != 0 ? z : a.transform.position.x.CompareTo(b.transform.position.x);
            });
            for (int i = 0; i < crates.Count; i++) crates[i].Index = i;

            var s = NetSession.Ensure();
            if (!hooked)
            {
                s.GameMessage += OnGameMessage;
                s.PeerLeft += OnPeerLeft;
                s.SessionEnded += OnSessionEnded;
                s.OwnerOf = OwnerOf;
                hooked = true;
            }
            ShowMenuCamera();
        }

        public Contestant Find(byte netId) => byId.TryGetValue(netId, out var c) ? c : null;

        public Contestant HumanFor(ulong peer)
        {
            foreach (var c in byId.Values)
                if (c != null && c.IsHuman && c.OwnerPeer == peer) return c;
            return null;
        }

        ulong OwnerOf(byte netId)
        {
            var c = Find(netId);
            return c != null ? c.OwnerPeer : Net.LocalId;
        }

        // =====================================================================
        // Starting and ending shows
        // =====================================================================

        /// <summary>A new show against bots.</summary>
        public void StartOffline()
        {
            if (Net.Online) return;
            showRoster.Clear();
            showRoster.Add(new RosterEntry { netId = 0, owner = Net.LocalId, bot = false, style = 0, name = OfflineName });
            AddBots(showRoster, OfflineBots);
            StartRound(0);
        }

        /// <summary>Host: start a new show with everyone in the lobby (plus bots).</summary>
        public void HostStartRace()
        {
            var s = Net.Session;
            if (!Net.Online || !s.IsHost) return;

            showRoster.Clear();
            foreach (var p in s.Peers)
                showRoster.Add(new RosterEntry { netId = (byte)showRoster.Count, owner = p.Id, bot = false, style = p.Color, name = p.Name });
            if (FillWithBots) AddBots(showRoster, RaceSize - showRoster.Count);
            StartRound(0);
        }

        /// <summary>Host / offline: the next event of the current show.</summary>
        public void HostNextRound()
        {
            if (!Net.IsHost || show.IsLastRound || showRoster.Count == 0) return;
            StartRound(show.RoundIndex + 1);
        }

        void StartRound(int round)
        {
            var kind = ShowManager.KindFor(round);
            int seed = Random.Range(1, int.MaxValue);
            // Loading screen (rules + controls), then the countdown, then GO.
            float start = Net.Time + ShowManager.LoadingTimeFor(round) + race.countdownTime;
            if (Net.Online)
            {
                var w = writer.Begin(MsgType.StartRace).Float(start).Byte((byte)kind).Byte((byte)round).Int(seed)
                    .Byte((byte)showRoster.Count);
                foreach (var e in showRoster) w.Byte(e.netId).ULong(e.owner).Bool(e.bot).Byte(e.style).Str(e.name);
                Net.Session.Send(w);
            }
            BeginRace(showRoster, start, kind, round, seed);
        }

        /// <summary>Host / offline: R on the results screen. Skips the wait, or starts a new show after the last event.</summary>
        public void NextRace()
        {
            if (!Net.IsHost) return;
            if (!show.ShowOver && !show.IsLastRound && showRoster.Count > 0)
            {
                HostNextRound();
                return;
            }
            if (Net.Online) HostStartRace();
            else StartOffline();
        }

        /// <summary>Host: everyone back to the lobby screen.</summary>
        public void HostBackToLobby()
        {
            if (!Net.Online || !Net.IsHost) return;
            Net.Session.Send(writer.Begin(MsgType.BackToLobby));
            EndMatch();
        }

        void AddBots(List<RosterEntry> roster, int count)
        {
            var personas = new List<int>();
            for (int i = 0; i < Personas.Bots.Length; i++) personas.Add(i);
            for (int i = 0; i < count; i++)
            {
                int pick = personas.Count > 0 ? personas[Random.Range(0, personas.Count)] : i % Personas.Bots.Length;
                personas.Remove(pick);
                roster.Add(new RosterEntry
                {
                    netId = (byte)roster.Count, owner = Net.LocalId, bot = true, style = (byte)pick, name = Personas.Bots[pick].Name
                });
            }
        }

        void BeginRace(List<RosterEntry> roster, float raceStart, EventKind kind, int round, int seed)
        {
            ClearContestants();
            Gadgets.ClearAll();
            foreach (var p in crates) if (p != null) p.ResetCrate();

            var eventCourse = CourseInfo.Find(kind);
            if (eventCourse == null) { eventCourse = course; kind = EventKind.Gauntlet; }
            show.BeginRound(kind, round, eventCourse, seed);

            var list = new List<Contestant>();
            for (int i = 0; i < roster.Count; i++)
            {
                var e = roster[i];
                bool local = e.owner == Net.LocalId;
                Persona persona = e.bot ? Personas.Bots[e.style % Personas.Bots.Length] : null;
                Color color = e.bot ? persona.Color : HumanColors[e.style % HumanColors.Length];
                bool isPlayer = !e.bot && local;

                var c = Contestant.Create(e.name, color, isPlayer, persona, race.SpawnPointFor(eventCourse, i), Quaternion.identity,
                    e.netId, e.owner, local, !e.bot);

                if (isPlayer)
                {
                    c.gameObject.AddComponent<PlayerInputDriver>().Init(c, cam);
                    cam.SetTarget(c.transform);
                    LocalPlayer = c;
                }
                if (e.bot && local)
                    c.gameObject.AddComponent<BotBrain>().Init(c, eventCourse, Random.Range(BotSkillMin, BotSkillMax));

                // Loadouts: yours from your profile, bots get a random one. Owners tell everyone else.
                if (local)
                {
                    c.ApplyLoadout(isPlayer ? Profile.Loadout : Loadouts.RandomForBot(new System.Random(seed + e.netId * 7919)));
                    if (Net.Online)
                    {
                        var w = writer.Begin(MsgType.Loadout).Byte(c.NetId);
                        c.Loadout.Write(w);
                        Net.Session.Send(w);
                    }
                }

                byId[e.netId] = c;
                list.Add(c);
            }

            // Joined mid-show and not in this one? Watch the first dummy.
            if (LocalPlayer == null && list.Count > 0) cam.SetTarget(list[0].transform);

            race.Begin(eventCourse, list, raceStart, kind, seed);
        }

        /// <summary>Stop the current race (if any) and go back to the menu/lobby view.</summary>
        public void EndMatch()
        {
            if (this == null) return; // being destroyed (quitting)
            ClearContestants();
            race.Clear();
            show.Clear();
            Gadgets.ClearAll();
            showRoster.Clear();
            foreach (var p in crates) if (p != null) p.ResetCrate();
            ShowMenuCamera();
            MenuUI.SetPaused(false);
        }

        void ClearContestants()
        {
            foreach (var c in byId.Values)
                if (c != null) Destroy(c.gameObject);
            byId.Clear();
            LocalPlayer = null;
            if (cam != null) cam.SetTarget(null);
        }

        void ShowMenuCamera()
        {
            if (cam == null) return;
            cam.SetTarget(null);
            DriftMenuCamera();
        }

        /// <summary>Behind the menus the camera slowly drifts over the course, like a TV crane shot.</summary>
        void LateUpdate()
        {
            if (!InMatch) DriftMenuCamera();
        }

        void DriftMenuCamera()
        {
            if (cam == null) return;
            float t = Time.unscaledTime;
            float along = 0.5f + 0.5f * Mathf.Sin(t * 0.045f);          // creeps up and down the course
            var pos = new Vector3(Mathf.Sin(t * 0.08f) * 16f, 15f + Mathf.Sin(t * 0.23f) * 1.5f, Mathf.Lerp(-18f, 70f, along));
            var look = new Vector3(Mathf.Sin(t * 0.06f + 1f) * 4f, 2f, pos.z + 45f);
            cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos, Vector3.up));
        }

        void OnSessionEnded() => EndMatch();

        void OnPeerLeft(ulong peer)
        {
            var gone = new List<byte>();
            foreach (var kv in byId)
                if (kv.Value != null && kv.Value.OwnerPeer == peer) gone.Add(kv.Key);
            foreach (var id in gone)
            {
                var c = byId[id];
                race.RemoveContestant(c);
                byId.Remove(id);
                Destroy(c.gameObject);
            }
            showRoster.RemoveAll(e => e.owner == peer);
        }

        // =====================================================================
        // Per frame
        // =====================================================================

        void Update()
        {
            if (!InMatch) return;

            if (Net.Online && Time.unscaledTime >= nextStateSend)
            {
                nextStateSend = Time.unscaledTime + StateInterval;
                SendLocalStates();
            }

            if (race.State == RaceManager.Phase.Results && Net.IsHost && !MenuUI.Paused)
            {
                var kb = Keyboard.current;
                var pad = Gamepad.current;
                if ((kb != null && kb.rKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame))
                    NextRace();
                else if (Net.Online && kb != null && kb.lKey.wasPressedThisFrame)
                    HostBackToLobby();
            }
        }

        void SendLocalStates()
        {
            int count = 0;
            foreach (var c in byId.Values) if (c != null && c.IsLocal) count++;
            if (count == 0) return;

            var w = writer.Begin(MsgType.State).Float(Net.Time).Byte((byte)count);
            foreach (var c in byId.Values)
            {
                if (c == null || !c.IsLocal) continue;
                var m = c.Motor;
                byte flags = 0;
                if (m.Grounded) flags |= RemoteDummy.FlagGrounded;
                if (m.Stunned) flags |= RemoteDummy.FlagStunned;
                if (m.Diving) flags |= RemoteDummy.FlagDiving;
                var held = c.Weapons.Held;
                w.Byte(c.NetId)
                 .Vec3(c.transform.position)
                 .Float(c.transform.eulerAngles.y)
                 .Vec3(m.Velocity)
                 .Byte(flags)
                 .Byte((byte)Mathf.Clamp(Mathf.RoundToInt(c.Dent.Dent), 0, 255))
                 .Byte(held != null ? held.netId : (byte)0);
            }
            Net.Session.Send(w);
        }

        // =====================================================================
        // Gameplay -> network (called by gameplay code; offline these just apply locally)
        // =====================================================================

        public void ReportFire(Contestant shooter, WeaponDef def, Vector3 origin, Vector3 dir)
        {
            if (!Net.Online || !shooter.IsLocal) return;
            Net.Session.Send(writer.Begin(MsgType.Fire).Byte(shooter.NetId).Byte(def.netId).Vec3(origin).Vec3(dir));
        }

        public void SendHit(DentBody victim, Vector3 dir, float knock, float dent, GameObject attacker, string weapon, float minLift)
        {
            if (!Net.Online) return;
            var v = victim.GetComponent<Contestant>();
            var a = attacker != null ? attacker.GetComponent<Contestant>() : null;
            if (v == null) return;
            var def = WeaponDef.ByName(weapon);
            var w = writer.Begin(MsgType.Hit)
                .Byte(v.NetId)
                .Byte(a != null ? a.NetId : NetProtocol.NoId)
                .Vec3(dir).Float(knock).Float(dent).Float(minLift)
                .Byte(def != null ? def.netId : (byte)0);
            if (Net.IsHost) Net.Session.SendTo(v.OwnerPeer, w);
            else Net.Session.Send(w); // the host forwards it to the owner
        }

        public void ReportLaunched(Contestant victim, Contestant attacker, float knock, bool hazard)
        {
            if (Net.Online)
                Net.Session.Send(writer.Begin(MsgType.Launched)
                    .Byte(victim.NetId).Byte(attacker != null ? attacker.NetId : NetProtocol.NoId).Float(knock).Bool(hazard));
            race.HandleLaunched(victim, attacker, knock, hazard);
        }

        public void ReportFell(Contestant victim, Contestant attacker, bool selfOwn)
        {
            if (Net.Online)
                Net.Session.Send(writer.Begin(MsgType.Fell)
                    .Byte(victim.NetId).Byte(attacker != null ? attacker.NetId : NetProtocol.NoId).Bool(selfOwn));
            race.HandleFell(victim, attacker, selfOwn);
        }

        public void ReportFinish(Contestant c)
        {
            if (Net.IsHost) race.HostRegisterFinish(c);
            else Net.Session.Send(writer.Begin(MsgType.FinishReq).Byte(c.NetId));
        }

        public void BroadcastFinished(Contestant c, int place, float time)
        {
            if (Net.Online) Net.Session.Send(writer.Begin(MsgType.Finished).Byte(c.NetId).Byte((byte)place).Float(time));
        }

        public void BroadcastResults(List<Contestant> order)
        {
            if (!Net.Online) return;
            var w = writer.Begin(MsgType.Results).Byte((byte)order.Count);
            foreach (var c in order) w.Byte(c.NetId).Byte((byte)Mathf.Min(c.Falls, 255)).Byte((byte)Mathf.Min(c.Knockoffs, 255));
            Net.Session.Send(w);
        }

        public void RequestCrate(WeaponPickup crate, Contestant c)
        {
            if (Net.IsHost) HostTakeCrate(crate, c);
            else Net.Session.Send(writer.Begin(MsgType.CrateReq).Byte((byte)crate.Index).Byte(c.NetId));
        }

        void HostTakeCrate(WeaponPickup crate, Contestant c)
        {
            if (crate == null || c == null || !crate.Available) return;
            crate.Take(c);
            if (Net.Online)
                Net.Session.Send(writer.Begin(MsgType.CrateTaken).Byte((byte)crate.Index).Byte(c.NetId));
        }

        public void HostCrateRespawned(WeaponPickup crate)
        {
            if (Net.Online && Net.IsHost) Net.Session.Send(writer.Begin(MsgType.CrateBack).Byte((byte)crate.Index));
        }

        WeaponPickup Crate(int index) => index >= 0 && index < crates.Count ? crates[index] : null;

        // =====================================================================
        // Network -> gameplay
        // =====================================================================

        void OnGameMessage(ulong from, MsgType type, NetReader r)
        {
            switch (type)
            {
                case MsgType.StartRace:
                {
                    if (Net.IsHost) return;
                    float start = r.Float();
                    var kind = (EventKind)r.Byte();
                    int round = r.Byte();
                    int seed = r.Int();
                    int n = r.Byte();
                    showRoster.Clear();
                    for (int i = 0; i < n; i++)
                        showRoster.Add(new RosterEntry { netId = r.Byte(), owner = r.ULong(), bot = r.Bool(), style = r.Byte(), name = r.Str() });
                    if (round == 0) MenuUI.SetPaused(false);
                    BeginRace(showRoster, start, kind, round, seed);
                    break;
                }

                case MsgType.BackToLobby:
                    if (!Net.IsHost) EndMatch();
                    break;

                case MsgType.State:
                {
                    float t = r.Float();
                    int n = r.Byte();
                    for (int i = 0; i < n; i++)
                    {
                        byte id = r.Byte();
                        Vector3 pos = r.Vec3();
                        float yaw = r.Float();
                        Vector3 vel = r.Vec3();
                        byte flags = r.Byte();
                        byte dent = r.Byte();
                        byte weapon = r.Byte();
                        var c = Find(id);
                        if (c == null || c.IsLocal) continue;
                        var remote = c.GetComponent<RemoteDummy>();
                        if (remote != null) remote.Push(t, pos, yaw, vel, flags, dent, weapon);
                    }
                    break;
                }

                case MsgType.Fire:
                {
                    var c = Find(r.Byte());
                    var def = WeaponDef.ById(r.Byte());
                    Vector3 origin = r.Vec3();
                    Vector3 dir = r.Vec3();
                    if (c == null || c.IsLocal || def == null) break;
                    Weapons.FireCosmetic(def, c.gameObject, origin, dir);
                    if (def == WeaponDef.Shove) c.Visual.PlayShove();
                    break;
                }

                case MsgType.Hit:
                {
                    var victim = Find(r.Byte());
                    var attacker = Find(r.Byte());
                    Vector3 dir = r.Vec3();
                    float knock = r.Float();
                    float dent = r.Float();
                    float minLift = r.Float();
                    var def = WeaponDef.ById(r.Byte());
                    if (victim == null || !victim.IsLocal) break;
                    victim.Dent.Hit(dir, knock, dent, attacker != null ? attacker.gameObject : null,
                        def != null ? def.name : "Hit", minLift);
                    break;
                }

                case MsgType.Launched:
                {
                    var victim = Find(r.Byte());
                    var attacker = Find(r.Byte());
                    float knock = r.Float();
                    bool hazard = r.Bool();
                    if (victim != null) race.HandleLaunched(victim, attacker, knock, hazard);
                    break;
                }

                case MsgType.Fell:
                {
                    var victim = Find(r.Byte());
                    var attacker = Find(r.Byte());
                    bool selfOwn = r.Bool();
                    if (victim != null) race.HandleFell(victim, attacker, selfOwn);
                    break;
                }

                case MsgType.FinishReq:
                {
                    var c = Find(r.Byte());
                    if (Net.IsHost && c != null && c.OwnerPeer == from) race.HostRegisterFinish(c);
                    break;
                }

                case MsgType.Finished:
                {
                    var c = Find(r.Byte());
                    int place = r.Byte();
                    float time = r.Float();
                    if (c != null && !Net.IsHost) race.ApplyFinished(c, place, time);
                    break;
                }

                case MsgType.Results:
                {
                    if (Net.IsHost) break;
                    int n = r.Byte();
                    var order = new List<Contestant>(n);
                    for (int i = 0; i < n; i++)
                    {
                        var c = Find(r.Byte());
                        int falls = r.Byte();
                        int knockoffs = r.Byte();
                        if (c == null) continue;
                        c.Falls = falls;
                        c.Knockoffs = knockoffs;
                        order.Add(c);
                    }
                    race.ApplyResults(order);
                    break;
                }

                case MsgType.CrateReq:
                {
                    var crate = Crate(r.Byte());
                    var c = Find(r.Byte());
                    if (Net.IsHost && c != null && c.OwnerPeer == from) HostTakeCrate(crate, c);
                    break;
                }

                case MsgType.CrateTaken:
                {
                    var crate = Crate(r.Byte());
                    var c = Find(r.Byte());
                    if (crate != null && !Net.IsHost) crate.Take(c);
                    break;
                }

                case MsgType.CrateBack:
                {
                    var crate = Crate(r.Byte());
                    if (crate != null && !Net.IsHost) crate.SetAvailable(true);
                    break;
                }

                case MsgType.Loadout:
                {
                    var c = Find(r.Byte());
                    var loadout = Loadout.Read(r);
                    if (c != null && !c.IsLocal) c.Loadout = loadout;
                    break;
                }

                case MsgType.Gadget:
                {
                    var c = Find(r.Byte());
                    var gadget = (Gadget)r.Byte();
                    ushort id = r.UShort();
                    Vector3 pos = r.Vec3();
                    Vector3 vel = r.Vec3();
                    Gadgets.OnRemoteGadget(c, gadget, id, pos, vel);
                    break;
                }

                case MsgType.Boom:
                {
                    ushort id = r.UShort();
                    Vector3 pos = r.Vec3();
                    float radius = r.Float();
                    Gadgets.OnRemoteBoom(id, pos, radius);
                    break;
                }

                case MsgType.StreakCall:
                {
                    var c = Find(r.Byte());
                    var streak = (Streak)r.Byte();
                    int n = r.Byte();
                    var targets = new List<Vector3>(n);
                    var delays = new List<float>(n);
                    for (int i = 0; i < n; i++)
                    {
                        targets.Add(r.Vec3());
                        delays.Add(r.Float());
                    }
                    Gadgets.OnRemoteStreak(c, streak, targets, delays);
                    break;
                }

                case MsgType.Heckle:
                {
                    var fromC = Find(r.Byte());
                    var target = Find(r.Byte());
                    var kind = (HeckleKind)r.Byte();
                    if (fromC != null && !fromC.IsLocal) Heckles.Apply(fromC, target, kind);
                    break;
                }

                case MsgType.CashDelta:
                    if (!Net.IsHost) show.OnCashDelta(r);
                    break;

                case MsgType.BagReq:
                {
                    int index = r.Byte();
                    var c = Find(r.Byte());
                    if (Net.IsHost && c != null && c.OwnerPeer == from) show.OnBagRequest(index, c);
                    break;
                }

                case MsgType.Bag:
                {
                    int index = r.Byte();
                    byte carrierId = r.Byte();
                    Vector3 pos = r.Vec3();
                    bool carried = carrierId != NetProtocol.NoId;
                    show.OnBagMessage(index, carried ? Find(carrierId) : null, carried, pos);
                    break;
                }
            }
        }
    }
}
