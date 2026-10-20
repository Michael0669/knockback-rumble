using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Runs one event (round) of the show: countdown, falls and respawns, the event's
    /// win condition, and the results screen. Cash is handled by ShowManager.
    ///
    /// Events:
    ///  - The Gauntlet: obstacle course. Ends when enough dummies finish or time runs out.
    ///  - Cash Grab: arena with money bags. Ends when time runs out.
    ///  - Crumble Plaza: the floor falls apart. Falling eliminates you. Ends at one survivor or time.
    ///
    /// Online, the HOST decides anything that must be fair (finish order, cash, when the
    /// event ends). Each machine handles its own dummies' falls and checkpoints and reports
    /// them through MatchController; every machine then plays the commentary from its own
    /// player's point of view. Offline, this machine is the host.
    /// (The class keeps its old name so existing scenes keep working.)
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        public enum Phase { Idle, Countdown, Racing, Results }

        public float countdownTime = 5f;
        [Tooltip("The Gauntlet ends once this share of dummies has finished (8 dummies -> 5)")]
        [Range(0.1f, 1f)] public float finishShare = 0.625f;
        [Tooltip("A fall counts as a knock-off if someone hit you within this many seconds")]
        public float knockoffCreditTime = 4f;

        public Phase State { get; private set; } = Phase.Idle;
        /// <summary>Countdown: seconds until GO. Racing/Results: seconds since GO.</summary>
        public float Clock { get; private set; }
        public float RaceStartTime { get; private set; }
        public EventKind Kind { get; private set; }
        public int Seed { get; private set; }
        public float TimeLimit => Events.Duration(Kind);
        public float TimeLeft => Mathf.Max(0f, TimeLimit - Clock);
        /// <summary>Gauntlet: how many finishers end the event.</summary>
        public int FinishTarget { get; private set; }
        public CourseInfo Course { get; private set; }
        public Contestant Player { get; private set; }
        public float ResultsTime { get; private set; }
        public readonly List<Contestant> Contestants = new List<Contestant>();
        public readonly List<Contestant> Finishers = new List<Contestant>();
        /// <summary>Final standings of the event (sorted by show cash).</summary>
        public readonly List<Contestant> Results = new List<Contestant>();
        /// <summary>Crumble Plaza: dummies in the order they were knocked out.</summary>
        public readonly List<Contestant> Eliminations = new List<Contestant>();

        bool saidCountdown;
        CrumbleFloor crumble;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public int AliveCount
        {
            get
            {
                int n = 0;
                foreach (var c in Contestants) if (c != null && !c.Eliminated) n++;
                return n;
            }
        }

        /// <summary>raceStart is the shared clock time (Net.Time) when GO happens.</summary>
        public void Begin(CourseInfo course, List<Contestant> contestants, float raceStart, EventKind kind, int seed)
        {
            StopCrumble();
            Course = course;
            Kind = kind;
            Seed = seed;
            Contestants.Clear();
            Contestants.AddRange(contestants);
            Finishers.Clear();
            Results.Clear();
            Eliminations.Clear();
            Player = Contestants.Find(c => c.IsPlayer);
            FinishTarget = Mathf.Clamp(Mathf.RoundToInt(Contestants.Count * finishShare), 1, Mathf.Max(1, Contestants.Count));
            foreach (var c in Contestants)
            {
                c.Dent.Launched += OnLocalLaunched;
                c.Motor.InputLocked = true;
            }
            RaceStartTime = raceStart;
            Clock = raceStart - Net.Time;
            State = Phase.Countdown;
            saidCountdown = false;

            if (kind == EventKind.Crumble && course != null)
            {
                crumble = course.GetComponentInChildren<CrumbleFloor>(true);
                if (crumble != null) crumble.Begin(seed, raceStart, TimeLimit);
            }
            Announcer.Intro(kind);
        }

        /// <summary>Back to no event (menu or lobby).</summary>
        public void Clear()
        {
            StopCrumble();
            Contestants.Clear();
            Finishers.Clear();
            Results.Clear();
            Eliminations.Clear();
            Player = null;
            State = Phase.Idle;
        }

        void StopCrumble()
        {
            if (crumble != null) crumble.Stop();
            crumble = null;
        }

        public void RemoveContestant(Contestant c)
        {
            Contestants.Remove(c);
            Finishers.Remove(c);
            Results.Remove(c);
            Eliminations.Remove(c);
            if (Player == c) Player = null;
        }

        public Vector3 SpawnPointFor(CourseInfo course, int i)
        {
            if (course != null && course.spawnPoints != null && course.spawnPoints.Length > 0)
                return course.spawnPoints[i % course.spawnPoints.Length];
            return new Vector3(i * 2f, 1f, 0f);
        }

        void Update()
        {
            switch (State)
            {
                case Phase.Countdown:
                    Clock = RaceStartTime - Net.Time;
                    if (!saidCountdown && Clock <= 3f) { Announcer.Countdown(); saidCountdown = true; }
                    if (Clock <= 0f)
                    {
                        // Every machine starts on the shared clock, so nobody waits on lag to hear "GO".
                        State = Phase.Racing;
                        foreach (var c in Contestants) if (c != null) c.Motor.InputLocked = false;
                        Announcer.Go();
                    }
                    break;

                case Phase.Racing:
                    Clock = Net.Time - RaceStartTime;
                    CheckLocalFalls();
                    if (Net.IsHost && ShouldEnd()) HostEndRace();
                    break;

                case Phase.Results:
                    Clock = Net.Time - RaceStartTime;
                    CheckLocalFalls();
                    break;
            }
        }

        bool ShouldEnd()
        {
            if (Clock >= TimeLimit) return true;
            switch (Kind)
            {
                case EventKind.Gauntlet:
                    return Finishers.Count >= FinishTarget;
                case EventKind.Crumble:
                    return Contestants.Count >= 2 ? AliveCount <= 1 : AliveCount == 0;
                default:
                    return false;
            }
        }

        // ---------- Falls ----------

        void CheckLocalFalls()
        {
            if (Course == null) return;
            for (int i = 0; i < Contestants.Count; i++)
            {
                var c = Contestants[i];
                if (c != null && c.IsLocal && c.transform.position.y < Course.killY) LocalFell(c);
            }
        }

        void LocalFell(Contestant c)
        {
            var bot = c.GetComponent<BotBrain>();

            // Already out (Crumble Plaza)? Just put them back in the Losers' Lounge.
            if (c.Eliminated)
            {
                c.Motor.Teleport(LoungePoint(), Quaternion.identity);
                c.Dent.ResetDent();
                return;
            }

            var dent = c.Dent;
            bool credited = dent.LastAttacker != null && Time.time - dent.LastHitTime < knockoffCreditTime;
            var attacker = credited ? dent.LastAttacker.GetComponent<Contestant>() : null;
            bool selfOwn = !credited && Time.time - dent.LastSelfHitTime < knockoffCreditTime;

            bool eliminate = Kind == EventKind.Crumble && State == Phase.Racing;
            Vector3 p = eliminate ? LoungePoint() : RespawnFor(c);
            c.Motor.Teleport(p, Quaternion.identity);
            c.Dent.ResetDent();
            if (bot != null) bot.ResetPath();

            if (MatchController.Instance != null) MatchController.Instance.ReportFell(c, attacker, selfOwn);
            else HandleFell(c, attacker, selfOwn);
        }

        Vector3 RespawnFor(Contestant c)
        {
            if (Kind == EventKind.Gauntlet || Course == null || Course.spawnPoints == null || Course.spawnPoints.Length == 0)
                return c.RespawnPoint + new Vector3(Random.Range(-2f, 2f), 0.5f, Random.Range(-1f, 1f));
            // Arenas: drop back in at a random spawn point.
            return Course.spawnPoints[Random.Range(0, Course.spawnPoints.Length)] + Vector3.up * 0.5f;
        }

        Vector3 LoungePoint()
        {
            Vector3 basePos = Course != null ? Course.loungePoint : Vector3.zero;
            return basePos + new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(-1.5f, 1.5f));
        }

        /// <summary>A dummy fell (runs on every machine).</summary>
        public void HandleFell(Contestant victim, Contestant attacker, bool selfOwn)
        {
            if (victim == null) return;
            victim.Falls++;
            bool knockoff = attacker != null && attacker != victim;
            if (knockoff) attacker.Knockoffs++;
            victim.StreakCount = 0; // streaks are per life, like in the old military shooters

            if (State == Phase.Racing)
            {
                // Payout Streaks are counted by whoever owns the attacker.
                if (knockoff && attacker.IsLocal)
                {
                    var earned = attacker.AddStreakKnockoff();
                    if (earned != Streak.None && attacker.IsPlayer)
                    {
                        var info = Loadouts.StreakInfo(earned);
                        Announcer.ShowToast(info.name.ToUpperInvariant() + " READY!  Press Q");
                    }
                }

                if (Kind == EventKind.Crumble && !victim.Eliminated)
                {
                    victim.Eliminated = true;
                    Eliminations.Add(victim);
                    if (victim.IsLocal) victim.Weapons.Disabled = true;
                    if (victim.IsPlayer) Announcer.ShowToast("ELIMINATED!  Welcome to the Losers' Lounge");
                }

                if (Net.IsHost && ShowManager.Instance != null) ShowManager.Instance.HostOnFell(victim, knockoff ? attacker : null);
            }

            // Only narrate falls involving you (or the occasional other one) so Rusty doesn't spam.
            bool playerInvolved = victim.IsPlayer || (attacker != null && attacker.IsPlayer);
            if (State == Phase.Racing && (playerInvolved || Random.value < 0.25f))
                Announcer.Fell(victim.IsPlayer, attacker != null && attacker.IsPlayer, attacker != null, selfOwn);

            if (victim.Persona != null) victim.Bark(VoiceLine.Pick(victim.Persona.Fell));
        }

        // ---------- Launches ----------

        void OnLocalLaunched(DentBody body, float knock, GameObject attackerGo, string weapon)
        {
            var victim = body.GetComponent<Contestant>();
            if (victim == null || attackerGo == body.gameObject) return; // ignore rocket jumps
            var attacker = attackerGo != null ? attackerGo.GetComponent<Contestant>() : null;
            bool hazard = attackerGo == null;
            if (MatchController.Instance != null) MatchController.Instance.ReportLaunched(victim, attacker, knock, hazard);
            else HandleLaunched(victim, attacker, knock, hazard);
        }

        /// <summary>A dummy got launched (runs on every machine).</summary>
        public void HandleLaunched(Contestant victim, Contestant attacker, float knock, bool hazard)
        {
            if (victim == null) return;
            if (State == Phase.Racing && Net.IsHost && ShowManager.Instance != null)
                ShowManager.Instance.HostOnLaunched(victim, attacker, knock, hazard);

            if (victim.Persona != null && knock > 12f && Random.value < 0.5f)
                victim.Bark(VoiceLine.Pick(victim.Persona.Launched));

            bool playerInvolved = victim.IsPlayer || (attacker != null && attacker.IsPlayer);
            if (State == Phase.Racing && knock > 16f && (playerInvolved || Random.value < 0.15f))
                Announcer.BigLaunch(victim.IsPlayer, attacker != null && attacker.IsPlayer, hazard);
        }

        // ---------- Payout Streaks ----------

        /// <summary>Someone called in a streak (runs on every machine).</summary>
        public void StreakCalled(Contestant caller, Streak streak)
        {
            var info = Loadouts.StreakInfo(streak);
            if (caller == null || info == null) return;
            if (!caller.IsPlayer) Announcer.ShowToast(caller.DisplayName + " called in a " + info.name.ToUpperInvariant() + "!");
            Announcer.Streak(streak, caller.IsPlayer);
        }

        // ---------- Checkpoints and finish (The Gauntlet) ----------

        public void ReachCheckpoint(Contestant c, Checkpoint cp)
        {
            if (Kind != EventKind.Gauntlet || !c.IsLocal || cp.index <= c.CheckpointIndex) return;
            c.CheckpointIndex = cp.index;
            c.RespawnPoint = cp.transform.position;
            if (c.IsPlayer) Announcer.ShowToast("Checkpoint, baby!");
        }

        public void ReachFinish(Contestant c)
        {
            if (Kind != EventKind.Gauntlet || !c.IsLocal || State != Phase.Racing || c.Finished || c.FinishRequested) return;
            c.FinishRequested = true;
            if (MatchController.Instance != null) MatchController.Instance.ReportFinish(c);
            else HostRegisterFinish(c);
        }

        /// <summary>Host: someone crossed the line. Decides their place and tells everyone.</summary>
        public void HostRegisterFinish(Contestant c)
        {
            if (State != Phase.Racing || c == null || c.Finished) return;
            int place = Finishers.Count + 1;
            ApplyFinished(c, place, Clock);
            if (MatchController.Instance != null) MatchController.Instance.BroadcastFinished(c, place, Clock);
            if (ShowManager.Instance != null) ShowManager.Instance.HostOnFinish(c, place);
        }

        /// <summary>Runs on every machine.</summary>
        public void ApplyFinished(Contestant c, int place, float time)
        {
            if (c.Finished) return;
            c.Finished = true;
            c.FinishPlace = place;
            c.FinishTime = time;
            Finishers.Add(c);

            if (c.IsPlayer || Random.value < 0.4f) Announcer.Qualified(c.IsPlayer);
            if (c.IsPlayer) Announcer.ShowToast("FINISHED #" + place + "!");
            if (c.Persona != null) c.Bark(VoiceLine.Pick(c.Persona.Finished));
        }

        // ---------- End ----------

        void HostEndRace()
        {
            var show = ShowManager.Instance;
            if (show != null) show.HostOnRoundEnd(this);
            var order = show != null ? show.Ranking(Contestants) : new List<Contestant>(Contestants);
            ApplyResults(order);
            if (MatchController.Instance != null) MatchController.Instance.BroadcastResults(order);
        }

        /// <summary>Runs on every machine.</summary>
        public void ApplyResults(List<Contestant> order)
        {
            State = Phase.Results;
            ResultsTime = Time.time;
            Results.Clear();
            Results.AddRange(order);
            var show = ShowManager.Instance;
            if (show != null) show.OnRoundResults(this);
            if (Player != null) Announcer.RaceOver(Results.Count > 0 && Results[0] == Player);
        }

        /// <summary>1-based standing in the event results (0 if not listed).</summary>
        public int PlaceOf(Contestant c)
        {
            int i = Results.IndexOf(c);
            return i < 0 ? 0 : i + 1;
        }
    }
}
