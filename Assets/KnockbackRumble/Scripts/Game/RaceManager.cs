using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Runs one Race round: countdown, falls and respawns, checkpoints, finish order,
    /// qualification and the results screen.
    ///
    /// Online, the HOST decides anything that must be fair (finish order, when the race
    /// ends, results). Each machine handles its own dummies' falls and checkpoints and
    /// reports them through MatchController; every machine then plays the commentary
    /// from its own player's point of view. Offline, this machine is the host.
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        public enum Phase { Idle, Countdown, Racing, Results }

        [Tooltip("Share of contestants that qualify (8 players -> 5 qualify)")]
        [Range(0.1f, 1f)] public float qualifyShare = 0.625f;
        public float countdownTime = 5f;
        public float timeLimit = 150f;
        [Tooltip("A fall counts as a knock-off if someone hit you within this many seconds")]
        public float knockoffCreditTime = 4f;

        public Phase State { get; private set; } = Phase.Idle;
        /// <summary>Countdown: seconds until GO. Racing/Results: seconds since GO.</summary>
        public float Clock { get; private set; }
        public float RaceStartTime { get; private set; }
        public int QualifySlots { get; private set; }
        public CourseInfo Course { get; private set; }
        public Contestant Player { get; private set; }
        public readonly List<Contestant> Contestants = new List<Contestant>();
        public readonly List<Contestant> Finishers = new List<Contestant>();
        public readonly List<Contestant> Results = new List<Contestant>();

        bool saidCountdown;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>raceStart is the shared clock time (Net.Time) when GO happens.</summary>
        public void Begin(CourseInfo course, List<Contestant> contestants, float raceStart)
        {
            Course = course;
            Contestants.Clear();
            Contestants.AddRange(contestants);
            Finishers.Clear();
            Results.Clear();
            Player = Contestants.Find(c => c.IsPlayer);
            QualifySlots = Mathf.Clamp(Mathf.RoundToInt(Contestants.Count * qualifyShare), 1, Mathf.Max(1, Contestants.Count));
            foreach (var c in Contestants)
            {
                c.Dent.Launched += OnLocalLaunched;
                c.Motor.InputLocked = true;
            }
            RaceStartTime = raceStart;
            Clock = raceStart - Net.Time;
            State = Phase.Countdown;
            saidCountdown = false;
            Announcer.Intro();
        }

        /// <summary>Back to no race (menu or lobby).</summary>
        public void Clear()
        {
            Contestants.Clear();
            Finishers.Clear();
            Results.Clear();
            Player = null;
            State = Phase.Idle;
        }

        public void RemoveContestant(Contestant c)
        {
            Contestants.Remove(c);
            Finishers.Remove(c);
            Results.Remove(c);
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
                        foreach (var c in Contestants) c.Motor.InputLocked = false;
                        Announcer.Go();
                    }
                    break;

                case Phase.Racing:
                    Clock = Net.Time - RaceStartTime;
                    CheckLocalFalls();
                    if (Net.IsHost && (Finishers.Count >= QualifySlots || Clock >= timeLimit)) HostEndRace();
                    break;

                case Phase.Results:
                    CheckLocalFalls();
                    break;
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
            var dent = c.Dent;
            bool credited = dent.LastAttacker != null && Time.time - dent.LastHitTime < knockoffCreditTime;
            var attacker = credited ? dent.LastAttacker.GetComponent<Contestant>() : null;
            bool selfOwn = !credited && Time.time - dent.LastSelfHitTime < knockoffCreditTime;

            Vector3 p = c.RespawnPoint + new Vector3(Random.Range(-2f, 2f), 0.5f, Random.Range(-1f, 1f));
            c.Motor.Teleport(p, Quaternion.identity);
            c.Dent.ResetDent();
            var bot = c.GetComponent<BotBrain>();
            if (bot != null) bot.ResetPath();

            if (MatchController.Instance != null) MatchController.Instance.ReportFell(c, attacker, selfOwn);
            else HandleFell(c, attacker, selfOwn);
        }

        /// <summary>A dummy fell (runs on every machine).</summary>
        public void HandleFell(Contestant victim, Contestant attacker, bool selfOwn)
        {
            victim.Falls++;
            if (attacker != null && attacker != victim) attacker.Knockoffs++;

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
            if (victim.Persona != null && knock > 12f && Random.value < 0.5f)
                victim.Bark(VoiceLine.Pick(victim.Persona.Launched));

            bool playerInvolved = victim.IsPlayer || (attacker != null && attacker.IsPlayer);
            if (State == Phase.Racing && knock > 16f && (playerInvolved || Random.value < 0.15f))
                Announcer.BigLaunch(victim.IsPlayer, attacker != null && attacker.IsPlayer, hazard);
        }

        // ---------- Checkpoints and finish ----------

        public void ReachCheckpoint(Contestant c, Checkpoint cp)
        {
            if (!c.IsLocal || cp.index <= c.CheckpointIndex) return;
            c.CheckpointIndex = cp.index;
            c.RespawnPoint = cp.transform.position;
            if (c.IsPlayer) Announcer.ShowToast("Checkpoint, baby!");
        }

        public void ReachFinish(Contestant c)
        {
            if (!c.IsLocal || State != Phase.Racing || c.Finished || c.FinishRequested) return;
            c.FinishRequested = true;
            if (MatchController.Instance != null) MatchController.Instance.ReportFinish(c);
            else HostRegisterFinish(c);
        }

        /// <summary>Host: someone crossed the line. Decides their place and tells everyone.</summary>
        public void HostRegisterFinish(Contestant c)
        {
            if (State != Phase.Racing || c.Finished) return;
            int place = Finishers.Count + 1;
            ApplyFinished(c, place, Clock);
            if (MatchController.Instance != null) MatchController.Instance.BroadcastFinished(c, place, Clock);
        }

        /// <summary>Runs on every machine.</summary>
        public void ApplyFinished(Contestant c, int place, float time)
        {
            if (c.Finished) return;
            c.Finished = true;
            c.FinishPlace = place;
            c.FinishTime = time;
            Finishers.Add(c);

            bool qualified = place <= QualifySlots;
            if (qualified && (c.IsPlayer || Random.value < 0.4f)) Announcer.Qualified(c.IsPlayer);
            if (c.IsPlayer) Announcer.ShowToast(qualified ? "QUALIFIED! #" + place : "Too slow!");
            if (c.Persona != null) c.Bark(VoiceLine.Pick(c.Persona.Finished));
        }

        // ---------- End ----------

        void HostEndRace()
        {
            var order = new List<Contestant>(Finishers);
            var rest = Contestants.FindAll(c => !c.Finished);
            rest.Sort((a, b) => b.transform.position.z.CompareTo(a.transform.position.z));
            order.AddRange(rest);
            ApplyResults(order);
            if (MatchController.Instance != null) MatchController.Instance.BroadcastResults(order);
        }

        /// <summary>Runs on every machine.</summary>
        public void ApplyResults(List<Contestant> order)
        {
            State = Phase.Results;
            Results.Clear();
            Results.AddRange(order);
            foreach (var c in Results)
                if (c.IsLocal && !IsQualified(c)) c.Motor.InputLocked = true; // eliminated dummies freeze where they stand
            if (Player != null) Announcer.RaceOver(IsQualified(Player));
        }

        public bool IsQualified(Contestant c) => c != null && c.Finished && c.FinishPlace <= QualifySlots;
    }
}
