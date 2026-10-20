using UnityEngine;

namespace KnockbackRumble
{
    public enum HeckleKind : byte { Tomato = 1, Boo = 2, Horn = 3, Fart = 4 }

    /// <summary>
    /// Spectators (finished, or knocked out of Crumble Plaza) can heckle whoever they're
    /// watching: everyone sees a pop-up over the target's head and hears it there, and the
    /// target gets it on their screen (a tomato actually splats across your view).
    /// Purely for fun; nothing here changes the score.
    /// </summary>
    public static class Heckles
    {
        public const float Cooldown = 2.5f;
        static readonly NetWriter writer = new NetWriter();

        public static string Label(HeckleKind k)
        {
            switch (k)
            {
                case HeckleKind.Tomato: return "TOMATO";
                case HeckleKind.Boo: return "BOO";
                case HeckleKind.Horn: return "AIR HORN";
                default: return "FART";
            }
        }

        static string PopupText(HeckleKind k)
        {
            switch (k)
            {
                case HeckleKind.Tomato: return "SPLAT!";
                case HeckleKind.Boo: return "BOOOO!";
                case HeckleKind.Horn: return "HONK!";
                default: return "PFFFT!";
            }
        }

        static Color PopupColor(HeckleKind k)
        {
            switch (k)
            {
                case HeckleKind.Tomato: return new Color(0.95f, 0.25f, 0.15f);
                case HeckleKind.Boo: return new Color(0.75f, 0.7f, 1f);
                case HeckleKind.Horn: return Palette.Gold;
                default: return new Color(0.6f, 0.85f, 0.3f);
            }
        }

        static string Sound(HeckleKind k)
        {
            switch (k)
            {
                case HeckleKind.Tomato: return "tomato";
                case HeckleKind.Boo: return "boo";
                case HeckleKind.Horn: return "go_horn";
                default: return "fart";
            }
        }

        /// <summary>Heckle a contestant (call on the heckler's machine).</summary>
        public static void Send(Contestant from, Contestant target, HeckleKind kind)
        {
            if (from == null || target == null) return;
            Apply(from, target, kind);
            if (Net.Online)
                Net.Session.Send(writer.Begin(MsgType.Heckle).Byte(from.NetId).Byte(target.NetId).Byte((byte)kind));
        }

        /// <summary>Show and play a heckle (every machine).</summary>
        public static void Apply(Contestant from, Contestant target, HeckleKind kind)
        {
            if (target == null) return;
            HecklePopup.Spawn(target, PopupText(kind), PopupColor(kind));
            Sfx.At(Sound(kind), target.transform.position + Vector3.up * 2f, 1f, 0.12f, 35f);
            if (kind == HeckleKind.Tomato) Fx.Burst(target.transform.position + Vector3.up * 1.8f, new Color(0.9f, 0.15f, 0.1f), 8, 4f, 0.14f);

            if (target.IsPlayer)
            {
                string who = from != null ? from.DisplayName : "Someone";
                string what = kind == HeckleKind.Tomato ? " threw a tomato at you!"
                            : kind == HeckleKind.Boo ? " booed you!"
                            : kind == HeckleKind.Horn ? " blasted an air horn at you!"
                            : " farted at you!";
                Announcer.ShowToast(who + what);
                HUD.Heckled(kind);
            }
        }
    }
}
