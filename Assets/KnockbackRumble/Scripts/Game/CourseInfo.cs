using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>The kinds of event a show is made of.</summary>
    public enum EventKind : byte
    {
        Gauntlet = 0,   // obstacle course; cash for finishing position
        CashGrab = 1,   // hold money bags to earn; get hit and you drop them
        Crumble = 2,    // floor falls apart; cash for every second you survive
    }

    public static class Events
    {
        public static string Title(EventKind k)
        {
            switch (k)
            {
                case EventKind.CashGrab: return "CASH GRAB";
                case EventKind.Crumble: return "CRUMBLE PLAZA";
                default: return "THE GAUNTLET";
            }
        }

        public static string Objective(EventKind k)
        {
            switch (k)
            {
                case EventKind.CashGrab: return "Grab a money bag and hold it. $10 a second. Get hit and you drop it!";
                case EventKind.Crumble: return "The floor is falling apart. $5 a second while you're standing. Fall and you're out!";
                default: return "Reach the finish. First across gets the biggest payout. Knock rivals off for $100!";
            }
        }

        /// <summary>The rules, one short line each, for the loading screen.</summary>
        public static string[] Rules(EventKind k)
        {
            string ko = "Knock someone off: +$" + ShowManager.KnockoffCash + ".   Fall off: -$" + ShowManager.FallPenalty + ".";
            switch (k)
            {
                case EventKind.CashGrab:
                    return new[]
                    {
                        ShowManager.BagCount + " money bags sit on the floor under gold beams. Touch one to grab it.",
                        "Hold a bag for +$" + ShowManager.BagPerSecond + " every second. One bag per person.",
                        "Take a big hit and the bag pops loose. Shoot the carriers and steal their income!",
                        ko + "   No rails, so watch the edge.",
                        "Ends when the clock runs out.",
                    };
                case EventKind.Crumble:
                    return new[]
                    {
                        "Tiles flash orange, then drop out. Keep moving onto solid ground.",
                        "+$" + ShowManager.StandingPerSecond + " every second you're still standing.",
                        "Fall ONCE and you're out. You watch the rest from the Losers' Lounge.",
                        "Last ones standing get $" + ShowManager.SurvivorPayout[0] + " / $" + ShowManager.SurvivorPayout[1] +
                            " / $" + ShowManager.SurvivorPayout[2] + ".   Knock-offs still pay +$" + ShowManager.KnockoffCash + ".",
                        "Ends when one dummy is left or the clock runs out.",
                    };
                default:
                    return new[]
                    {
                        "Run the obstacle course to the finish line.",
                        "First four across get $" + ShowManager.FinishPayout[0] + " / $" + ShowManager.FinishPayout[1] + " / $" +
                            ShowManager.FinishPayout[2] + " / $" + ShowManager.FinishPayout[3] + ". Everyone else who finishes gets $" +
                            ShowManager.FinishOther + ".",
                        ko + "   You respawn at your last checkpoint.",
                        "Ends when 5 dummies finish or the clock runs out.",
                    };
            }
        }

        /// <summary>One tip per event for the loading screen.</summary>
        public static string Tip(EventKind k)
        {
            switch (k)
            {
                case EventKind.CashGrab: return "Getting hit pays a little \"pain pay\", so being a target isn't all bad.";
                case EventKind.Crumble: return "The outside goes first. Shove people toward the holes.";
                default: return "Rocket Launcher? Shoot your feet and jump to reach the purple Express Lane.";
            }
        }

        /// <summary>How long the event lasts (seconds after GO).</summary>
        public static float Duration(EventKind k)
        {
            switch (k)
            {
                case EventKind.CashGrab: return 100f;
                case EventKind.Crumble: return 110f;
                default: return 150f;
            }
        }
    }

    /// <summary>Data every arena exposes: spawns, bot paths, fall-out height, event extras.</summary>
    public class CourseInfo : MonoBehaviour
    {
        public static readonly List<CourseInfo> All = new List<CourseInfo>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        /// <summary>The Gauntlet course (kept for older code paths and the menu camera).</summary>
        public static CourseInfo Current => Find(EventKind.Gauntlet);

        public static CourseInfo Find(EventKind kind)
        {
            foreach (var c in All) if (c != null && c.kind == kind) return c;
            return null;
        }

        public string courseName = "Scrap Sprint";
        public EventKind kind = EventKind.Gauntlet;
        public Vector3[] spawnPoints;
        [Tooltip("The main route, in order. Bots run from point to point (Gauntlet only).")]
        public Vector3[] waypoints;
        public float killY = -12f;

        [Header("Arenas")]
        public Vector3 center;
        public float radius = 20f;
        [Tooltip("Cash Grab: where money bags drop.")]
        public Vector3[] bagSpawns;
        [Tooltip("Crumble Plaza: where knocked-out players wait.")]
        public Vector3 loungePoint;

        void Awake() { if (!All.Contains(this)) All.Add(this); }
        void OnDestroy() => All.Remove(this);

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            if (waypoints != null)
                for (int i = 0; i < waypoints.Length; i++)
                {
                    Gizmos.DrawSphere(waypoints[i] + Vector3.up * 0.5f, 0.3f);
                    if (i > 0) Gizmos.DrawLine(waypoints[i - 1] + Vector3.up * 0.5f, waypoints[i] + Vector3.up * 0.5f);
                }
            Gizmos.color = Color.green;
            if (spawnPoints != null) foreach (var s in spawnPoints) Gizmos.DrawWireSphere(s + Vector3.up, 0.5f);
            Gizmos.color = Color.yellow;
            if (bagSpawns != null) foreach (var s in bagSpawns) Gizmos.DrawWireCube(s + Vector3.up, Vector3.one);
        }
    }
}
