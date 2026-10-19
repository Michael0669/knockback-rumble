using UnityEngine;

namespace KnockbackRumble
{
    public enum WeaponKind { Melee, Hitscan, Rocket }

    /// <summary>
    /// Balance numbers for each weapon. "knockback" is launch speed at 0% Dent;
    /// "dent" is how much Dent one hit adds. Tweak these first when tuning feel.
    /// </summary>
    public class WeaponDef
    {
        public string id;
        public string name;
        public WeaponKind kind;
        public int ammo;
        public float cooldown;
        public float knockback;
        public float dent;
        public float minLift;          // minimum upward share of the launch
        public float range;            // hitscan range / melee reach
        public float radius;           // melee hit radius / explosion radius
        public float projectileSpeed;
        public Color color;

        public static readonly WeaponDef Shove = new WeaponDef
        {
            id = "shove", name = "Shove", kind = WeaponKind.Melee,
            cooldown = 0.8f, knockback = 9f, dent = 6f, minLift = 0.3f,
            range = 1.1f, radius = 1.0f, color = Palette.White,
        };

        public static readonly WeaponDef PopPistol = new WeaponDef
        {
            id = "pistol", name = "Pop Pistol", kind = WeaponKind.Hitscan, ammo = 8,
            cooldown = 0.3f, knockback = 4.5f, dent = 5f, minLift = 0.2f,
            range = 60f, color = new Color(0.25f, 0.85f, 0.95f),
        };

        public static readonly WeaponDef RocketLauncher = new WeaponDef
        {
            id = "rocket", name = "Rocket Launcher", kind = WeaponKind.Rocket, ammo = 3,
            cooldown = 0.9f, knockback = 16f, dent = 18f, minLift = 0.35f,
            radius = 4f, projectileSpeed = 30f, color = new Color(0.95f, 0.25f, 0.2f),
        };

        /// <summary>"pistol", "rocket", or "random" (55% pistol / 45% rocket).</summary>
        public static WeaponDef FromId(string id)
        {
            switch (id)
            {
                case "pistol": return PopPistol;
                case "rocket": return RocketLauncher;
                default: return Random.value < 0.55f ? PopPistol : RocketLauncher;
            }
        }
    }
}
