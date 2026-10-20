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

        /// <summary>The whole event in one line (loading screen).</summary>
        public static string Goal(EventKind k)
        {
            switch (k)
            {
                case EventKind.CashGrab: return "Grab a money bag and hold on to it.";
                case EventKind.Crumble: return "The floor falls apart. Don't fall.";
                default: return "Race to the finish line.";
            }
        }

        /// <summary>Three rule cards for the loading screen: { big text, small text } each.</summary>
        public static string[,] RuleCards(EventKind k)
        {
            switch (k)
            {
                case EventKind.CashGrab:
                    return new[,]
                    {
                        { "$" + ShowManager.BagPerSecond + " / SEC", "while you hold a bag" },
                        { "GET HIT", "and you drop it" },
                        { "+$" + ShowManager.KnockoffCash, "knock someone off" },
                    };
                case EventKind.Crumble:
                    return new[,]
                    {
                        { "$" + ShowManager.StandingPerSecond + " / SEC", "while you're standing" },
                        { "FALL = OUT", "one life only" },
                        { "$" + ShowManager.SurvivorPayout[0], "last one standing" },
                    };
                default:
                    return new[,]
                    {
                        { "$" + ShowManager.FinishPayout[0], "for 1st place" },
                        { "+$" + ShowManager.KnockoffCash, "knock someone off" },
                        { "-$" + ShowManager.FallPenalty, "when you fall off" },
                    };
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
