using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    // =========================================================================
    // Everything you can put in a loadout. To add a new perk, gadget or streak:
    // add an entry to the list below (pick the next unused id), then handle its
    // id where noted. Weapons live in WeaponDef.cs (add them to Loadouts.Primaries).
    // =========================================================================

    public enum Perk : byte
    {
        None = 0,
        SteelToes = 1,     // take less knockback
        GreaseMonkey = 2,  // shorter stuns
        DoubleJump = 3,    // one extra jump in the air
        DeepPockets = 4,   // lose half as much cash when you fall
        Lightweight = 5,   // run faster
        Scavenger = 6,     // ammo crates also refill your gadget
    }

    public enum Gadget : byte
    {
        None = 0,
        BouncerGrenade = 1,
        StickyMine = 2,
    }

    public enum Streak : byte
    {
        None = 0,
        GaryStrike = 1,    // seagull bombing run
        WreckingBall = 2,  // giant ball dropped on the cash leader
    }

    /// <summary>Display info shared by every loadout item.</summary>
    public class LoadoutItem
    {
        public byte id;
        public string name;
        public string description;
        public int unlockLevel;
        public int cost;          // streaks: knock-offs needed. gadgets: uses per round.
        public Color color;
    }

    public static class Loadouts
    {
        public const int MaxLevel = 30;

        /// <summary>Weapons you can pick as your primary (in display order).</summary>
        public static readonly WeaponDef[] Primaries =
        {
            WeaponDef.PopPistol, WeaponDef.BoomStick, WeaponDef.RocketLauncher, WeaponDef.LongShot,
        };

        public static readonly LoadoutItem[] Gadgets =
        {
            new LoadoutItem { id = (byte)Gadget.BouncerGrenade, name = "Bouncer Grenade", unlockLevel = 1, cost = 2,
                description = "Bounces around, then blows everyone nearby sky high.", color = new Color(0.55f, 0.9f, 0.3f) },
            new LoadoutItem { id = (byte)Gadget.StickyMine, name = "Sticky Mine", unlockLevel = 2, cost = 2,
                description = "Drop it on a choke point. Whoever steps near it goes flying.", color = new Color(1f, 0.45f, 0.2f) },
        };

        public static readonly LoadoutItem[] Perks =
        {
            new LoadoutItem { id = (byte)Perk.SteelToes, name = "Steel Toes", unlockLevel = 1,
                description = "Take 25% less knockback.", color = new Color(0.6f, 0.65f, 0.75f) },
            new LoadoutItem { id = (byte)Perk.GreaseMonkey, name = "Grease Monkey", unlockLevel = 1,
                description = "Get back up 40% faster after a hit.", color = new Color(0.95f, 0.7f, 0.2f) },
            new LoadoutItem { id = (byte)Perk.DoubleJump, name = "Double Jump", unlockLevel = 2,
                description = "Jump once more in mid-air.", color = new Color(0.3f, 0.8f, 0.95f) },
            new LoadoutItem { id = (byte)Perk.DeepPockets, name = "Deep Pockets", unlockLevel = 3,
                description = "Lose half as much cash when you fall.", color = new Color(0.35f, 0.85f, 0.45f) },
            new LoadoutItem { id = (byte)Perk.Lightweight, name = "Lightweight", unlockLevel = 4,
                description = "Run 12% faster.", color = new Color(0.95f, 0.95f, 0.95f) },
            new LoadoutItem { id = (byte)Perk.Scavenger, name = "Scavenger", unlockLevel = 4,
                description = "Ammo crates also refill your gadget.", color = new Color(0.75f, 0.55f, 0.95f) },
        };

        public static readonly LoadoutItem[] Streaks =
        {
            new LoadoutItem { id = (byte)Streak.GaryStrike, name = "Gary Strike", unlockLevel = 1, cost = 3,
                description = "3 knock-offs in a row: Gary the Shithawk bombs your rivals.", color = new Color(0.95f, 0.95f, 0.8f) },
            new LoadoutItem { id = (byte)Streak.WreckingBall, name = "Wrecking Ball", unlockLevel = 3, cost = 5,
                description = "5 knock-offs in a row: drop a wrecking ball on the cash leader.", color = new Color(0.9f, 0.25f, 0.2f) },
        };

        public static LoadoutItem Find(LoadoutItem[] list, byte id)
        {
            foreach (var i in list) if (i.id == id) return i;
            return null;
        }

        public static LoadoutItem PerkInfo(Perk p) => Find(Perks, (byte)p);
        public static LoadoutItem GadgetInfo(Gadget g) => Find(Gadgets, (byte)g);
        public static LoadoutItem StreakInfo(Streak s) => Find(Streaks, (byte)s);

        /// <summary>XP needed to go from this level to the next.</summary>
        public static int XpForLevel(int level) => 400 + 200 * Mathf.Max(0, level - 1);

        /// <summary>A random loadout for a bot (bots get everything).</summary>
        public static Loadout RandomForBot(System.Random rng)
        {
            var l = new Loadout
            {
                primary = Primaries[rng.Next(Primaries.Length)].netId,
                gadget = (Gadget)Gadgets[rng.Next(Gadgets.Length)].id,
                streak1 = Streak.GaryStrike,
                streak2 = Streak.WreckingBall,
            };
            l.perk1 = (Perk)Perks[rng.Next(Perks.Length)].id;
            do { l.perk2 = (Perk)Perks[rng.Next(Perks.Length)].id; } while (l.perk2 == l.perk1);
            return l;
        }
    }

    /// <summary>One player's chosen loadout.</summary>
    public class Loadout
    {
        public byte primary = 2;                      // WeaponDef.netId
        public Gadget gadget = Gadget.BouncerGrenade;
        public Perk perk1 = Perk.SteelToes;
        public Perk perk2 = Perk.GreaseMonkey;
        public Streak streak1 = Streak.GaryStrike;
        public Streak streak2 = Streak.None;

        public bool Has(Perk p) => perk1 == p || perk2 == p;

        public WeaponDef PrimaryDef => WeaponDef.ById(primary) ?? WeaponDef.PopPistol;

        public Loadout Copy() => (Loadout)MemberwiseClone();

        public void Write(NetWriter w) =>
            w.Byte(primary).Byte((byte)gadget).Byte((byte)perk1).Byte((byte)perk2).Byte((byte)streak1).Byte((byte)streak2);

        public static Loadout Read(NetReader r) => new Loadout
        {
            primary = r.Byte(), gadget = (Gadget)r.Byte(), perk1 = (Perk)r.Byte(),
            perk2 = (Perk)r.Byte(), streak1 = (Streak)r.Byte(), streak2 = (Streak)r.Byte(),
        };

        /// <summary>Swap anything not unlocked at this level for a default that is.</summary>
        public void Sanitize(int level)
        {
            var pd = WeaponDef.ById(primary);
            if (pd == null || pd.unlockLevel > level) primary = WeaponDef.PopPistol.netId;
            var g = Loadouts.GadgetInfo(gadget);
            if (g == null || g.unlockLevel > level) gadget = Gadget.BouncerGrenade;
            var p1 = Loadouts.PerkInfo(perk1);
            if (p1 == null || p1.unlockLevel > level) perk1 = Perk.SteelToes;
            var p2 = Loadouts.PerkInfo(perk2);
            if (p2 == null || p2.unlockLevel > level || perk2 == perk1) perk2 = perk1 == Perk.GreaseMonkey ? Perk.SteelToes : Perk.GreaseMonkey;
            var s1 = Loadouts.StreakInfo(streak1);
            if (s1 == null || s1.unlockLevel > level) streak1 = Streak.GaryStrike;
            var s2 = Loadouts.StreakInfo(streak2);
            if (streak2 != Streak.None && (s2 == null || s2.unlockLevel > level || streak2 == streak1)) streak2 = Streak.None;
        }
    }

    /// <summary>
    /// The local player's saved progress: level, XP, wallet cash and loadout.
    /// Stored in PlayerPrefs on this PC (moves to Steam Cloud / Steam Inventory later).
    /// </summary>
    public static class Profile
    {
        const string Key = "kr_profile_v1";

        [System.Serializable]
        class Data
        {
            public int level = 1;
            public int xp;
            public int wallet;
            public int showsPlayed;
            public int showsWon;
            // Loadout stored as plain numbers (safest for Unity's JSON).
            public int primary = 2, gadget = 1, perk1 = 1, perk2 = 2, streak1 = 1, streak2 = 0;
            // Cosmetics: owned items as slot * 256 + id, and the equipped id per slot.
            public List<int> owned = new List<int>();
            public int[] outfit = new int[Outfit.SlotCount];
        }

        static Data data;
        static Loadout loadout;

        static Data D
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { data = null; loadout = null; }

        static void Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(Key, "");
                data = string.IsNullOrEmpty(json) ? new Data() : JsonUtility.FromJson<Data>(json);
            }
            catch
            {
                data = new Data();
            }
            data.level = Mathf.Clamp(data.level, 1, Loadouts.MaxLevel);
            if (data.owned == null) data.owned = new List<int>();
            if (data.outfit == null || data.outfit.Length != Outfit.SlotCount) data.outfit = new int[Outfit.SlotCount];
            loadout = new Loadout
            {
                primary = (byte)data.primary, gadget = (Gadget)data.gadget, perk1 = (Perk)data.perk1,
                perk2 = (Perk)data.perk2, streak1 = (Streak)data.streak1, streak2 = (Streak)data.streak2,
            };
            loadout.Sanitize(data.level);
        }

        public static void Save()
        {
            var d = D;
            d.primary = loadout.primary; d.gadget = (int)loadout.gadget; d.perk1 = (int)loadout.perk1;
            d.perk2 = (int)loadout.perk2; d.streak1 = (int)loadout.streak1; d.streak2 = (int)loadout.streak2;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        public static int Level => D.level;
        public static int Xp => D.xp;
        public static int XpToNext => Loadouts.XpForLevel(D.level);
        public static int Wallet => D.wallet;
        public static int ShowsPlayed => D.showsPlayed;
        public static int ShowsWon => D.showsWon;
        public static Loadout Loadout { get { if (data == null) Load(); return loadout; } }

        public static void SetLoadout(Loadout l)
        {
            if (data == null) Load();
            loadout = l.Copy();
            loadout.Sanitize(data.level);
            Save();
        }

        /// <summary>Adds XP and cash after a show. Returns the levels gained (0 if none).</summary>
        public static int AwardShow(int cash, bool won)
        {
            var d = D;
            d.wallet += Mathf.Max(0, cash);
            d.showsPlayed++;
            if (won) d.showsWon++;
            int xp = 100 + Mathf.Max(0, cash) / 5 + (won ? 150 : 0);
            d.xp += xp;
            int gained = 0;
            while (d.level < Loadouts.MaxLevel && d.xp >= Loadouts.XpForLevel(d.level))
            {
                d.xp -= Loadouts.XpForLevel(d.level);
                d.level++;
                gained++;
            }
            Save();
            LastXpGained = xp;
            return gained;
        }

        public static int LastXpGained { get; private set; }

        // ---------- Testing tools ----------

        /// <summary>Testing: add wallet cash.</summary>
        public static void TestAddCash(int amount)
        {
            D.wallet = Mathf.Max(0, D.wallet + amount);
            Save();
        }

        /// <summary>Testing: jump straight to the next level (or the max).</summary>
        public static void TestLevelUp(bool toMax)
        {
            var d = D;
            d.level = toMax ? Loadouts.MaxLevel : Mathf.Min(Loadouts.MaxLevel, d.level + 1);
            d.xp = 0;
            Save();
        }

        /// <summary>Testing: wipe level, XP, wallet, stats, owned items, outfit and loadout back to a brand-new player.</summary>
        public static void ResetAll()
        {
            data = new Data();
            loadout = new Loadout();
            loadout.Sanitize(data.level);
            Save();
        }

        // ---------- Cosmetics ----------

        static int Key(CosmeticItem i) => (int)i.slot * 256 + i.id;

        public static bool Owns(CosmeticItem i) => i == null || i.price <= 0 || D.owned.Contains(Key(i));

        /// <summary>Buy an item with wallet cash. False if you can't afford it.</summary>
        public static bool Buy(CosmeticItem i)
        {
            if (i == null || Owns(i)) return true;
            var d = D;
            if (d.wallet < i.price) return false;
            d.wallet -= i.price;
            d.owned.Add(Key(i));
            Save();
            return true;
        }

        public static Outfit Outfit
        {
            get
            {
                var d = D;
                var o = new Outfit();
                for (int s = 0; s < Outfit.SlotCount; s++)
                {
                    var item = Cosmetics.Find((CosmeticSlot)s, (byte)Mathf.Clamp(d.outfit[s], 0, 255));
                    o.ids[s] = item != null && Owns(item) ? item.id : (byte)0;
                }
                return o;
            }
        }

        public static void SetOutfit(Outfit o)
        {
            var d = D;
            for (int s = 0; s < Outfit.SlotCount; s++) d.outfit[s] = o.ids[s];
            Save();
        }

        /// <summary>Everything unlocked at exactly this level (for the level-up screen).</summary>
        public static List<string> UnlocksAt(int level)
        {
            var list = new List<string>();
            foreach (var w in Loadouts.Primaries) if (w.unlockLevel == level) list.Add(w.name);
            foreach (var i in Loadouts.Gadgets) if (i.unlockLevel == level) list.Add(i.name);
            foreach (var i in Loadouts.Perks) if (i.unlockLevel == level) list.Add(i.name);
            foreach (var i in Loadouts.Streaks) if (i.unlockLevel == level) list.Add(i.name);
            return list;
        }
    }
}
