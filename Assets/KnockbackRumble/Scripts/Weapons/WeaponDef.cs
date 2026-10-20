using UnityEngine;

namespace KnockbackRumble
{
    public enum WeaponKind { Melee, Hitscan, Rocket, Shotgun }

    /// <summary>
    /// Balance numbers for each weapon. "knockback" is launch speed at 0% Dent;
    /// "dent" is how much Dent one hit adds. Tweak these first when tuning feel.
    /// </summary>
    public class WeaponDef
    {
        public byte netId;             // 1 shove, 2 pistol, 3 rocket, 4 boom stick, 5 long shot (0 = none)
        public string id;
        public string name;
        public string description;
        public int unlockLevel = 1;
        public float zoomFov = 52f;    // camera field of view while aiming (right mouse)
        public int pellets;            // shotgun: how many rays in the cone
        public float spread;           // shotgun: cone half-angle in degrees
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
            netId = 1, id = "shove", name = "Shove", kind = WeaponKind.Melee,
            cooldown = 0.8f, knockback = 9f, dent = 6f, minLift = 0.3f,
            range = 1.1f, radius = 1.0f, color = Palette.White,
        };

        public static readonly WeaponDef PopPistol = new WeaponDef
        {
            netId = 2, id = "pistol", name = "Pop Pistol", kind = WeaponKind.Hitscan, ammo = 10,
            cooldown = 0.3f, knockback = 5.5f, dent = 5f, minLift = 0.2f,
            range = 60f, color = new Color(0.25f, 0.85f, 0.95f), unlockLevel = 1,
            description = "Fast, light pushes. Juggle people toward the edge.",
        };

        public static readonly WeaponDef BoomStick = new WeaponDef
        {
            netId = 4, id = "boomstick", name = "Boom Stick", kind = WeaponKind.Shotgun, ammo = 5,
            cooldown = 0.9f, knockback = 22f, dent = 16f, minLift = 0.3f, range = 10f, pellets = 7, spread = 14f,
            color = new Color(0.95f, 0.6f, 0.15f), unlockLevel = 1, zoomFov = 60f,
            description = "Huge launch up close. Useless from far away.",
        };

        public static readonly WeaponDef LongShot = new WeaponDef
        {
            netId = 5, id = "longshot", name = "Long Shot", kind = WeaponKind.Hitscan, ammo = 3,
            cooldown = 1.3f, knockback = 24f, dent = 24f, minLift = 0.3f, range = 250f,
            color = new Color(0.7f, 0.4f, 0.95f), unlockLevel = 5, zoomFov = 18f,
            description = "3 shots. Hold right mouse to scope in and snipe people across the map.",
        };

        public static readonly WeaponDef RocketLauncher = new WeaponDef
        {
            netId = 3, id = "rocket", name = "Rocket Launcher", kind = WeaponKind.Rocket, ammo = 3,
            cooldown = 0.9f, knockback = 16f, dent = 18f, minLift = 0.35f,
            radius = 4f, projectileSpeed = 30f, color = new Color(0.95f, 0.25f, 0.2f), unlockLevel = 3,
            description = "Big splash. Shoot your feet + jump to rocket jump.",
        };

        public static WeaponDef ById(byte netId)
        {
            switch (netId)
            {
                case 1: return Shove;
                case 2: return PopPistol;
                case 3: return RocketLauncher;
                case 4: return BoomStick;
                case 5: return LongShot;
                default: return null;
            }
        }

        public static WeaponDef ByName(string name)
        {
            if (name == Shove.name) return Shove;
            if (name == PopPistol.name) return PopPistol;
            if (name == RocketLauncher.name) return RocketLauncher;
            if (name == BoomStick.name) return BoomStick;
            if (name == LongShot.name) return LongShot;
            return null;
        }

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
