using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Rusty Nutz (and occasionally Trina Tailpipe) heckling the race as subtitles.
    /// Text only for now; these become the recording script for voice lines later.
    /// Lines starting with "T:" are Trina's.
    /// </summary>
    public static class Announcer
    {
        public const string Rusty = "RUSTY NUTZ";
        public const string Trina = "TRINA TAILPIPE";

        public static string Speaker { get; private set; }
        public static string Line { get; private set; }
        public static float LineTime { get; private set; } = -99f;
        public static string Toast { get; private set; }
        public static float ToastTime { get; private set; } = -99f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Speaker = null; Line = null; LineTime = -99f;
            Toast = null; ToastTime = -99f;
        }

        public static void Say(string line, bool force = false)
        {
            if (string.IsNullOrEmpty(line)) return;
            if (!force && Time.time - LineTime < 1.6f) return; // let the last joke land
            if (line.StartsWith("T:")) { Speaker = Trina; Line = line.Substring(2).Trim(); }
            else { Speaker = Rusty; Line = line; }
            LineTime = Time.time;
        }

        public static void ShowToast(string text)
        {
            Toast = text;
            ToastTime = Time.time;
        }

        static string Pick(string[] lines) => lines[Random.Range(0, lines.Length)];

        static string Fill(string line, string victim, string attacker = null) =>
            line.Replace("{victim}", victim).Replace("{attacker}", attacker ?? "somebody");

        // ---------- Event hooks ----------

        public static void Intro() => Say(Pick(IntroLines), true);
        public static void Countdown() => Say(Pick(CountdownLines), true);
        public static void Go() => Say(Pick(GoLines), true);

        public static void BigLaunch(string victim, string attacker, string weapon)
        {
            if (attacker == null && weapon != null && HazardLines.Length > 0)
            {
                Say(Fill(Pick(HazardLines), victim).Replace("{hazard}", weapon.ToLowerInvariant()));
                return;
            }
            Say(Fill(Pick(LaunchLines), victim, attacker));
        }

        public static void Fell(string victim, string attacker, bool selfOwn)
        {
            if (selfOwn) Say(Fill(Pick(SelfOwnLines), victim), true);
            else if (attacker != null) Say(Fill(Pick(KnockoffLines), victim, attacker), true);
            else Say(Fill(Pick(SoloFallLines), victim));
        }

        public static void Qualified(string name, bool isPlayer) =>
            Say(Fill(Pick(isPlayer ? PlayerQualifiedLines : QualifiedLines), name), isPlayer);

        public static void RaceOver(bool playerQualified, string playerName) =>
            Say(Fill(Pick(playerQualified ? EndWinLines : EndLoseLines), playerName), true);

        // ---------- Script ----------

        static readonly string[] IntroLines =
        {
            "Welcome back to Knockback Rumble, the only show the FCC begged us to cancel!",
            "I'm Rusty Nutz, I'm legally drunk, and this is Scrap Sprint!",
            "T:Rusty's been sober for... nope, I can smell him from the booth.",
            "Tonight's contestants signed a waiver none of 'em read. God bless America.",
        };

        static readonly string[] CountdownLines =
        {
            "Three... two... one... somebody's gonna cry!",
            "On your marks, get set, sign your organ donor cards!",
        };

        static readonly string[] GoLines =
        {
            "GO! Run, you plastic bastards!",
            "GO GO GO! Insurance doesn't cover any of this!",
            "T:And they're off! Like Rusty's marriage!",
        };

        static readonly string[] LaunchLines =
        {
            "Holy shit, {victim} is in orbit!",
            "{attacker} just sent {victim} to the shadow realm!",
            "Look at {victim} fly! Farthest anything's flown since my third wife left.",
            "{victim} just got launched harder than a crypto scam!",
            "That's gonna leave a dent. Several dents. A whole dent family.",
            "T:{attacker} just rearranged {victim}'s whole skeleton. Do dummies have skeletons?",
            "Somebody call {victim}'s mama. Actually don't, she's watching.",
        };

        static readonly string[] HazardLines =
        {
            "That {hazard} has more hits than my ex-wife's lawyer!",
            "{victim} just got personally victimized by the {hazard}!",
            "T:Dr. Lee Ability swears that {hazard} passed inspection.",
            "The {hazard} sends its regards, {victim}!",
        };

        static readonly string[] KnockoffLines =
        {
            "{attacker} yeeted {victim} off the course! Beautiful. Disgusting. Beautiful.",
            "{victim} is swimming with the scrap now, courtesy of {attacker}!",
            "{attacker} just evicted {victim}. No thirty-day notice!",
            "T:{victim} got bodied by {attacker}! Somebody pour one out!",
        };

        static readonly string[] SoloFallLines =
        {
            "{victim} fell off all by themselves. Nobody touched you, dumbass!",
            "Gravity: one. {victim}: zero.",
            "And {victim} eats shit with zero help from anybody!",
            "T:{victim} just walked off the edge like it owed 'em money.",
        };

        static readonly string[] SelfOwnLines =
        {
            "{victim} rocket-jumped straight into the void! Ten outta ten, would watch again.",
            "{victim} blew themselves up! That's the most honest thing I've seen all night.",
        };

        static readonly string[] QualifiedLines =
        {
            "{victim} crosses the line! Qualified, and only slightly dented!",
            "{victim} is through! Somebody hose 'em off!",
        };

        static readonly string[] PlayerQualifiedLines =
        {
            "You made it, you beautiful dented son of a gun!",
            "Look at you! Qualified! Your parents might finally return your calls!",
        };

        static readonly string[] EndWinLines =
        {
            "That's the race! Losers, report to the crusher.",
            "Race over! {victim} lives to get dented another day!",
        };

        static readonly string[] EndLoseLines =
        {
            "Eliminated! Go home, {victim}, your family's embarrassed.",
            "That's it for {victim}. Don't let the scrap door hit your ass on the way out.",
            "T:Aww, {victim} got eliminated. Anyway!",
        };
    }
}
