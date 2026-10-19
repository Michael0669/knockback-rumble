using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>A contestant voice pack: name, color, voice and the one-liners they yell.</summary>
    public class Persona
    {
        public string Key;
        public string Name;
        public Color Color;
        public VoiceStyle Voice;
        public VoiceLine[] Launched;
        public VoiceLine[] Fell;
        public VoiceLine[] Finished;

        public Persona(string key, string name, Color color, VoiceStyle voice,
            string[] launched = null, string[] fell = null, string[] finished = null)
        {
            Key = key;
            Name = name;
            Color = color;
            Voice = voice;
            Launched = VoiceLine.Set(key + "_launched", name, voice, launched ?? new string[0]);
            Fell = VoiceLine.Set(key + "_fell", name, voice, fell ?? new string[0]);
            Finished = VoiceLine.Set(key + "_finished", name, voice, finished ?? new string[0]);
        }
    }

    public static class Personas
    {
        public static readonly Persona Player = new Persona("player", "You", new Color(1f, 0.5f, 0.1f), new VoiceStyle(false, 0, "medium"));

        public static readonly Persona[] Bots =
        {
            new Persona("chad", "Chad Thundercrash", new Color(0.85f, 0.15f, 0.15f), new VoiceStyle(false, 1, "low"),
                new[] { "Bro, I can't feel my legs. Literally. I'm plastic.", "Not the gains!", "Spot me, bro! SPOT ME!" },
                new[] { "Skipped leg day for THIS?!", "Bro... that's not cardio." },
                new[] { "Too easy. Where's my protein?", "Flex on 'em!" }),

            new Persona("brayden", "Brayden", new Color(0.20f, 0.75f, 0.30f), new VoiceStyle(false, 3, "high"),
                new[] { "This launch is a rug pull!", "Buy the dip! I AM the dip!", "Somebody short me!" },
                new[] { "Down a hundred percent! Diamond hands, bro!", "Mint THAT as an NFT." },
                new[] { "To the moon, losers!", "Generational wealth, baby!" }),

            new Persona("karen", "Karen Kollision", new Color(0.75f, 0.60f, 0.95f), new VoiceStyle(true, 2, "high"),
                new[] { "I did NOT consent to this trajectory!", "Where is the manager of gravity?!", "One star. ONE STAR!" },
                new[] { "I want to speak to whoever built this pit!", "My lawyer will hear about this!" },
                new[] { "Finally. Some competence.", "I still expect a refund." }),

            new Persona("dale", "Uncle Dale", new Color(0.55f, 0.50f, 0.25f), new VoiceStyle(false, -2, "x-low"),
                new[] { "Hold my beer. No wait, give it back, give it baaack!", "Who moved the damn ground?!", "Ain't the first time I've flown drunk!" },
                new[] { "Somebody grab my cooler!", "Worth it." },
                new[] { "Hell yeah, brother!", "Somebody fire up the grill!" }),

            new Persona("gramps", "Gramps", new Color(0.70f, 0.66f, 0.58f), new VoiceStyle(false, -4, "low"),
                new[] { "In my day we got launched uphill both ways!", "Get off my damn lawn!", "My hip! My plastic hip!" },
                new[] { "I'm writing my congressman!", "Back in my day pits had railings!" },
                new[] { "Still got it, you little shits!", "Now where the hell did I park?" }),

            new Persona("tyler", "Tyler", new Color(0.50f, 0.25f, 0.80f), new VoiceStyle(false, 4, "high"),
                new[] { "Chat, CHAT, clip that!", "Lag! That was lag!", "Mom, is the wifi down?!" },
                new[] { "That's going in the fail compilation.", "I'm ending stream." },
                new[] { "G G E Z. Like and subscribe.", "Four viewers saw that. FOUR!" }),

            new Persona("brittani", "Brittani", new Color(1f, 0.40f, 0.70f), new VoiceStyle(true, 3, "x-high"),
                new[] { "Wait, launch me again, the lighting was trash.", "Do NOT tag me in this!", "This is SO going on my story!" },
                new[] { "Ugh, my followers can NOT see this.", "This is not my aesthetic." },
                new[] { "Link in bio, losers!", "Smash that follow button!" }),

            new Persona("sarge", "Sarge", new Color(0.15f, 0.25f, 0.55f), new VoiceStyle(false, 1, "low"),
                new[] { "Tactical retreat! TACTICAL RETREAT!", "Man down! Man DOWN!", "I'm calling for backup!" },
                new[] { "Tell the food court... I tried.", "This is a code red, people!" },
                new[] { "Mission accomplished. Mall secured.", "Nobody gets past Sarge!" }),
        };
    }
}
