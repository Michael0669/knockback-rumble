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
        Bandolier = 7,     // +50% ammo
        QuickHands = 8,    // shoot 20% faster
        HeavyHitter = 9,   // your hits knock 15% harder
        BountyHunter = 10, // +$50 extra for every knock-off
        HazardPay = 11,    // double pain pay
        Springs = 12,      // jump 20% higher
    }

    public enum Gadget : byte
    {
        None = 0,
        BouncerGrenade = 1,
        StickyMine = 2,
        StinkBomb = 3,
        BananaPeel = 4,
        AirHorn = 5,
        BouncePad = 6,
    }

    public enum Streak : byte
    {
        None = 0,
        GaryStrike = 1,    // seagull bombing run
        WreckingBall = 2,  // giant ball dropped on the cash leader
        CarpetBomb = 3,    // a line of bombs in front of you
        MoonBoots = 4,     // low gravity for you
        AnvilDrop = 5,     // anvils on the two nearest rivals
        TacticalToilet = 6,// a giant toilet on every rival
    }

    /// <summary>Display info shared by every loadout item.</summary>
    public class LoadoutItem
    {
        public byte id;
        public string name;
        public string description;
        /// <summary>Wallet cash to unlock it (0 = free from the start).</summary>
        public int price;
        public int cost;          // streaks: knock-offs needed. gadgets: uses per round.
        public Color color;
    }

    /// <summary>What kind of thing you're buying (used for the saved "owned" list).</summary>
    public enum ShopCategory { Weapon = 10, Gadget = 11, Perk = 12, Streak = 13, Character = 14 }

    public static class Loadouts
    {
        /// <summary>How many loadouts you can save.</summary>
        public const int SlotCount = 5;

        /// <summary>Weapons you can pick as your primary (in display order, cheapest first).</summary>
        public static WeaponDef[] Primaries => WeaponDef.Guns;

        public static readonly LoadoutItem[] Gadgets =
        {
            new LoadoutItem { id = (byte)Gadget.BouncerGrenade, name = "Bouncer Grenade", price = 0, cost = 2,
                description = "Bounces around, then blows everyone nearby sky high.", color = new Color(0.55f, 0.9f, 0.3f) },
            new LoadoutItem { id = (byte)Gadget.StickyMine, name = "Sticky Mine", price = 1000, cost = 2,
                description = "Drop it on a choke point. Whoever steps near it goes flying.", color = new Color(1f, 0.45f, 0.2f) },
            new LoadoutItem { id = (byte)Gadget.BananaPeel, name = "Banana Peel", price = 1200, cost = 3,
                description = "Drop it and walk away. Whoever steps on it slips straight up into the air.", color = new Color(1f, 0.86f, 0.2f) },
            new LoadoutItem { id = (byte)Gadget.StinkBomb, name = "Stink Bomb", price = 1500, cost = 2,
                description = "A big, soft blast that piles on damage %. Softens everyone up for the kill.", color = new Color(0.6f, 0.75f, 0.25f) },
            new LoadoutItem { id = (byte)Gadget.BouncePad, name = "Bounce Pad", price = 2000, cost = 2,
                description = "Drop a trampoline. Anyone who steps on it gets flung skyward. Including you.", color = new Color(1f, 0.35f, 0.7f) },
            new LoadoutItem { id = (byte)Gadget.AirHorn, name = "Air Horn", price = 2500, cost = 2,
                description = "BWAAAAH! Blasts everyone around you away. Never touches you.", color = new Color(0.95f, 0.3f, 0.3f) },
        };

        public static readonly LoadoutItem[] Perks =
        {
            new LoadoutItem { id = (byte)Perk.SteelToes, name = "Steel Toes", price = 0,
                description = "Take 25% less knockback.", color = new Color(0.6f, 0.65f, 0.75f) },
            new LoadoutItem { id = (byte)Perk.GreaseMonkey, name = "Grease Monkey", price = 0,
                description = "Get back up 40% faster after a hit.", color = new Color(0.95f, 0.7f, 0.2f) },
            new LoadoutItem { id = (byte)Perk.Scavenger, name = "Scavenger", price = 1000,
                description = "Ammo crates also refill your gadget.", color = new Color(0.75f, 0.55f, 0.95f) },
            new LoadoutItem { id = (byte)Perk.Springs, name = "Springs", price = 1000,
                description = "Jump 20% higher.", color = new Color(0.4f, 0.95f, 0.75f) },
            new LoadoutItem { id = (byte)Perk.Bandolier, name = "Bandolier", price = 1200,
                description = "Carry 50% more ammo.", color = new Color(0.8f, 0.6f, 0.35f) },
            new LoadoutItem { id = (byte)Perk.DoubleJump, name = "Double Jump", price = 1500,
                description = "Jump once more in mid-air.", color = new Color(0.3f, 0.8f, 0.95f) },
            new LoadoutItem { id = (byte)Perk.Lightweight, name = "Lightweight", price = 1500,
                description = "Run 12% faster.", color = new Color(0.95f, 0.95f, 0.95f) },
            new LoadoutItem { id = (byte)Perk.HazardPay, name = "Hazard Pay", price = 1500,
                description = "Get double pain pay every time someone hits you.", color = new Color(1f, 0.55f, 0.3f) },
            new LoadoutItem { id = (byte)Perk.DeepPockets, name = "Deep Pockets", price = 2000,
                description = "Lose half as much cash when you fall.", color = new Color(0.35f, 0.85f, 0.45f) },
            new LoadoutItem { id = (byte)Perk.QuickHands, name = "Quick Hands", price = 2000,
                description = "Shoot 20% faster.", color = new Color(0.95f, 0.85f, 0.4f) },
            new LoadoutItem { id = (byte)Perk.BountyHunter, name = "Bounty Hunter", price = 2500,
                description = "Earn an extra $50 for every knock-off.", color = new Color(0.9f, 0.75f, 0.2f) },
            new LoadoutItem { id = (byte)Perk.HeavyHitter, name = "Heavy Hitter", price = 3000,
                description = "Everything you hit flies 15% farther.", color = new Color(0.9f, 0.3f, 0.25f) },
        };

        public static readonly LoadoutItem[] Streaks =
        {
            new LoadoutItem { id = (byte)Streak.MoonBoots, name = "Moon Boots", price = 1000, cost = 2,
                description = "2 knock-offs in a row: low gravity for you for 12 seconds. Float like a butterfly.", color = new Color(0.7f, 0.75f, 1f) },
            new LoadoutItem { id = (byte)Streak.CarpetBomb, name = "Carpet Bomb", price = 1500, cost = 2,
                description = "2 knock-offs in a row: a line of bombs rolls out in front of you.", color = new Color(0.95f, 0.6f, 0.2f) },
            new LoadoutItem { id = (byte)Streak.GaryStrike, name = "Gary Strike", price = 0, cost = 3,
                description = "3 knock-offs in a row: Gary the Shithawk bombs your rivals.", color = new Color(0.95f, 0.95f, 0.8f) },
            new LoadoutItem { id = (byte)Streak.AnvilDrop, name = "Anvil Drop", price = 3000, cost = 4,
                description = "4 knock-offs in a row: cartoon anvils fall on the two closest rivals.", color = new Color(0.55f, 0.58f, 0.65f) },
            new LoadoutItem { id = (byte)Streak.WreckingBall, name = "Wrecking Ball", price = 2500, cost = 5,
                description = "5 knock-offs in a row: drop a wrecking ball on the cash leader.", color = new Color(0.9f, 0.25f, 0.2f) },
            new LoadoutItem { id = (byte)Streak.TacticalToilet, name = "Tactical Toilet", price = 6000, cost = 7,
                description = "7 knock-offs in a row: a giant toilet falls on EVERY rival. Flush 'em.", color = new Color(0.85f, 0.9f, 0.95f) },
        };

        public static LoadoutItem Find(LoadoutItem[] list, byte id)
        {
            foreach (var i in list) if (i.id == id) return i;
            return null;
        }

        public static LoadoutItem PerkInfo(Perk p) => Find(Perks, (byte)p);
        public static LoadoutItem GadgetInfo(Gadget g) => Find(Gadgets, (byte)g);
        public static LoadoutItem StreakInfo(Streak s) => Find(Streaks, (byte)s);

        /// <summary>A random loadout for a bot (bots get everything).</summary>
        public static Loadout RandomForBot(System.Random rng)
        {
            var l = new Loadout
            {
                primary = Primaries[rng.Next(Primaries.Length)].netId,
                gadget = (Gadget)Gadgets[rng.Next(Gadgets.Length)].id,
            };
            l.perk1 = (Perk)Perks[rng.Next(Perks.Length)].id;
            do { l.perk2 = (Perk)Perks[rng.Next(Perks.Length)].id; } while (l.perk2 == l.perk1);
            l.streak1 = (Streak)Streaks[rng.Next(Streaks.Length)].id;
            do { l.streak2 = (Streak)Streaks[rng.Next(Streaks.Length)].id; } while (l.streak2 == l.streak1);
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

        /// <summary>Swap anything you don't own for a free default.</summary>
        public void Sanitize()
        {
            var pd = WeaponDef.ById(primary);
            if (pd == null || pd == WeaponDef.Shove || !Profile.Owns(pd)) primary = WeaponDef.PopPistol.netId;
            var g = Loadouts.GadgetInfo(gadget);
            if (g == null || !Profile.Owns(ShopCategory.Gadget, g)) gadget = Gadget.BouncerGrenade;
            var p1 = Loadouts.PerkInfo(perk1);
            if (p1 == null || !Profile.Owns(ShopCategory.Perk, p1)) perk1 = Perk.SteelToes;
            var p2 = Loadouts.PerkInfo(perk2);
            if (p2 == null || !Profile.Owns(ShopCategory.Perk, p2) || perk2 == perk1) perk2 = perk1 == Perk.GreaseMonkey ? Perk.SteelToes : Perk.GreaseMonkey;
            var s1 = Loadouts.StreakInfo(streak1);
            if (s1 == null || !Profile.Owns(ShopCategory.Streak, s1)) streak1 = Streak.GaryStrike;
            var s2 = Loadouts.StreakInfo(streak2);
            if (streak2 != Streak.None && (s2 == null || !Profile.Owns(ShopCategory.Streak, s2) || streak2 == streak1)) streak2 = Streak.None;
        }
    }

    /// <summary>
    /// The local player's saved progress: wallet cash, everything they've bought, and 5 saved loadouts.
    /// No levels: everything unlocks with cash (stronger stuff costs more).
    /// Stored in PlayerPrefs on this PC (moves to Steam Cloud / Steam Inventory later).
    /// </summary>
    public static class Profile
    {
        const string Key = "kr_profile_v1";

        [System.Serializable]
        public class SavedLoadout
        {
            public string name;
            public int primary = 2, gadget = 1, perk1 = 1, perk2 = 2, streak1 = 1, streak2 = 0;
        }

        [System.Serializable]
        class Data
        {
            public int wallet;
            public int showsPlayed;
            public int showsWon;
            // Old single loadout (before saved loadouts). Only read once to fill slot 1.
            public int primary = 2, gadget = 1, perk1 = 1, perk2 = 2, streak1 = 1, streak2 = 0;
            // The 5 saved loadouts and which one you're using.
            public List<SavedLoadout> loadouts = new List<SavedLoadout>();
            public int activeLoadout;
            // Bought things. Cosmetics: slot * 256 + id. Shop items: category * 1000 + id (see ShopCategory).
            public List<int> owned = new List<int>();
            public int[] outfit = new int[Outfit.SlotCount];
            // The character picked on the CHARACTER screen
            public int character;
        }

        static Data data;
        static Loadout[] loadouts;

        static Data D
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { data = null; loadouts = null; }

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
            if (data.owned == null) data.owned = new List<int>();
            if (data.outfit == null || data.outfit.Length != Outfit.SlotCount) data.outfit = new int[Outfit.SlotCount];
            if (data.loadouts == null) data.loadouts = new List<SavedLoadout>();
            if (data.loadouts.Count == 0)
            {
                // First time with saved loadouts: your old loadout becomes slot 1.
                data.loadouts.Add(new SavedLoadout
                {
                    primary = data.primary, gadget = data.gadget, perk1 = data.perk1,
                    perk2 = data.perk2, streak1 = data.streak1, streak2 = data.streak2,
                });
            }
            while (data.loadouts.Count < Loadouts.SlotCount) data.loadouts.Add(new SavedLoadout());
            while (data.loadouts.Count > Loadouts.SlotCount) data.loadouts.RemoveAt(data.loadouts.Count - 1);
            data.activeLoadout = Mathf.Clamp(data.activeLoadout, 0, Loadouts.SlotCount - 1);

            loadouts = new Loadout[Loadouts.SlotCount];
            for (int i = 0; i < Loadouts.SlotCount; i++)
            {
                var sl = data.loadouts[i];
                if (string.IsNullOrEmpty(sl.name)) sl.name = "LOADOUT " + (i + 1);
                loadouts[i] = new Loadout
                {
                    primary = (byte)sl.primary, gadget = (Gadget)sl.gadget, perk1 = (Perk)sl.perk1,
                    perk2 = (Perk)sl.perk2, streak1 = (Streak)sl.streak1, streak2 = (Streak)sl.streak2,
                };
                loadouts[i].Sanitize();
            }
        }

        public static void Save()
        {
            var d = D;
            for (int i = 0; i < Loadouts.SlotCount; i++)
            {
                var l = loadouts[i];
                var sl = d.loadouts[i];
                sl.primary = l.primary; sl.gadget = (int)l.gadget; sl.perk1 = (int)l.perk1;
                sl.perk2 = (int)l.perk2; sl.streak1 = (int)l.streak1; sl.streak2 = (int)l.streak2;
            }
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        public static int Wallet => D.wallet;
        public static int ShowsPlayed => D.showsPlayed;
        public static int ShowsWon => D.showsWon;

        /// <summary>The loadout you take into games (the active saved slot).</summary>
        public static Loadout Loadout => GetLoadout(ActiveLoadout);

        /// <summary>Which of the 5 saved loadouts you're using (0-4).</summary>
        public static int ActiveLoadout
        {
            get => D.activeLoadout;
            set { D.activeLoadout = Mathf.Clamp(value, 0, Loadouts.SlotCount - 1); Save(); }
        }

        public static Loadout GetLoadout(int slot)
        {
            if (data == null) Load();
            return loadouts[Mathf.Clamp(slot, 0, Loadouts.SlotCount - 1)];
        }

        public static string LoadoutName(int slot) => D.loadouts[Mathf.Clamp(slot, 0, Loadouts.SlotCount - 1)].name;

        public static void SetLoadoutName(int slot, string name)
        {
            name = (name ?? "").Trim();
            if (name.Length > 16) name = name.Substring(0, 16);
            D.loadouts[Mathf.Clamp(slot, 0, Loadouts.SlotCount - 1)].name = name.Length > 0 ? name.ToUpperInvariant() : "LOADOUT " + (slot + 1);
            Save();
        }

        /// <summary>Save a loadout into a slot (anything you don't own is swapped for a free default).</summary>
        public static void SetLoadout(int slot, Loadout l)
        {
            if (data == null) Load();
            slot = Mathf.Clamp(slot, 0, Loadouts.SlotCount - 1);
            loadouts[slot] = l.Copy();
            loadouts[slot].Sanitize();
            Save();
        }

        /// <summary>Banks your show cash after a tournament.</summary>
        public static void AwardShow(int cash, bool won)
        {
            var d = D;
            d.wallet += Mathf.Max(0, cash);
            d.showsPlayed++;
            if (won) d.showsWon++;
            Save();
        }

        // ---------- Buying weapons, gadgets, perks, streaks and characters ----------

        static int ShopKey(ShopCategory cat, int id) => (int)cat * 1000 + id;

        public static bool Owns(ShopCategory cat, int id, int price) => price <= 0 || D.owned.Contains(ShopKey(cat, id));
        public static bool Owns(WeaponDef w) => w != null && Owns(ShopCategory.Weapon, w.netId, w.price);
        public static bool Owns(ShopCategory cat, LoadoutItem i) => i != null && Owns(cat, i.id, i.price);

        /// <summary>Buy something with wallet cash. False if you can't afford it. True if you already own it.</summary>
        public static bool Buy(ShopCategory cat, int id, int price)
        {
            if (Owns(cat, id, price)) return true;
            var d = D;
            if (d.wallet < price) return false;
            d.wallet -= price;
            d.owned.Add(ShopKey(cat, id));
            Save();
            return true;
        }

        // ---------- Testing tools ----------

        /// <summary>Testing: add wallet cash.</summary>
        public static void TestAddCash(int amount)
        {
            D.wallet = Mathf.Max(0, D.wallet + amount);
            Save();
        }

        /// <summary>Testing: own every weapon, gadget, perk, streak and character for free.</summary>
        public static void TestUnlockAll()
        {
            var d = D;
            void Own(ShopCategory c, int id) { int k = ShopKey(c, id); if (!d.owned.Contains(k)) d.owned.Add(k); }
            foreach (var w in WeaponDef.Guns) Own(ShopCategory.Weapon, w.netId);
            foreach (var i in Loadouts.Gadgets) Own(ShopCategory.Gadget, i.id);
            foreach (var i in Loadouts.Perks) Own(ShopCategory.Perk, i.id);
            foreach (var i in Loadouts.Streaks) Own(ShopCategory.Streak, i.id);
            foreach (var c in Characters.All) Own(ShopCategory.Character, c.id);
            Save();
        }

        /// <summary>Testing: wipe wallet, stats, everything bought, outfit and loadouts back to a brand-new player.</summary>
        public static void ResetAll()
        {
            data = new Data();
            loadouts = null;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            data = null;
            Load();
            Save();
        }

        // ---------- Cosmetics ----------

        static int ItemKey(CosmeticItem i) => (int)i.slot * 256 + i.id;

        public static bool Owns(CosmeticItem i) => i == null || i.price <= 0 || D.owned.Contains(ItemKey(i));

        /// <summary>Buy an item with wallet cash. False if you can't afford it.</summary>
        public static bool Buy(CosmeticItem i)
        {
            if (i == null || Owns(i)) return true;
            var d = D;
            if (d.wallet < i.price) return false;
            d.wallet -= i.price;
            d.owned.Add(ItemKey(i));
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

        public static bool OwnsCharacter(CharacterDef c) => c != null && Owns(ShopCategory.Character, c.id, c.price);

        /// <summary>The character you play as (CHARACTER screen). Falls back to the first one if you don't own it.</summary>
        public static int Character
        {
            get
            {
                var c = Characters.Get(D.character);
                return c != null && OwnsCharacter(c) ? D.character : Characters.All[0].id;
            }
            set { D.character = value; Save(); }
        }

        public static void SetOutfit(Outfit o)
        {
            var d = D;
            for (int s = 0; s < Outfit.SlotCount; s++) d.outfit[s] = o.ids[s];
            Save();
        }
    }
}
