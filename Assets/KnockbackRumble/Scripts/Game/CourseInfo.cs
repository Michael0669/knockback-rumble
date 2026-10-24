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

        // Mini-games (see the MiniGames folder)
        HotPotato = 3,
        ThroneRoom = 4,
        MakeItRain = 5,
        GreasedSumo = 6,
        SpinCycle = 7,
        FloorIsLava = 8,
        TurfWar = 9,
        RocketRodeo = 10,
        ToiletRoll = 11,
        HoleInTheWall = 12,
        PottyPanic = 13,
        BirdBrained = 14,
        RedLight = 15,
        BalloonPop = 16,
        Treadmill = 17,
        BottomlessClip = 18,
        ShotgunWedding = 19,
        GoneWithTheWind = 20,
        DuckSeason = 21,
        MoonBase = 22,
        Pinball = 23,
        ColorCrazy = 24,
        WheelOfMisfortune = 25,
        SkyHigh = 26,
        Minefield = 27,
        BouncyCastle = 28,
        Swirly = 29,
    }

    public static class Events
    {
        /// <summary>Every game that can be put in a tournament, in menu order.</summary>
        public static readonly EventKind[] All =
        {
            EventKind.Gauntlet, EventKind.CashGrab, EventKind.Crumble,
            EventKind.HotPotato, EventKind.ThroneRoom, EventKind.MakeItRain, EventKind.GreasedSumo, EventKind.SpinCycle,
            EventKind.FloorIsLava, EventKind.TurfWar, EventKind.RocketRodeo, EventKind.ToiletRoll, EventKind.HoleInTheWall,
            EventKind.PottyPanic, EventKind.BirdBrained, EventKind.RedLight, EventKind.BalloonPop, EventKind.Treadmill,
            EventKind.BottomlessClip, EventKind.ShotgunWedding, EventKind.GoneWithTheWind, EventKind.DuckSeason, EventKind.MoonBase,
            EventKind.Pinball, EventKind.ColorCrazy, EventKind.WheelOfMisfortune, EventKind.SkyHigh, EventKind.Minefield,
            EventKind.BouncyCastle, EventKind.Swirly,
        };

        /// <summary>Short description for the tournament picker.</summary>
        public static string Blurb(EventKind k)
        {
            var g = MiniGames.Get(k);
            if (g != null) return g.Blurb;
            switch (k)
            {
                case EventKind.CashGrab: return "Grab the money bags and don't let go.";
                case EventKind.Crumble: return "The floor falls apart under you.";
                default: return "Obstacle course race to the finish.";
            }
        }

        public static string Title(EventKind k)
        {
            var g = MiniGames.Get(k);
            if (g != null) return g.Title;
            switch (k)
            {
                case EventKind.CashGrab: return "CASH GRAB";
                case EventKind.Crumble: return "CRUMBLE PLAZA";
                default: return "THE GAUNTLET";
            }
        }

        public static string Objective(EventKind k)
        {
            var g = MiniGames.Get(k);
            if (g != null) return g.Objective;
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
            var g = MiniGames.Get(k);
            if (g != null) return g.Goal;
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
            var g = MiniGames.Get(k);
            if (g != null) return g.Cards;
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
            var g = MiniGames.Get(k);
            if (g != null) return g.Duration;
            switch (k)
            {
                case EventKind.CashGrab: return 100f;
                case EventKind.Crumble: return 110f;
                default: return 200f;
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

        Transform finishLine;

        /// <summary>The Gauntlet: a spot past the finish line where this dummy walks to and celebrates.</summary>
        public Vector3 FinishParkPoint(Contestant c)
        {
            if (finishLine == null)
            {
                var line = GetComponentInChildren<FinishLine>(true);
                if (line == null) return c.transform.position;
                finishLine = line.transform;
            }
            int slot = c.NetId % 8, row = c.NetId / 8;
            return finishLine.position + new Vector3(-7f + slot * 1.9f, 0f, 5f + row * 2f);
        }

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
