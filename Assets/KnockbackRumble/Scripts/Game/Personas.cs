using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>A contestant voice pack: name, color and the one-liners they yell.</summary>
    public class Persona
    {
        public string Name;
        public Color Color;
        public string[] Launched;
        public string[] Fell;
        public string[] Finished;

        public string Pick(string[] lines) => lines == null || lines.Length == 0 ? null : lines[Random.Range(0, lines.Length)];
    }

    public static class Personas
    {
        public static readonly Persona Player = new Persona
        {
            Name = "You",
            Color = new Color(1f, 0.5f, 0.1f),
        };

        public static readonly Persona[] Bots =
        {
            new Persona
            {
                Name = "Chad Thundercrash", Color = new Color(0.85f, 0.15f, 0.15f),
                Launched = new[] { "Bro, I can't feel my legs. Literally. I'm plastic.", "Not the gains!", "SPOT ME, BRO!" },
                Fell = new[] { "Skipped leg day for THIS?!", "Bro... that's not cardio." },
                Finished = new[] { "Too easy. Where's my protein?", "Flex on 'em!" },
            },
            new Persona
            {
                Name = "Brayden", Color = new Color(0.20f, 0.75f, 0.30f),
                Launched = new[] { "This launch is a rug pull!", "Buy the dip! I AM the dip!", "Somebody short me!" },
                Fell = new[] { "Down 100%! Diamond hands, bro!", "Mint THAT as an NFT." },
                Finished = new[] { "To the moon, losers!", "Generational wealth, baby!" },
            },
            new Persona
            {
                Name = "Karen Kollision", Color = new Color(0.75f, 0.60f, 0.95f),
                Launched = new[] { "I did NOT consent to this trajectory!", "Where is the manager of gravity?!", "One star. ONE STAR!" },
                Fell = new[] { "I want to speak to whoever built this pit!", "My lawyer will hear about this!" },
                Finished = new[] { "Finally. Some competence.", "I expect a refund anyway." },
            },
            new Persona
            {
                Name = "Uncle Dale", Color = new Color(0.55f, 0.50f, 0.25f),
                Launched = new[] { "Hold my beer. No wait, give it b—AAAH!", "Who moved the damn ground?!", "Ain't the first time I've flown drunk!" },
                Fell = new[] { "Somebody grab my cooler!", "Worth it." },
                Finished = new[] { "Hell yeah, brother!", "Somebody fire up the grill!" },
            },
            new Persona
            {
                Name = "Gramps", Color = new Color(0.70f, 0.66f, 0.58f),
                Launched = new[] { "In my day we got launched uphill both ways!", "Get off my damn lawn!", "My hip! My plastic hip!" },
                Fell = new[] { "I'm writing my congressman!", "Back in my day pits had RAILINGS!" },
                Finished = new[] { "Still got it, you little shits!", "Now where'd I park?" },
            },
            new Persona
            {
                Name = "Tyler", Color = new Color(0.50f, 0.25f, 0.80f),
                Launched = new[] { "Chat, CHAT, clip that!", "Lag! That was lag!", "Mom, is the Wi-Fi down?!" },
                Fell = new[] { "That's going in the fail compilation...", "I'm ending stream." },
                Finished = new[] { "GG EZ. Like and subscribe.", "Four viewers saw that. FOUR!" },
            },
            new Persona
            {
                Name = "Brittani", Color = new Color(1f, 0.40f, 0.70f),
                Launched = new[] { "Wait, launch me again, the lighting was trash.", "Don't tag me in this!", "This is SO going on my story!" },
                Fell = new[] { "Ugh, my followers can NOT see this.", "This is not my aesthetic." },
                Finished = new[] { "Link in bio, losers!", "Smash that follow button!" },
            },
            new Persona
            {
                Name = "Sarge", Color = new Color(0.15f, 0.25f, 0.55f),
                Launched = new[] { "Tactical retreat! TACTICAL RETREAT!", "Man down! Man DOWN!", "I'm calling for backup!" },
                Fell = new[] { "Tell the food court... I tried.", "This is a code red, people!" },
                Finished = new[] { "Mission accomplished. Mall secured.", "Nobody gets past Sarge!" },
            },
        };
    }
}
