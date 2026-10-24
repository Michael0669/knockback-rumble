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
        public byte netId;             // 1 shove, 2 pistol, 3 rocket, 4 boom stick, 5 long shot, 6-10 see below (0 = none)
        public string id;
        public string name;
        public string description;
        /// <summary>Wallet cash to unlock it in the loadout screen (0 = free from the start).</summary>
        public int price;
        /// <summary>Hitscan guns: random aim wobble in degrees (Spray-n-Pray).</summary>
        public float wobble;
        /// <summary>If 0 or more: how long one hit stuns (the Leaf Blower barely stuns, so it pushes instead of locking you).</summary>
        public float stunOverride = -1f;
        public float zoomFov = 52f;    // camera field of view while aiming (right mouse)
        public int pellets;            // shotgun: how many rays in the cone (0 = no tracers: Leaf Blower)
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
            range = 60f, color = new Color(0.25f, 0.85f, 0.95f),
            description = "Fast, light pushes. Juggle people toward the edge.",
        };

        public static readonly WeaponDef BoomStick = new WeaponDef
        {
            netId = 4, id = "boomstick", name = "Boom Stick", kind = WeaponKind.Shotgun, ammo = 5,
            cooldown = 0.9f, knockback = 22f, dent = 16f, minLift = 0.3f, range = 10f, pellets = 7, spread = 14f,
            color = new Color(0.95f, 0.6f, 0.15f), price = 3000, zoomFov = 60f,
            description = "Huge launch up close. Useless from far away.",
        };

        public static readonly WeaponDef LongShot = new WeaponDef
        {
            netId = 5, id = "longshot", name = "Long Shot", kind = WeaponKind.Hitscan, ammo = 3,
            cooldown = 1.3f, knockback = 24f, dent = 24f, minLift = 0.3f, range = 250f,
            color = new Color(0.7f, 0.4f, 0.95f), price = 5000, zoomFov = 18f,
            description = "3 shots. Hold right mouse to scope in and snipe people across the map.",
        };

        public static readonly WeaponDef RocketLauncher = new WeaponDef
        {
            netId = 3, id = "rocket", name = "Rocket Launcher", kind = WeaponKind.Rocket, ammo = 3,
            cooldown = 0.9f, knockback = 16f, dent = 18f, minLift = 0.35f,
            radius = 4f, projectileSpeed = 30f, color = new Color(0.95f, 0.25f, 0.2f), price = 6000,
            description = "Big splash. Shoot your feet + jump to rocket jump.",
        };

        public static readonly WeaponDef BonkRifle = new WeaponDef
        {
            netId = 6, id = "bonk", name = "Bonk Rifle", kind = WeaponKind.Hitscan, ammo = 8,
            cooldown = 0.45f, knockback = 9f, dent = 8f, minLift = 0.25f, range = 90f,
            color = new Color(0.4f, 0.85f, 0.35f), price = 1500, zoomFov = 40f,
            description = "Solid all-rounder. Steady pushes at any range.",
        };

        public static readonly WeaponDef SprayNPray = new WeaponDef
        {
            netId = 7, id = "spray", name = "Spray-n-Pray", kind = WeaponKind.Hitscan, ammo = 30,
            cooldown = 0.085f, knockback = 3.2f, dent = 2.6f, minLift = 0.15f, range = 45f, wobble = 4f,
            color = new Color(1f, 0.85f, 0.2f), price = 2500, zoomFov = 46f,
            description = "30 rounds of pure chaos. Hold the trigger and hope.",
        };

        public static readonly WeaponDef HandCannon = new WeaponDef
        {
            netId = 8, id = "cannon", name = "Hand Cannon", kind = WeaponKind.Hitscan, ammo = 6,
            cooldown = 0.75f, knockback = 15f, dent = 13f, minLift = 0.3f, range = 70f,
            color = new Color(0.85f, 0.75f, 0.55f), price = 3500, zoomFov = 38f,
            description = "Six big slugs. Every hit is a shove from a truck.",
        };

        public static readonly WeaponDef LeafBlower = new WeaponDef
        {
            netId = 9, id = "blower", name = "Leaf Blower", kind = WeaponKind.Shotgun, ammo = 60,
            cooldown = 0.07f, knockback = 2.6f, dent = 0.6f, minLift = 0.12f, stunOverride = 0.12f, range = 9f, pellets = 0, spread = 16f,
            color = new Color(0.95f, 0.45f, 0.15f), price = 2000, zoomFov = 56f,
            description = "Hold to blow people off the edge. Barely hurts, never stops.",
        };

        public static readonly WeaponDef BigFlush = new WeaponDef
        {
            netId = 10, id = "flush", name = "The Big Flush", kind = WeaponKind.Rocket, ammo = 2,
            cooldown = 1.6f, knockback = 24f, dent = 26f, minLift = 0.4f,
            radius = 6.5f, projectileSpeed = 17f, color = new Color(0.55f, 0.35f, 0.2f), price = 8000,
            description = "Fires a whole toilet. Slow, huge, and absolutely disgusting.",
        };

        /// <summary>Every gun, cheapest first (the loadout screen and random pickers use this).</summary>
        public static readonly WeaponDef[] Guns =
        {
            PopPistol, BonkRifle, LeafBlower, SprayNPray, BoomStick, HandCannon, LongShot, RocketLauncher, BigFlush,
        };

        public static WeaponDef ById(byte netId)
        {
            if (netId == 1) return Shove;
            foreach (var g in Guns) if (g.netId == netId) return g;
            return null;
        }

        public static WeaponDef ByName(string name)
        {
            if (name == Shove.name) return Shove;
            foreach (var g in Guns) if (g.name == name) return g;
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
