using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>One playable character. The vest always takes the player's color.</summary>
    public class CharacterDef
    {
        public byte id;
        public string name;
        public string blurb;
        // Built-in soldier look (used until real models are dropped in):
        public int skin;       // DummyVisual.SkinTones index
        public int hairColor;  // DummyVisual.HairColors index
        public int beard;      // 0 full beard, 1 horseshoe mustache, 2 stubble, 3 clean shaven
        public int hairStyle;  // 0 buzz cut, 1 mohawk, 2 patrol cap, 3 bald
    }

    /// <summary>
    /// The characters you can pick on the CHARACTER screen. Right now they're variations of the
    /// built-in low-poly soldier; when real character models are added, each entry will point at one.
    /// </summary>
    public static class Characters
    {
        public static readonly CharacterDef[] All =
        {
            new CharacterDef { id = 0, name = "Sarge", blurb = "Has yelled at every one of you. Twice.", skin = 1, hairColor = 0, beard = 1, hairStyle = 0 },
            new CharacterDef { id = 1, name = "Rookie", blurb = "First day. Already lost his rifle.", skin = 0, hairColor = 2, beard = 3, hairStyle = 2 },
            new CharacterDef { id = 2, name = "Mohawk", blurb = "Thinks the haircut counts as armor.", skin = 3, hairColor = 3, beard = 2, hairStyle = 1 },
            new CharacterDef { id = 3, name = "Beardo", blurb = "The beard has its own security clearance.", skin = 2, hairColor = 1, beard = 0, hairStyle = 3 },
            new CharacterDef { id = 4, name = "Doc", blurb = "Medic. Mostly prescribes walking it off.", skin = 4, hairColor = 3, beard = 3, hairStyle = 0 },
            new CharacterDef { id = 5, name = "Gramps", blurb = "Been in this war since before it started.", skin = 0, hairColor = 4, beard = 0, hairStyle = 2 },
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
