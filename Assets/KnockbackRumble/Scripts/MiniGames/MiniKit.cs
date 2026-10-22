using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Building blocks shared by the mini-game arenas.</summary>
    public static class MiniKit
    {
        public static Material M(string name, Color c) => Mats.Get(name, c);
        public static Material Glow(string name, Color c) => Mats.Unlit(name, c);

        public static GameObject Block(Transform g, string name, Vector3 pos, Vector3 size, Material mat, bool collider = true, Vector3? euler = null) =>
            Shapes.Make(PrimitiveType.Cube, name, g, pos, size, mat, collider, euler);

        /// <summary>A round platform you can stand on (top at pos.y + height/2).</summary>
        public static GameObject Disc(Transform g, string name, Vector3 pos, float radius, float height, Material mat, int sides = 28) =>
            LowPoly.MakeSolid(LowPoly.Cylinder(sides), name, g, pos, new Vector3(radius * 2f, height, radius * 2f), mat);

        public static GameObject Visual(Transform g, Mesh mesh, string name, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null) =>
            LowPoly.Make(mesh, name, g, pos, scale, mat, euler);

        public static GameObject Sign(Transform g, string text, Vector3 pos, float size, Color color) => Props.Sign(g, text, pos, size, color);

        /// <summary>A sign turned to face along yaw (text reads from the -Z side at yaw 0).</summary>
        public static GameObject Sign(Transform g, string text, Vector3 pos, float size, Color color, float yaw)
        {
            var s = Props.Sign(g, text, pos, size, color);
            if (s != null) s.transform.localEulerAngles = new Vector3(0f, yaw, 0f);
            return s;
        }

        public static void Pickup(Transform g, Vector3 localPos)
        {
            var go = new GameObject("Pickup_ammo");
            go.transform.SetParent(g, false);
            go.transform.localPosition = localPos;
            go.AddComponent<WeaponPickup>().Setup("ammo");
        }

        /// <summary>Spawn points in a ring (world positions).</summary>
        public static Vector3[] Ring(Vector3 center, int count, float radius, float y = 0.05f, float offset = 0f)
        {
            var list = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float a = ((i + offset) / count) * Mathf.PI * 2f;
                list[i] = center + new Vector3(Mathf.Sin(a) * radius, y, Mathf.Cos(a) * radius);
            }
            return list;
        }

        /// <summary>Two rings of 8 (16 spots), like the classic arenas.</summary>
        public static Vector3[] TwoRings(Vector3 center, float inner, float outer, float y = 0.05f)
        {
            var a = Ring(center, 8, inner, y);
            var b = Ring(center, 8, outer, y, 0.5f);
            var all = new Vector3[16];
            a.CopyTo(all, 0);
            b.CopyTo(all, 8);
            return all;
        }

        /// <summary>A grid of start spots facing +Z (race games), world positions.</summary>
        public static Vector3[] StartGrid(Vector3 origin, float z, float width, float y = 0.05f)
        {
            var list = new List<Vector3>();
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 8; i++)
                    list.Add(origin + new Vector3(-width * 0.5f + width * (i + 0.5f) / 8f, y, z - row * 2.2f));
            return list.ToArray();
        }

        /// <summary>
        /// The Losers' Lounge: a floating balcony where knocked-out dummies watch (and heckle).
        /// pos is local to the arena; yaw turns it so the open side faces the action
        /// (0 = looking toward -Z). Sets info.loungePoint.
        /// </summary>
        public static void Lounge(Transform g, CourseInfo info, Vector3 pos, float yaw = 0f)
        {
            var floor = M("Metal", Palette.Metal);
            var couch = M("Couch", new Color(0.55f, 0.25f, 0.45f));
            var l = new GameObject("Lounge").transform;
            l.SetParent(g, false);
            l.localPosition = pos;
            l.localEulerAngles = new Vector3(0f, yaw, 0f);
            Block(l, "Lounge_Floor", Vector3.down * 0.5f, new Vector3(16f, 1f, 7f), floor);
            Block(l, "Lounge_Carpet", new Vector3(0f, 0.006f, 0f), new Vector3(15f, 0.01f, 6f), M("Lounge_Carpet", new Color(0.35f, 0.15f, 0.4f)), false);
            Kit.RailingAlong(l, new Vector3(-8f, 0f, -3.5f), new Vector3(8f, 0f, -3.5f));
            Kit.RailingAlong(l, new Vector3(-8f, 0f, -3.5f), new Vector3(-8f, 0f, 3.5f));
            Kit.RailingAlong(l, new Vector3(8f, 0f, -3.5f), new Vector3(8f, 0f, 3.5f));
            Block(l, "Lounge_Back", new Vector3(0f, 1.5f, 3.7f), new Vector3(16f, 3f, 0.4f), M("Lounge_Wall", new Color(0.25f, 0.2f, 0.3f)));
            for (int i = 0; i < 2; i++)
            {
                Vector3 c = new Vector3(-4f + i * 8f, 0f, 2.2f);
                var seat = Block(l, "Couch_Seat", c + Vector3.up * 0.3f, new Vector3(3.4f, 0.6f, 1.4f), couch);
                var back = Block(l, "Couch_Back", c + new Vector3(0f, 0.9f, 0.6f), new Vector3(3.4f, 1.2f, 0.4f), couch);
                // The pack's couch, if we have it (the blocks stay as invisible seats).
                if (Art.Prop("Environment/Couch", l, c, 180f, 3.4f, Art.Fit.Length) != null)
                {
                    seat.GetComponent<Renderer>().enabled = false;
                    back.GetComponent<Renderer>().enabled = false;
                }
            }
            Kit.IBeam(l, new Vector3(-5f, -23f, 0f), new Vector3(-5f, -1f, 0f), 1f, Kit.Girder);
            Kit.IBeam(l, new Vector3(5f, -23f, 0f), new Vector3(5f, -1f, 0f), 1f, Kit.Girder);
            Sign(l, "LOSERS' LOUNGE", new Vector3(0f, 2.3f, 3.45f), 0.22f, Palette.Gold);
            info.loungePoint = l.TransformPoint(new Vector3(0f, 0.1f, -0.5f));
        }

        /// <summary>
        /// The TV-show dressing around an arena: a pit far below, three grandstands, light towers,
        /// mountains and a blimp, and the title sign. "reach" is roughly the arena's half-size.
        /// </summary>
        public static void Dress(Transform g, float reach, int seed, string title, string subtitle, string blimp, Color pit, bool northStand = false)
        {
            Block(g, "Pit", new Vector3(0f, -14f, 0f), new Vector3(reach * 6f, 1f, reach * 6f), M("MiniPit_" + ColorUtility.ToHtmlStringRGB(pit), pit), false);
            var rng = new System.Random(seed);
            float d = reach + 12f;
            Props.Grandstand(g, new Vector3(0f, -1f, -d), 0f, reach * 2f, 5, rng, 13f);
            Props.Grandstand(g, new Vector3(-d, -1f, 0f), 90f, reach * 2f, 5, rng, 13f);
            Props.Grandstand(g, new Vector3(d, -1f, 0f), -90f, reach * 2f, 5, rng, 13f);
            if (northStand) Props.Grandstand(g, new Vector3(0f, -1f, d), 180f, reach * 2f, 5, rng, 13f);
            float t = reach + 6f;
            foreach (var c in new[] { new Vector3(-t, -14f, -t), new Vector3(t, -14f, -t), new Vector3(-t, -14f, t), new Vector3(t, -14f, t) })
                Props.LightTower(g, c, 28f, Vector3.zero);
            Props.Backdrop(g, Vector3.zero, reach + 40f, seed, blimp);
            // Junkyard and street bits from the pack down in the pit and around the stands.
            Art.Prop("Environment/WaterTower", g, new Vector3(-reach - 26f, -14f, reach + 20f), 30f, 26f);
            string[] cars = { "Vehicles/Vehicle_Pickup", "Vehicles/Vehicle_Truck", "Vehicles/Vehicle_Sports_Armored", "Vehicles/Vehicle_Truck_Armored" };
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 30f + seed % 40) * Mathf.Deg2Rad;
                Art.Prop(cars[i], g, new Vector3(Mathf.Sin(a) * (reach + 4f), -14f, Mathf.Cos(a) * (reach + 4f)), i * 77f + seed % 90, 4.6f, Art.Fit.Length);
            }
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(i < 2 ? -reach - 5f : reach + 5f, -14f, i % 2 == 0 ? -reach * 0.5f : reach * 0.5f);
                Art.Prop("Environment/StreetLights", g, p, i < 2 ? 90f : -90f, 9f);
                Art.Prop(i % 2 == 0 ? "Environment/TrashBag_1" : "Environment/Pallet", g, p + new Vector3(0f, 0f, 2f), i * 50f, 1.2f, Art.Fit.Largest);
            }
            Sign(g, title, new Vector3(0f, 17f, reach + 14f), 0.6f, Palette.Gold);
            if (!string.IsNullOrEmpty(subtitle)) Sign(g, subtitle, new Vector3(0f, 14.6f, reach + 14f), 0.16f, Color.white);
        }

        /// <summary>Hazard stripes around the rim of a round platform.</summary>
        public static void StripeRim(Transform g, Vector3 center, float radius, float y, int count = 32)
        {
            var yellow = M("Stripe", Palette.Stripe);
            var black = M("Dummy_Black", Palette.Black);
            float seg = 2f * Mathf.PI * radius / count;
            for (int i = 0; i < count; i++)
            {
                float a = (i + 0.5f) / count * 360f;
                Vector3 p = center + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, y, radius - 0.35f);
                Block(g, "Rim", p, new Vector3(seg * 0.98f, 0.02f, 0.7f), i % 2 == 0 ? yellow : black, false, new Vector3(0f, a, 0f));
            }
        }

        /// <summary>Flat ring decal (a torus squashed flat) used for zones and target markers.</summary>
        public static GameObject FlatRing(Transform g, string name, Vector3 pos, float radius, Material mat, float thickness = 0.12f) =>
            LowPoly.Make(LowPoly.Torus(32, 4, thickness), name, g, pos, new Vector3(radius * 2f, 0.15f, radius * 2f), mat);

        /// <summary>A finish line across a race course (trigger + checkered strip + arch). pos = middle of the line on the floor.</summary>
        public static void FinishGate(Transform g, Vector3 pos, float width)
        {
            var go = new GameObject("FinishLine");
            go.transform.SetParent(g, false);
            go.transform.localPosition = pos;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(width, 4f, 1.2f);
            box.center = new Vector3(0f, 2f, 0f);
            go.AddComponent<FinishLine>();
            var white = M("White", Palette.White);
            var black = M("Dummy_Black", Palette.Black);
            int n = Mathf.Max(2, Mathf.RoundToInt(width / 0.8f));
            for (int i = 0; i < n; i++)
                for (int row = 0; row < 2; row++)
                    Block(g, "Checker", pos + new Vector3(-width * 0.5f + (i + 0.5f) * width / n, 0.012f, -0.3f + row * 0.6f),
                        new Vector3(width / n, 0.01f, 0.6f), (i + row) % 2 == 0 ? white : black, false);
            Kit.Arch(g, pos, width + 1f, 6f, M("Gold", Palette.Gold), new Color(1f, 0.9f, 0.5f));
            Sign(g, "FINISH", pos + new Vector3(0f, 6.6f, -0.45f), 0.22f, Palette.Gold);
        }

        /// <summary>A checkpoint across a race course (respawn here after falling).</summary>
        public static void CheckpointGate(Transform g, Vector3 pos, float width, int index)
        {
            var go = new GameObject("Checkpoint_" + index);
            go.transform.SetParent(g, false);
            go.transform.localPosition = pos;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(width, 4f, 1.5f);
            box.center = new Vector3(0f, 2f, 0f);
            go.AddComponent<Checkpoint>().index = index;
            var flag = M("Checkpoint", Palette.Checkpoint);
            foreach (float side in new[] { -1f, 1f })
            {
                LowPoly.Make(LowPoly.Cylinder(8), "Pole", g, pos + new Vector3(side * (width * 0.5f + 0.3f), 1.5f, 0f), new Vector3(0.2f, 3f, 0.2f), Kit.Steel);
                Block(g, "Flag", pos + new Vector3(side * (width * 0.5f + 0.75f), 2.6f, 0f), new Vector3(0.9f, 0.6f, 0.04f), flag, false);
            }
        }

        // ---------- Runtime helpers ----------

        /// <summary>Apply a hit to every local dummy within radius of p (explosions). Returns how many were hit.</summary>
        public static int Blast(Vector3 p, float radius, float knock, float dent, string what)
        {
            int n = 0;
            foreach (var body in DentBody.All)
            {
                if (body == null || !body.IsLocal) continue;
                Vector3 to = body.Center - p;
                if (to.magnitude > radius) continue;
                float falloff = 1f - Mathf.Clamp01(to.magnitude / radius) * 0.5f;
                body.Hit(Shapes.Flat(to) + Vector3.up * 0.6f, knock * falloff, dent, null, what, 0.45f);
                n++;
            }
            return n;
        }

        /// <summary>Big boom effect + sound + shake (every machine).</summary>
        public static void BoomFx(Vector3 p, float radius) => Fx.Explosion(p, radius);

        /// <summary>Is this dummy standing on something (raycast from its feet)?</summary>
        public static bool OnGround(Contestant c) => c != null && c.Motor.Grounded;
    }
}
