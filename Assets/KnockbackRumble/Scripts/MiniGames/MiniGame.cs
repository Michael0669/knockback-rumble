using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// One mini-game of the tournament. Each game builds its own arena (on demand, the first time
    /// it's played), sets its rules (weapons, eliminations, race to a finish line...), runs its
    /// hazards and scoring, and tells the bots what to do.
    ///
    /// How a game stays in sync online, with almost no extra messages:
    ///  - Hazards run on the shared clock (Net.Time) from the round's seed, so every machine sees
    ///    the same thing at the same time. Each machine applies hits to its OWN dummies only
    ///    (DentBody.Hit does this automatically).
    ///  - Anything that needs one answer (who grabbed the coin, who has the bomb, who's out) is
    ///    decided by the HOST from everyone's positions and sent as a Mini message.
    ///  - Cash goes through ShowManager.HostAward like everything else.
    /// </summary>
    public abstract class MiniGame
    {
        // =====================================================================
        // Identity (shown in menus and on the loading screen)
        // =====================================================================

        public abstract EventKind Kind { get; }
        public abstract string Title { get; }
        /// <summary>The whole game in one line (loading screen).</summary>
        public abstract string Goal { get; }
        /// <summary>A slightly longer explanation (countdown panel).</summary>
        public abstract string Objective { get; }
        /// <summary>Short description for the tournament picker.</summary>
        public abstract string Blurb { get; }
        /// <summary>Three rule cards: { big text, small text } each.</summary>
        public abstract string[,] Cards { get; }
        public virtual float Duration => 90f;

        // =====================================================================
        // Rules
        // =====================================================================

        /// <summary>Falling off knocks you out of this game (you watch from the lounge).</summary>
        public virtual bool FallsEliminate => false;
        /// <summary>People get knocked out somehow (falls, bombs, musical chairs...). Survivors get bonuses at the end.</summary>
        public virtual bool HasEliminations => FallsEliminate;
        /// <summary>The game ends early when only one dummy is left.</summary>
        public virtual bool EndWhenOneLeft => HasEliminations;
        /// <summary>Race to a FinishLine (finish payouts like the Gauntlet). The course must run along +Z.</summary>
        public virtual bool IsRace => false;
        /// <summary>Loadout guns and gadgets. If false, everyone only has the shove.</summary>
        public virtual bool WeaponsAllowed => true;
        public virtual bool StreaksAllowed => WeaponsAllowed;
        /// <summary>Guns never run out of ammo in this game.</summary>
        public virtual bool InfiniteAmmo => false;
        /// <summary>Charge the usual fall penalty.</summary>
        public virtual bool FallPenalty => true;
        /// <summary>Cash per second for everyone still in (games with eliminations).</summary>
        public virtual int StandingPerSecond => 0;
        /// <summary>What the cash popup says for this game's own payouts (Reason.Prize).</summary>
        public virtual string PrizeLabel => "PRIZE";

        // =====================================================================
        // State
        // =====================================================================

        public CourseInfo Course { get; private set; }
        public int Seed { get; private set; }
        /// <summary>Shared clock time of GO.</summary>
        public float StartTime { get; private set; }
        /// <summary>Seconds since GO (negative during the countdown).</summary>
        public float Clock => Net.Time - StartTime;
        /// <summary>World position of the arena's origin.</summary>
        public Vector3 Origin { get; internal set; }

        protected static RaceManager Race => RaceManager.Instance;
        protected static ShowManager Show => ShowManager.Instance;
        protected static bool Racing => Race != null && Race.State == RaceManager.Phase.Racing;
        protected Vector3 W(Vector3 local) => Origin + local;
        protected Vector3 W(float x, float y, float z) => Origin + new Vector3(x, y, z);

        internal void StartGame(CourseInfo course, int seed, float start)
        {
            Course = course;
            Seed = seed;
            StartTime = start;
            Begin();
        }

        // =====================================================================
        // Hooks
        // =====================================================================

        /// <summary>Build the arena under root (root sits at Origin). Fill in spawns, center, radius, lounge...</summary>
        public abstract void Build(Transform root, CourseInfo info);

        /// <summary>The game is starting (every machine, after everyone has spawned).</summary>
        public virtual void Begin() { }
        /// <summary>Every frame on every machine, from the countdown until the next game.</summary>
        public virtual void Tick() { }
        /// <summary>Host only, once a second while playing.</summary>
        public virtual void HostSecond(RaceManager race) { }
        /// <summary>Host only, every frame while playing.</summary>
        public virtual void HostFrame(RaceManager race) { }
        /// <summary>Host only: end the game now?</summary>
        public virtual bool ShouldEnd(RaceManager race) => false;
        /// <summary>Host only: time's up, pay the end-of-game prizes.</summary>
        public virtual void HostRoundEnd(RaceManager race) { }
        /// <summary>The game is over (every machine). Put things back.</summary>
        public virtual void End() { }
        /// <summary>A Mini message from the host for this game.</summary>
        public virtual void OnMessage(byte op, NetReader r) { }
        /// <summary>Host: someone fell off (attacker = who knocked them off, if anyone).</summary>
        public virtual void HostOnFell(Contestant victim, Contestant attacker) { }
        /// <summary>Host: someone got launched (by a player, or a hazard when attacker is null).</summary>
        public virtual void HostOnLaunched(Contestant victim, Contestant attacker, float knock) { }
        /// <summary>Owner of each dummy: set it up for this game (after the loadout is applied).</summary>
        public virtual void SetupLocal(Contestant c)
        {
            if (!WeaponsAllowed) c.Weapons.Clear();
        }
        /// <summary>Below this height you've fallen off.</summary>
        public virtual float KillY => Course != null ? Course.killY : -12f;

        // =====================================================================
        // HUD
        // =====================================================================

        /// <summary>Line under the top bar.</summary>
        public virtual string Status(RaceManager race) => null;
        /// <summary>Big message in the middle of the screen for this player (or null).</summary>
        public virtual string Banner(Contestant player, out Color color)
        {
            color = UiSkin.Gold;
            return null;
        }
        /// <summary>Status column on the Tab scoreboard.</summary>
        public virtual string StatusFor(Contestant c) => null;

        // =====================================================================
        // Bots
        // =====================================================================

        /// <summary>Where this bot should head (null = wander around the middle).</summary>
        public virtual Vector3? BotGoal(Contestant me, Vector3 pos, float skill) => null;
        /// <summary>Stand still this frame.</summary>
        public virtual bool BotHold(Contestant me, float skill) => false;
        /// <summary>Extra per-frame bot behaviour (jumping hazards etc.).</summary>
        public virtual void BotExtra(Contestant me, float skill, float dt) { }

        // =====================================================================
        // Helpers
        // =====================================================================

        protected static IEnumerable<Contestant> Everyone()
        {
            var race = Race;
            if (race == null) yield break;
            for (int i = 0; i < race.Contestants.Count; i++)
                if (race.Contestants[i] != null) yield return race.Contestants[i];
        }

        protected static IEnumerable<Contestant> Alive()
        {
            foreach (var c in Everyone()) if (!c.Eliminated && !c.Finished) yield return c;
        }

        protected static IEnumerable<Contestant> LocalAlive()
        {
            foreach (var c in Everyone()) if (c.IsLocal && !c.Eliminated && !c.Finished) yield return c;
        }

        protected static Contestant ById(byte id) => MatchController.Instance != null ? MatchController.Instance.Find(id) : null;

        protected void Award(Contestant c, int amount)
        {
            if (Show != null) Show.HostAward(c, amount, ShowManager.Reason.Prize);
        }

        /// <summary>Host: start a Mini message for this game. Add the payload, then Send it.</summary>
        protected NetWriter Msg(byte op) => MiniGames.Writer.Begin(MsgType.Mini).Byte((byte)Kind).Byte(op);

        protected static void Send(NetWriter w)
        {
            if (Net.Online && Net.IsHost) Net.Session.Send(w);
        }

        /// <summary>Is the local player close enough to hear/see this?</summary>
        protected static bool NearCamera(Vector3 p, float range)
        {
            var cam = Camera.main;
            return cam != null && (cam.transform.position - p).sqrMagnitude < range * range;
        }

        /// <summary>Stable random number in [0,1) for an index, from the round's seed.</summary>
        protected float Rand(int index, int salt = 0)
        {
            unchecked
            {
                uint h = (uint)(Seed * 73856093) ^ (uint)(index * 19349663) ^ (uint)(salt * 83492791);
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }

    /// <summary>All the mini-games, their arenas, and the one being played right now.</summary>
    public static class MiniGames
    {
        public const byte OpEliminate = 200;

        static readonly List<MiniGame> games = new List<MiniGame>
        {
            new HotPotatoGame(), new ThroneRoomGame(), new MakeItRainGame(), new GreasedSumoGame(), new SpinCycleGame(),
            new FloorIsLavaGame(), new TurfWarGame(), new RocketRodeoGame(), new ToiletRollGame(), new HoleInTheWallGame(),
            new PottyPanicGame(), new BirdBrainedGame(), new RedLightGame(), new BalloonPopGame(), new TreadmillGame(),
            new BottomlessClipGame(), new ShotgunWeddingGame(), new GoneWithTheWindGame(), new DuckSeasonGame(), new MoonBaseGame(),
            new PinballGame(), new ColorCrazyGame(), new WheelOfMisfortuneGame(), new SkyHighGame(), new MinefieldGame(),
            new BouncyCastleGame(), new SwirlyGame(),
        };

        public static readonly NetWriter Writer = new NetWriter();

        /// <summary>The mini-game being played (null for the classic three events or between shows).</summary>
        public static MiniGame Current { get; private set; }

        public static IReadOnlyList<MiniGame> All => games;

        public static MiniGame Get(EventKind kind)
        {
            for (int i = 0; i < games.Count; i++) if (games[i].Kind == kind) return games[i];
            return null;
        }

        /// <summary>Where each mini-game's arena lives: a grid far from the classic arenas.</summary>
        public static Vector3 OriginFor(MiniGame g)
        {
            int i = games.IndexOf(g);
            return new Vector3(-900f + (i % 5) * 450f, 0f, 900f + (i / 5) * 450f);
        }

        /// <summary>The arena for an event, built the first time it's needed.</summary>
        public static CourseInfo EnsureArena(EventKind kind)
        {
            var found = CourseInfo.Find(kind);
            if (found != null) return found;
            var game = Get(kind);
            if (game == null) return null;
            game.Origin = OriginFor(game);
            var root = new GameObject("MiniGame_" + kind).transform;
            root.position = game.Origin;
            var info = root.gameObject.AddComponent<CourseInfo>();
            info.kind = kind;
            info.courseName = game.Title;
            info.center = game.Origin;
            info.radius = 15f;
            info.killY = game.Origin.y - 12f;
            info.loungePoint = game.Origin + new Vector3(0f, 9f, 26f);
            game.Build(root, info);
            return info;
        }

        /// <summary>Show only this event's arena (everything else is switched off to save drawing it). Null = the Gauntlet (menus).</summary>
        public static void ShowArena(EventKind? kind)
        {
            EventKind show = kind ?? EventKind.Gauntlet;
            foreach (var c in CourseInfo.All)
                if (c != null) c.gameObject.SetActive(c.kind == show);
        }

        /// <summary>A new event started (every machine). Starts the mini-game if it is one.</summary>
        public static void Begin(EventKind kind, CourseInfo course, int seed, float start)
        {
            EndCurrent();
            var game = Get(kind);
            if (game == null || course == null) return;
            game.Origin = course.transform.position;
            Current = game;
            WeaponHolder.InfiniteAmmo = game.InfiniteAmmo;
            game.StartGame(course, seed, start);
        }

        public static void EndCurrent()
        {
            var g = Current;
            Current = null;
            if (g != null) g.End();
            SfxRunner.MuteGameMusic = false;
            WeaponHolder.InfiniteAmmo = false;
        }

        /// <summary>Host: knock someone out of the current game (bombs, musical chairs...). Tells everyone.</summary>
        public static void HostEliminate(Contestant c)
        {
            var race = RaceManager.Instance;
            if (c == null || race == null || c.Eliminated || !Net.IsHost || Current == null) return;
            race.Eliminate(c);
            if (Net.Online) Net.Session.Send(Writer.Begin(MsgType.Mini).Byte((byte)Current.Kind).Byte(OpEliminate).Byte(c.NetId));
        }

        /// <summary>A Mini message arrived.</summary>
        public static void OnMessage(NetReader r)
        {
            var kind = (EventKind)r.Byte();
            byte op = r.Byte();
            if (Current == null || Current.Kind != kind) return;
            if (op == OpEliminate)
            {
                var c = MatchController.Instance != null ? MatchController.Instance.Find(r.Byte()) : null;
                if (c != null && RaceManager.Instance != null) RaceManager.Instance.Eliminate(c);
                return;
            }
            Current.OnMessage(op, r);
        }
    }
}
