using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KnockbackRumble
{
    /// <summary>
    /// Runs one Race round: countdown, falls and respawns, checkpoints, finish
    /// order, qualification and the results screen. Press R (or Start) to rerun.
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        public enum Phase { Countdown, Racing, Results }

        [Tooltip("Share of contestants that qualify (8 players -> 5 qualify)")]
        [Range(0.1f, 1f)] public float qualifyShare = 0.625f;
        public float countdownTime = 5f;
        public float timeLimit = 150f;
        [Tooltip("A fall counts as a knock-off if someone hit you within this many seconds")]
        public float knockoffCreditTime = 4f;

        public Phase State { get; private set; } = Phase.Countdown;
        public float Clock { get; private set; }
        public int QualifySlots { get; private set; }
        public CourseInfo Course { get; private set; }
        public Contestant Player { get; private set; }
        public readonly List<Contestant> Contestants = new List<Contestant>();
        public readonly List<Contestant> Finishers = new List<Contestant>();
        public readonly List<Contestant> Results = new List<Contestant>();

        bool saidCountdown;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Begin(CourseInfo course, List<Contestant> contestants)
        {
            Course = course;
            Contestants.Clear();
            Contestants.AddRange(contestants);
            Player = Contestants.Find(c => c.IsPlayer);
            QualifySlots = Mathf.Clamp(Mathf.RoundToInt(Contestants.Count * qualifyShare), 1, Contestants.Count);
            foreach (var c in Contestants) c.Dent.Launched += OnLaunched;
            StartCountdown();
        }

        void StartCountdown()
        {
            State = Phase.Countdown;
            Clock = countdownTime;
            saidCountdown = false;
            foreach (var c in Contestants) c.Motor.InputLocked = true;
            Announcer.Intro();
        }

        public void Restart()
        {
            Finishers.Clear();
            Results.Clear();
            for (int i = 0; i < Contestants.Count; i++)
            {
                var c = Contestants[i];
                c.ResetForRace(SpawnPoint(i));
                var bot = c.GetComponent<BotBrain>();
                if (bot != null) bot.ResetPath();
            }
            foreach (var p in WeaponPickup.All) p.SetAvailable(true);
            StartCountdown();
        }

        public Vector3 SpawnPoint(int i) => SpawnPointFor(Course, i);

        public Vector3 SpawnPointFor(CourseInfo course, int i)
        {
            if (course != null && course.spawnPoints != null && course.spawnPoints.Length > 0)
                return course.spawnPoints[i % course.spawnPoints.Length];
            return new Vector3(i * 2f, 1f, 0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (State)
            {
                case Phase.Countdown:
                    Clock -= dt;
                    if (!saidCountdown && Clock <= 3f) { Announcer.Countdown(); saidCountdown = true; }
                    if (Clock <= 0f)
                    {
                        State = Phase.Racing;
                        Clock = 0f;
                        foreach (var c in Contestants) c.Motor.InputLocked = false;
                        Announcer.Go();
                    }
                    break;

                case Phase.Racing:
                    Clock += dt;
                    CheckFalls();
                    if (Finishers.Count >= QualifySlots || Clock >= timeLimit) EndRace();
                    break;

                case Phase.Results:
                    CheckFalls();
                    var kb = Keyboard.current;
                    var pad = Gamepad.current;
                    if ((kb != null && kb.rKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame))
                        Restart();
                    break;
            }
        }

        void CheckFalls()
        {
            if (Course == null) return;
            foreach (var c in Contestants)
                if (c.transform.position.y < Course.killY) Fell(c);
        }

        void Fell(Contestant c)
        {
            c.Falls++;
            var dent = c.Dent;
            bool credited = dent.LastAttacker != null && Time.time - dent.LastHitTime < knockoffCreditTime;
            var attacker = credited ? dent.LastAttacker.GetComponent<Contestant>() : null;
            bool selfOwn = !credited && Time.time - dent.LastSelfHitTime < knockoffCreditTime;

            if (attacker != null && attacker != c) attacker.Knockoffs++;

            // Only narrate falls involving the player (or the occasional bot) so Rusty doesn't spam.
            bool playerInvolved = c.IsPlayer || (attacker != null && attacker.IsPlayer);
            if (State == Phase.Racing && (playerInvolved || Random.value < 0.25f))
                Announcer.Fell(c.IsPlayer, attacker != null && attacker.IsPlayer, attacker != null, selfOwn);

            if (!c.IsPlayer && c.Persona != null) c.Bark(VoiceLine.Pick(c.Persona.Fell));

            Vector3 p = c.RespawnPoint + new Vector3(Random.Range(-2f, 2f), 0.5f, Random.Range(-1f, 1f));
            c.Motor.Teleport(p, Quaternion.identity);
            c.Dent.ResetDent();
            var bot = c.GetComponent<BotBrain>();
            if (bot != null) bot.ResetPath();
        }

        void OnLaunched(DentBody body, float knock, GameObject attackerGo, string weapon)
        {
            var victim = body.GetComponent<Contestant>();
            if (victim == null || attackerGo == body.gameObject) return; // ignore rocket jumps
            var attacker = attackerGo != null ? attackerGo.GetComponent<Contestant>() : null;

            if (!victim.IsPlayer && victim.Persona != null && knock > 12f && Random.value < 0.7f)
                victim.Bark(VoiceLine.Pick(victim.Persona.Launched));

            bool playerInvolved = victim.IsPlayer || (attacker != null && attacker.IsPlayer);
            if (State == Phase.Racing && knock > 16f && (playerInvolved || Random.value < 0.15f))
                Announcer.BigLaunch(victim.IsPlayer, attacker != null && attacker.IsPlayer, attacker == null);
        }

        public void ReachCheckpoint(Contestant c, Checkpoint cp)
        {
            if (cp.index <= c.CheckpointIndex) return;
            c.CheckpointIndex = cp.index;
            c.RespawnPoint = cp.transform.position;
            if (c.IsPlayer) Announcer.ShowToast("Checkpoint, baby!");
        }

        public void ReachFinish(Contestant c)
        {
            if (State != Phase.Racing || c.Finished) return;
            c.Finished = true;
            Finishers.Add(c);
            c.FinishPlace = Finishers.Count;
            c.FinishTime = Clock;
            bool qualified = c.FinishPlace <= QualifySlots;
            if (qualified && (c.IsPlayer || Random.value < 0.4f)) Announcer.Qualified(c.IsPlayer);
            if (c.IsPlayer) Announcer.ShowToast(qualified ? "QUALIFIED! #" + c.FinishPlace : "Too slow!");
            if (!c.IsPlayer && c.Persona != null) c.Bark(VoiceLine.Pick(c.Persona.Finished));
        }

        void EndRace()
        {
            State = Phase.Results;
            Results.Clear();
            Results.AddRange(Finishers);
            var rest = Contestants.FindAll(c => !c.Finished);
            rest.Sort((a, b) => b.transform.position.z.CompareTo(a.transform.position.z));
            Results.AddRange(rest);
            foreach (var c in rest) c.Motor.InputLocked = true; // eliminated dummies freeze where they stand
            Announcer.RaceOver(IsQualified(Player));
        }

        public bool IsQualified(Contestant c) => c != null && c.Finished && c.FinishPlace <= QualifySlots;
    }
}
