using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>One playable character.</summary>
    public class CharacterDef
    {
        public byte id;
        public string name;
        public string blurb;
        /// <summary>Wallet cash to unlock (0 = free from the start).</summary>
        public int price;
        /// <summary>The character's parts in Characters/Humans_Master (the downloaded pack).</summary>
        public string[] parts;
        /// <summary>The material that gets the player's color (their main piece of clothing).</summary>
        public string tint;
        /// <summary>Turn the model if it faces the wrong way (degrees).</summary>
        public float yaw;
        // Built-in soldier look, used if the downloaded model is missing:
        public int skin;       // DummyVisual.SkinTones index
        public int hairColor;  // DummyVisual.HairColors index
        public int beard;      // 0 full beard, 1 horseshoe mustache, 2 stubble, 3 clean shaven
        public int hairStyle;  // 0 buzz cut, 1 mohawk, 2 patrol cap, 3 bald
    }

    /// <summary>
    /// The characters you can pick on the CHARACTER screen: the 12 people from the downloaded
    /// character pack (one rig, 4 parts each). Every one has a built-in soldier look to fall back on.
    /// </summary>
    public static class Characters
    {
        static string[] P(string who, string legs = "Legs") => new[] { who + "_Feet", who + "_" + legs, who + "_Body", who + "_Head" };

        public static readonly CharacterDef[] All =
        {
            new CharacterDef { id = 0, price = 0, name = "Sgt. Kneecap", blurb = "SWAT. Thinks every door is a battering-ram opportunity.", parts = P("Swat"), tint = "Swat",
                skin = 1, hairColor = 0, beard = 1, hairStyle = 0 },
            new CharacterDef { id = 1, price = 0, name = "Trailmix", blurb = "Adventurer. Has been lost since 2009.", parts = P("Adventurer"), tint = "Green",
                skin = 0, hairColor = 2, beard = 3, hairStyle = 2 },
            new CharacterDef { id = 2, price = 500, name = "Old McBonk", blurb = "Farmer. Smells like a barn. Proud of it.", parts = P("Farmer", "Pants"), tint = "LightBlue",
                skin = 0, hairColor = 4, beard = 0, hairStyle = 2 },
            new CharacterDef { id = 3, price = 750, name = "Hard Hat Harry", blurb = "Construction worker. Union break every four minutes.", parts = P("Worker"), tint = "Worker_Vest",
                skin = 2, hairColor = 1, beard = 1, hairStyle = 0 },
            new CharacterDef { id = 4, price = 1000, name = "Riot", blurb = "Punk. Allergic to rules and soap.", parts = P("Punk"), tint = "Red",
                skin = 3, hairColor = 3, beard = 2, hairStyle = 1 },
            new CharacterDef { id = 5, price = 1500, name = "Cosmo", blurb = "Astronaut. Got lost on the way to space.", parts = P("SpaceSuit"), tint = "SciFi_Main",
                skin = 4, hairColor = 3, beard = 3, hairStyle = 0 },
            new CharacterDef { id = 6, price = 2000, name = "His Royal Lowness", blurb = "King. Rules a kingdom of three porta-potties.", parts = P("King"), tint = "Blue",
                skin = 0, hairColor = 4, beard = 0, hairStyle = 3 },
            new CharacterDef { id = 7, price = 1000, name = "Beach Bod Brad", blurb = "Forgot his shirt. And his dignity.", parts = P("Beach"), tint = "Red_Dark",
                skin = 1, hairColor = 2, beard = 3, hairStyle = 0 },
            new CharacterDef { id = 8, price = 1500, name = "Izzy Influencer", blurb = "Livestreaming her own elimination.", parts = P("Casual"), tint = "Purple",
                skin = 2, hairColor = 1, beard = 3, hairStyle = 3 },
            new CharacterDef { id = 9, price = 500, name = "Dude", blurb = "Just here for the free snacks.", parts = P("Casual2"), tint = "LightBrown",
                skin = 3, hairColor = 0, beard = 2, hairStyle = 0 },
            new CharacterDef { id = 10, price = 3000, name = "The Lawyer", blurb = "Will sue whoever shoves him first.", parts = P("Suit"), tint = "Suit",
                skin = 0, hairColor = 0, beard = 3, hairStyle = 0 },
            new CharacterDef { id = 11, price = 5000, name = "Horse Guy", blurb = "Nobody knows why. Nobody asks.",
                parts = new[] { "Casual2_Feet", "Casual2_Legs", "Casual2_Body", "Horse_Head" }, tint = "LightBrown",
                skin = 2, hairColor = 1, beard = 0, hairStyle = 3 },
        };

        public static CharacterDef Get(int id)
        {
            foreach (var c in All) if (c.id == id) return c;
            return null;
        }

        /// <summary>A bot always plays the same character (from its persona).</summary>
        public static int ForBot(int style) => All[Mathf.Abs(style) % All.Length].id;
    }
}
