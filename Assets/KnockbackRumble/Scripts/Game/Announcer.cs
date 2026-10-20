using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Rusty Nutz and Trina Tailpipe heckling the race out loud. Each line plays its
    /// audio clip (Resources/VO/{id}) and shows a subtitle. Lines are name-free so
    /// they work for anyone. This file doubles as the recording script.
    /// </summary>
    public static class Announcer
    {
        public const string Rusty = "RUSTY NUTZ";
        public const string Trina = "TRINA TAILPIPE";

        static readonly VoiceStyle RustyVoice = new VoiceStyle(false, 0, "low");
        static readonly VoiceStyle TrinaVoice = new VoiceStyle(true, 1, "medium");

        public static string Speaker { get; private set; }
        public static string Line { get; private set; }
        public static float LineTime { get; private set; } = -99f;
        public static string Toast { get; private set; }
        public static float ToastTime { get; private set; } = -99f;

        static AudioSource source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Speaker = null; Line = null; LineTime = -99f;
            Toast = null; ToastTime = -99f;
            source = null;
        }

        public static void ShowToast(string text)
        {
            Toast = text;
            ToastTime = Time.time;
        }

        /// <summary>Speak a random line from the set. Unforced lines never talk over the current one.</summary>
        public static void Say(VoiceLine[] set, bool force = false)
        {
            var line = VoiceLine.Pick(set);
            if (line == null || !VoiceDirector.MayAnnounce(force)) return;

            Speaker = line.speaker;
            Line = line.text;
            LineTime = Time.time;

            var clip = line.Clip;
            if (clip != null && source == null)
            {
                var go = new GameObject("AnnouncerVoice");
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // in your ears, not in the world
                source.priority = 0;
            }
            VoiceDirector.Play(source, clip, true);
        }

        // ---------- Event hooks (called by RaceManager) ----------

        public static void Intro(EventKind kind)
        {
            switch (kind)
            {
                case EventKind.CashGrab: Say(CashGrabIntroLines, true); break;
                case EventKind.Crumble: Say(CrumbleIntroLines, true); break;
                default: Say(IntroLines, true); break;
            }
        }

        public static void Streak(Streak streak, bool byPlayer)
        {
            if (streak == KnockbackRumble.Streak.WreckingBall) Say(WreckingBallLines, true);
            else Say(GaryStrikeLines, byPlayer);
        }
        public static void Countdown() => Say(CountdownLines);
        public static void Go() => Say(GoLines, true);

        public static void BigLaunch(bool playerHit, bool playerAttacked, bool hazard)
        {
            if (playerHit && hazard) Say(HazardHitLines);
            else if (playerHit) Say(PlayerLaunchedLines);
            else if (playerAttacked) Say(PlayerLaunchedSomeoneLines);
            else Say(BotLaunchedLines);
        }

        public static void Fell(bool isPlayer, bool byPlayer, bool bySomeone, bool selfOwn)
        {
            if (isPlayer && selfOwn) Say(SelfOwnLines, true);
            else if (isPlayer && bySomeone) Say(PlayerKnockedOffLines, true);
            else if (isPlayer) Say(PlayerSoloFallLines, true);
            else if (byPlayer) Say(PlayerKnockedSomeoneOffLines);
            else Say(BotFellLines);
        }

        public static void Qualified(bool isPlayer) => Say(isPlayer ? PlayerQualifiedLines : BotQualifiedLines, isPlayer);
        public static void RaceOver(bool playerQualified) => Say(playerQualified ? EndWinLines : EndLoseLines, true);

        // ---------- Script ----------

        static VoiceLine[] R(string key, params string[] t) => VoiceLine.Set("rusty_" + key, Rusty, RustyVoice, t);
        static VoiceLine[] T(string key, params string[] t) => VoiceLine.Set("trina_" + key, Trina, TrinaVoice, t);
        static VoiceLine[] Mix(params VoiceLine[][] sets)
        {
            var all = new List<VoiceLine>();
            foreach (var s in sets) all.AddRange(s);
            return all.ToArray();
        }

        static readonly VoiceLine[] IntroLines = Mix(
            R("intro",
                "Welcome back to Knockback Rumble, the only show the F C C begged us to cancel!",
                "I'm Rusty Nutz, I'm legally drunk, and this is The Gauntlet!",
                "Tonight's contestants signed a waiver none of 'em read. God bless America."),
            T("intro", "Rusty's been sober for... nope, I can smell him from the booth."));

        static readonly VoiceLine[] CashGrabIntroLines = Mix(
            R("intro_cashgrab",
                "Welcome to Cash Grab! Three bags of money, eight greedy idiots. Grab a bag and don't let go!",
                "Cash Grab, baby! Hold the bag, get paid. Get hit, drop the bag. It's basically the economy."),
            T("intro_cashgrab", "This money is real. Just kidding, it's Rusty's alimony."));

        static readonly VoiceLine[] CrumbleIntroLines = Mix(
            R("intro_crumble",
                "Crumble Plaza! Built by the lowest bidder, and folks, it shows!",
                "The floor is falling apart and so is my liver. Last dummy standing gets paid!"),
            T("intro_crumble", "If you fall, you go to the Losers' Lounge. The snacks are expired."));

        static readonly VoiceLine[] GaryStrikeLines = Mix(
            R("gary_strike",
                "Gary's coming in hot! Gary had Taco Bell! Everybody take cover!",
                "Incoming seagull! That's not rain, folks!"),
            T("gary_strike", "Gary, no! Not on the contestants! ...Okay, yes on the contestants."));

        static readonly VoiceLine[] WreckingBallLines = Mix(
            R("wrecking_ball",
                "Wrecking ball incoming on the rich kid! Eat the rich!",
                "Somebody's got too much money and the universe noticed!"),
            T("wrecking_ball", "That's what you get for being rich. Trickle down THAT."));

        static readonly VoiceLine[] CountdownLines = R("countdown",
            "Three... two... one... somebody's gonna cry!",
            "On your marks, get set, sign your organ donor cards!");

        static readonly VoiceLine[] GoLines = Mix(
            R("go", "Go! Run, you plastic bastards!", "Go go go! Insurance doesn't cover any of this!"),
            T("go", "And they're off! Just like Rusty's marriage!"));

        static readonly VoiceLine[] PlayerLaunchedLines = Mix(
            R("player_launched",
                "Holy shit, you're in orbit!",
                "Look at you fly! Farthest anything's flown since my third wife left.",
                "That's gonna leave a dent. Several dents. A whole dent family."),
            T("player_launched", "Somebody call your mama. Actually don't, she's watching."));

        static readonly VoiceLine[] PlayerLaunchedSomeoneLines = Mix(
            R("player_launches",
                "Oh, you dirty bastard! Send 'em to the shadow realm!",
                "You just launched that poor dummy harder than a crypto scam!"),
            T("player_launches", "Did you just rearrange their whole skeleton? Do dummies even have skeletons?"));

        static readonly VoiceLine[] HazardHitLines = Mix(
            R("hazard",
                "That thing's got more hits than my ex-wife's lawyer!",
                "You just got personally victimized by the scenery!"),
            T("hazard", "Doctor Lee Ability swears that passed inspection."));

        static readonly VoiceLine[] BotLaunchedLines = R("bot_launched",
            "Holy shit, that dummy is in orbit!",
            "Somebody's getting launched into next week!");

        static readonly VoiceLine[] PlayerKnockedSomeoneOffLines = Mix(
            R("player_knockoff",
                "You yeeted 'em off the course! Beautiful. Disgusting. Beautiful.",
                "And they're swimming with the scrap now! Courtesy of you!"),
            T("player_knockoff", "You just evicted that dummy. No thirty day notice!"));

        static readonly VoiceLine[] PlayerKnockedOffLines = Mix(
            R("player_knocked_off",
                "Somebody just bodied you! Pour one out!",
                "You got evicted! Get your plastic ass back up there!"),
            T("player_knocked_off", "Ooh, that's gotta hurt your feelings. And your everything else."));

        static readonly VoiceLine[] PlayerSoloFallLines = Mix(
            R("player_fell",
                "You fell off all by yourself. Nobody touched you, dumbass!",
                "Gravity: one. You: zero."),
            T("player_fell", "You just walked off the edge like it owed you money."));

        static readonly VoiceLine[] BotFellLines = Mix(
            R("bot_fell", "Another dummy eats shit!", "Splat! Somebody hose that off."),
            T("bot_fell", "And one more for the scrap pile!"));

        static readonly VoiceLine[] SelfOwnLines = R("self_own",
            "You rocket jumped straight into the void! Ten out of ten, would watch again.",
            "You blew yourself up! Most honest thing I've seen all night.");

        static readonly VoiceLine[] PlayerQualifiedLines = R("player_qualified",
            "You made it, you beautiful dented son of a gun!",
            "Look at you! You finished! Your parents might finally return your calls!");

        static readonly VoiceLine[] BotQualifiedLines = R("bot_qualified",
            "Another one crosses the line! Somebody hose 'em off!",
            "Across the line, and only slightly dented!");

        static readonly VoiceLine[] EndWinLines = Mix(
            R("end_win", "That's the event, and you're on top! Somebody check if they cheated."),
            T("end_win", "You're the richest dummy out there. Don't spend it all on lawyers."));

        static readonly VoiceLine[] EndLoseLines = Mix(
            R("end_lose",
                "That's the event! You're broke, but at least you're ugly.",
                "Time's up! Somebody's richer than you, and they want you to know it."),
            T("end_lose", "Aww, somebody's got more money than you. Anyway!"));

        /// <summary>Every announcer line, for voice generation and the recording script.</summary>
        public static IEnumerable<VoiceLine> AllLines()
        {
            var sets = new[]
            {
                IntroLines, CashGrabIntroLines, CrumbleIntroLines, GaryStrikeLines, WreckingBallLines, CountdownLines, GoLines, PlayerLaunchedLines, PlayerLaunchedSomeoneLines, HazardHitLines,
                BotLaunchedLines, PlayerKnockedSomeoneOffLines, PlayerKnockedOffLines, PlayerSoloFallLines, BotFellLines,
                SelfOwnLines, PlayerQualifiedLines, BotQualifiedLines, EndWinLines, EndLoseLines,
            };
            foreach (var set in sets)
                foreach (var line in set)
                    yield return line;
        }
    }
}
