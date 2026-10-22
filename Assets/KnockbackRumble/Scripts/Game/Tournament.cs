using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// The host's tournament settings: either a hand-picked list of games (played in the order
    /// they were picked) or "Randomize" with a number of games. Saved between sessions.
    /// Most cash at the end of the last game wins the tournament.
    /// </summary>
    public static class Tournament
    {
        public const int MinGames = 1, MaxGames = 20, DefaultGames = 10;
        const string Key = "kr_tournament_v1";

        [System.Serializable]
        public class Data
        {
            public bool random = true;
            public int count = DefaultGames;
            public List<int> picks = new List<int>();
        }

        static Data data;

        static Data D
        {
            get
            {
                if (data != null) return data;
                try { data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(Key, "")); }
                catch { data = null; }
                if (data == null) data = new Data();
                if (data.picks == null) data.picks = new List<int>();
                data.count = Mathf.Clamp(data.count, MinGames, MaxGames);
                return data;
            }
        }

        static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(D));
            PlayerPrefs.Save();
        }

        /// <summary>True: N random games. False: the hand-picked list.</summary>
        public static bool Randomize
        {
            get => D.random;
            set { D.random = value; Save(); }
        }

        /// <summary>How many games a randomized tournament has.</summary>
        public static int RandomCount
        {
            get => D.count;
            set { D.count = Mathf.Clamp(value, MinGames, MaxGames); Save(); }
        }

        /// <summary>The hand-picked games, in play order.</summary>
        public static List<EventKind> Picks
        {
            get
            {
                var list = new List<EventKind>();
                foreach (int k in D.picks) if (System.Array.IndexOf(Events.All, (EventKind)k) >= 0) list.Add((EventKind)k);
                return list;
            }
        }

        public static int PickIndex(EventKind k) => Picks.IndexOf(k);

        /// <summary>Add a game to the end of the hand-picked list, or take it out if it's already in.</summary>
        public static void TogglePick(EventKind k)
        {
            int i = D.picks.IndexOf((int)k);
            if (i >= 0) D.picks.RemoveAt(i);
            else if (D.picks.Count < MaxGames) D.picks.Add((int)k);
            Save();
        }

        public static void ClearPicks()
        {
            D.picks.Clear();
            Save();
        }

        /// <summary>Pick every game (up to the limit).</summary>
        public static void PickAll()
        {
            D.picks.Clear();
            foreach (var k in Events.All)
                if (D.picks.Count < MaxGames) D.picks.Add((int)k);
            Save();
        }

        /// <summary>Number of games the next tournament will have.</summary>
        public static int GameCount => Randomize ? RandomCount : Mathf.Max(1, Picks.Count);

        /// <summary>True if START can be pressed.</summary>
        public static bool Ready => Randomize || Picks.Count > 0;

        /// <summary>The games for a new tournament. Randomized ones don't repeat until every game has been played once.</summary>
        public static List<EventKind> BuildPlaylist(System.Random rng)
        {
            var list = new List<EventKind>();
            if (!Randomize)
            {
                list.AddRange(Picks);
                if (list.Count == 0) list.AddRange(ShowManager.ClassicLineup);
                return list;
            }
            var bag = new List<EventKind>();
            while (list.Count < RandomCount)
            {
                if (bag.Count == 0)
                {
                    bag.AddRange(Events.All);
                    // Don't play the same game twice in a row across a refill.
                    if (list.Count > 0 && bag.Count > 1) bag.Remove(list[list.Count - 1]);
                }
                int i = rng.Next(bag.Count);
                list.Add(bag[i]);
                bag.RemoveAt(i);
            }
            return list;
        }

        /// <summary>One line for the lobby: "10 random games" or "6 games: A > B > C...".</summary>
        public static string Summary()
        {
            if (Randomize) return RandomCount + " random game" + (RandomCount == 1 ? "" : "s");
            var picks = Picks;
            if (picks.Count == 0) return "No games picked yet";
            return picks.Count + " game" + (picks.Count == 1 ? "" : "s") + ": " + Events.Title(picks[0]) + (picks.Count > 1 ? " and more" : "");
        }
    }
}
