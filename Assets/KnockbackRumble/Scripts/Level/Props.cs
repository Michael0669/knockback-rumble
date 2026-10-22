using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Low-poly set dressing shared by every arena: junk (tires, oil drums, wrecked cars,
    /// rocks), the TV-show stuff (grandstands full of fans, light towers, a jumbotron,
    /// a sponsor blimp) and the horizon (scrap mountains, clouds). All visual only: no colliders.
    /// Positions are local to the group you pass in.
    /// </summary>
    public static class Props
    {
        static Material M(string name, Color c) => Mats.Get(name, c);
        static Material Rubber => M("Prop_Rubber", new Color(0.12f, 0.12f, 0.13f));
        static Material Steel => M("Gun_Steel", new Color(0.55f, 0.57f, 0.62f));
        static Material DarkSteel => M("Prop_DarkSteel", new Color(0.3f, 0.31f, 0.34f));
        static Material RustMat => M("Rust", Palette.Rust);

        static readonly Color[] CarColors =
        {
            new Color(0.75f, 0.2f, 0.18f), new Color(0.2f, 0.45f, 0.75f), new Color(0.85f, 0.7f, 0.2f),
            new Color(0.3f, 0.6f, 0.35f), new Color(0.6f, 0.6f, 0.62f), new Color(0.55f, 0.3f, 0.6f),
        };

        static readonly Color[] FanColors =
        {
            new Color(1f, 0.35f, 0.3f), new Color(0.3f, 0.6f, 1f), new Color(1f, 0.85f, 0.25f), new Color(0.4f, 0.9f, 0.45f),
            new Color(0.95f, 0.5f, 0.9f), new Color(1f, 0.6f, 0.2f), new Color(0.9f, 0.9f, 0.9f), new Color(0.35f, 0.3f, 0.4f),
        };

        // ---------------------------------------------------------------- junk

        public static void Tire(Transform g, Vector3 pos, float size = 1f)
        {
            if (Art.Prop("Environment/Wheel", g, pos, pos.x * 37f % 360f, 1.6f * size, Art.Fit.Largest) != null) return;
            LowPoly.Make(LowPoly.Torus(12, 5, 0.42f), "Tire", g, pos + Vector3.up * 0.35f * size, new Vector3(1.6f, 1.9f, 1.6f) * size, Rubber);
        }

        public static void TireStack(Transform g, Vector3 pos, int count, System.Random rng)
        {
            if (Art.Prop("Environment/Wheels_Stack", g, pos, (float)rng.NextDouble() * 360f, Mathf.Max(1, count) * 0.62f + 0.3f) != null) return;
            for (int i = 0; i < count; i++)
            {
                var off = new Vector3((float)rng.NextDouble() * 0.3f - 0.15f, i * 0.62f, (float)rng.NextDouble() * 0.3f - 0.15f);
                Tire(g, pos + off);
            }
        }

        public static void Drum(Transform g, Vector3 pos, Color color, bool tipped = false)
        {
            if (!tipped && Art.Prop("Environment/Barrel", g, pos, pos.z * 53f % 360f, 1.7f) != null) return;
            var mat = M("Prop_Drum_" + ColorUtility.ToHtmlStringRGB(color), color);
            Vector3 euler = tipped ? new Vector3(0f, 0f, 90f) : Vector3.zero;
            Vector3 p = pos + Vector3.up * (tipped ? 0.6f : 0.85f);
            var d = LowPoly.Make(LowPoly.Cylinder(10), "Drum", g, p, new Vector3(1.2f, 1.7f, 1.2f), mat, euler);
            LowPoly.Make(LowPoly.Torus(12, 3, 0.08f), "Band", d.transform, new Vector3(0f, 0.22f, 0f), new Vector3(1.04f, 0.1f, 1.04f), DarkSteel);
            LowPoly.Make(LowPoly.Torus(12, 3, 0.08f), "Band", d.transform, new Vector3(0f, -0.22f, 0f), new Vector3(1.04f, 0.1f, 1.04f), DarkSteel);
        }

        public static void CarWreck(Transform g, Vector3 pos, float yaw, Color color, float tilt = 0f)
        {
            var root = new GameObject("CarWreck").transform;
            root.SetParent(g, false);
            root.localPosition = pos;
            root.localEulerAngles = new Vector3(0f, yaw, tilt);
            // A real (wrecked-looking) vehicle from the pack if we have one.
            string[] cars = { "Vehicle_Pickup", "Vehicle_Sports", "Vehicle_Truck", "Vehicle_Pickup_Armored", "Vehicle_Sports_Armored", "Vehicle_Truck_Armored" };
            int pick = Mathf.Abs(Mathf.RoundToInt(pos.x * 7f + pos.z * 13f + yaw)) % cars.Length;
            if (Art.Prop("Vehicles/" + cars[pick], root, Vector3.zero, 0f, 4.4f, Art.Fit.Length) != null) return;
            var paint = M("Prop_Car_" + ColorUtility.ToHtmlStringRGB(color), color);
            var glass = M("Prop_Glass", new Color(0.25f, 0.35f, 0.45f));
            Shapes.Make(PrimitiveType.Cube, "Body", root, new Vector3(0f, 0.75f, 0f), new Vector3(2f, 0.9f, 4.2f), paint, false);
            Shapes.Make(PrimitiveType.Cube, "Cabin", root, new Vector3(0f, 1.55f, -0.3f), new Vector3(1.8f, 0.75f, 2.1f), paint, false,
                new Vector3(0f, 0f, 3f));
            Shapes.Make(PrimitiveType.Cube, "Windows", root, new Vector3(0f, 1.55f, -0.3f), new Vector3(1.84f, 0.5f, 1.9f), glass, false,
                new Vector3(0f, 0f, 3f));
            Shapes.Make(PrimitiveType.Cube, "Bumper", root, new Vector3(0f, 0.45f, 2.15f), new Vector3(2.1f, 0.25f, 0.2f), Steel, false);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -1f : 1f, z = i < 2 ? 1.35f : -1.35f;
                LowPoly.Make(LowPoly.Cylinder(8), "Wheel", root, new Vector3(x, 0.4f, z), new Vector3(0.8f, 0.3f, 0.8f), Rubber,
                    new Vector3(0f, 0f, 90f));
            }
        }

        public static void Rock(Transform g, Vector3 pos, float size, Material mat, int variant = 0)
        {
            LowPoly.Make(LowPoly.Rock(variant % 3), "Rock", g, pos + Vector3.up * size * 0.3f, new Vector3(size, size * 0.75f, size), mat,
                new Vector3(0f, variant * 47f, 0f));
        }

        /// <summary>A heap of scrap: lumpy rust and steel chunks with junk stuck in it.</summary>
        public static void ScrapPile(Transform g, Vector3 pos, float size, System.Random rng)
        {
            Material[] mats = { RustMat, DarkSteel, M("Prop_Junk", new Color(0.45f, 0.38f, 0.3f)) };
            int n = 3 + rng.Next(3);
            for (int i = 0; i < n; i++)
            {
                var off = new Vector3((float)rng.NextDouble() - 0.5f, 0f, (float)rng.NextDouble() - 0.5f) * size * 0.8f;
                float s = size * (0.5f + (float)rng.NextDouble() * 0.6f);
                Rock(g, pos + off, s, mats[rng.Next(mats.Length)], rng.Next(3));
            }
            if (rng.NextDouble() < 0.5) Tire(g, pos + new Vector3(size * 0.4f, size * 0.4f, 0f), 0.9f);
            // Bits of street junk from the pack.
            string[] junk = { "Environment/TrashBag_1", "Environment/TrashBag_2", "Environment/Pallet_Broken", "Environment/CinderBlock", "Environment/Pallet" };
            if (rng.NextDouble() < 0.6)
                Art.Prop(junk[rng.Next(junk.Length)], g, pos + new Vector3(-size * 0.5f, 0f, -size * 0.3f), (float)rng.NextDouble() * 360f, size * 0.35f, Art.Fit.Largest);
            if (rng.NextDouble() < 0.4) Drum(g, pos + new Vector3(-size * 0.4f, size * 0.25f, size * 0.2f), CarColors[rng.Next(CarColors.Length)], true);
        }

        public static void TrafficCone(Transform g, Vector3 pos)
        {
            if (Art.Prop("Environment/TrafficCone_1", g, pos, 0f, 0.8f) != null) return;
            var orange = M("Prop_ConeOrange", new Color(1f, 0.45f, 0.1f));
            Shapes.Make(PrimitiveType.Cube, "ConeBase", g, pos + Vector3.up * 0.04f, new Vector3(0.6f, 0.08f, 0.6f), orange, false);
            LowPoly.Make(LowPoly.Cone(8), "Cone", g, pos + Vector3.up * 0.42f, new Vector3(0.45f, 0.72f, 0.45f), orange);
            LowPoly.Make(LowPoly.Torus(10, 3, 0.2f), "Stripe", g, pos + Vector3.up * 0.45f, new Vector3(0.33f, 0.2f, 0.33f), M("White", Palette.White));
        }

        /// <summary>Pinball bumper look on a 1.6 x 2 x 1.6 cylinder: white cap, glowing ring, dark skirt.</summary>
        public static void DressBumper(Transform b)
        {
            LowPoly.Make(LowPoly.Cylinder(12), "Cap", b, new Vector3(0f, 1.02f, 0f), new Vector3(1.08f, 0.08f, 1.08f), M("White", Palette.White));
            LowPoly.Make(LowPoly.Torus(16, 4, 0.12f), "Glow", b, new Vector3(0f, 0.55f, 0f), new Vector3(1.12f, 0.3f, 1.12f),
                Mats.Unlit("BumperGlow", new Color(1f, 0.5f, 0.85f)));
            LowPoly.Make(LowPoly.Cylinder(12), "Skirt", b, new Vector3(0f, -0.9f, 0f), new Vector3(1.15f, 0.2f, 1.15f), M("Dummy_Black", Palette.Black));
        }

        // ---------------------------------------------------------------- the show

        /// <summary>Stepped bleachers packed with bouncing fans. Faces local +Z (the fans look that way).</summary>
        public static void Grandstand(Transform g, Vector3 pos, float yaw, float length, int rows, System.Random rng, float height = 10f)
        {
            var root = new GameObject("Grandstand").transform;
            root.SetParent(g, false);
            root.localPosition = pos;
            root.localEulerAngles = new Vector3(0f, yaw, 0f);
            var concrete = M("Prop_Concrete", new Color(0.62f, 0.6f, 0.58f));
            var seat = M("Prop_Seat", new Color(0.2f, 0.35f, 0.65f));
            var roof = M("Prop_Roof", new Color(0.85f, 0.25f, 0.25f));

            // Legs down into the pit
            for (float x = -length * 0.5f + 2f; x <= length * 0.5f - 2f; x += 8f)
                Shapes.Make(PrimitiveType.Cube, "Leg", root, new Vector3(x, -height * 0.5f, -1f), new Vector3(0.6f, height, 0.6f), DarkSteel, false);

            var crowd = new GameObject("Crowd").transform;
            crowd.SetParent(root, false);
            for (int r = 0; r < rows; r++)
            {
                float y = r * 0.9f, z = -r * 1.3f;
                Shapes.Make(PrimitiveType.Cube, "Step", root, new Vector3(0f, y - 0.25f, z), new Vector3(length, 0.5f + 0.0f, 1.3f), concrete, false);
                Shapes.Make(PrimitiveType.Cube, "Seats", root, new Vector3(0f, y + 0.08f, z - 0.35f), new Vector3(length - 0.4f, 0.16f, 0.4f), seat, false);
                for (float x = -length * 0.5f + 0.8f; x < length * 0.5f - 0.6f; x += 1.25f)
                {
                    if (rng.NextDouble() < 0.12) continue; // a few empty seats
                    var col = FanColors[rng.Next(FanColors.Length)];
                    var fan = Shapes.Make(PrimitiveType.Capsule, "Fan", crowd, new Vector3(x + (float)rng.NextDouble() * 0.3f, y + 0.55f, z),
                        new Vector3(0.55f, 0.42f, 0.5f), M("Prop_Fan_" + ColorUtility.ToHtmlStringRGB(col), col), false);
                    if (rng.NextDouble() < 0.08)
                        Shapes.Make(PrimitiveType.Cube, "Sign", fan.transform, new Vector3(0f, 1.6f, 0.25f), new Vector3(1.8f, 0.9f, 0.08f),
                            M("White", Palette.White), false);
                }
            }
            // Back wall and roof
            float back = -rows * 1.3f;
            Shapes.Make(PrimitiveType.Cube, "BackWall", root, new Vector3(0f, rows * 0.45f + 1.5f, back - 0.2f), new Vector3(length, rows * 0.9f + 3f, 0.4f), concrete, false);
            Shapes.Make(PrimitiveType.Cube, "Roof", root, new Vector3(0f, rows * 0.9f + 3.2f, back * 0.5f), new Vector3(length + 1f, 0.3f, -back + 2.5f), roof, false,
                new Vector3(-6f, 0f, 0f));
            crowd.gameObject.AddComponent<CrowdBob>();
        }

        /// <summary>A stadium light tower: lattice mast with a bank of glowing lamps aimed at "target".</summary>
        public static void LightTower(Transform g, Vector3 pos, float height, Vector3 lookAt)
        {
            var root = new GameObject("LightTower").transform;
            root.SetParent(g, false);
            root.localPosition = pos;
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.5f : 0.5f, z = i < 2 ? -0.5f : 0.5f;
                Shapes.Make(PrimitiveType.Cube, "Mast", root, new Vector3(x, height * 0.5f, z), new Vector3(0.18f, height, 0.18f), DarkSteel, false);
            }
            for (float y = 3f; y < height; y += 3f)
                Shapes.Make(PrimitiveType.Cube, "Brace", root, new Vector3(0f, y, 0f), new Vector3(1.2f, 0.12f, 1.2f), DarkSteel, false, new Vector3(0f, 45f, 0f));
            var head = new GameObject("Lamps").transform;
            head.SetParent(root, false);
            head.localPosition = new Vector3(0f, height + 1.2f, 0f);
            Vector3 dir = lookAt - pos;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) head.localRotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(20f, 0f, 0f);
            Shapes.Make(PrimitiveType.Cube, "Panel", head, Vector3.zero, new Vector3(4.4f, 2.6f, 0.4f), DarkSteel, false);
            var glow = Mats.Unlit("Prop_LampGlow", new Color(1f, 0.97f, 0.85f));
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 2; y++)
                    Shapes.Make(PrimitiveType.Cube, "Lamp", head, new Vector3(-1.5f + x, -0.6f + y * 1.2f, 0.22f), new Vector3(0.8f, 0.9f, 0.05f), glow, false);
        }

        /// <summary>A giant screen on legs with the show's name on it. Faces local -Z (readable from the course).</summary>
        public static void Jumbotron(Transform g, Vector3 pos, float yaw, string line1, string line2)
        {
            var root = new GameObject("Jumbotron").transform;
            root.SetParent(g, false);
            root.localPosition = pos;
            root.localEulerAngles = new Vector3(0f, yaw, 0f);
            Shapes.Make(PrimitiveType.Cube, "LegL", root, new Vector3(-8f, -18f, 0.5f), new Vector3(1f, 36f, 1f), DarkSteel, false);
            Shapes.Make(PrimitiveType.Cube, "LegR", root, new Vector3(8f, -18f, 0.5f), new Vector3(1f, 36f, 1f), DarkSteel, false);
            Shapes.Make(PrimitiveType.Cube, "Frame", root, Vector3.zero, new Vector3(22f, 12f, 1f), M("Prop_Frame", new Color(0.1f, 0.1f, 0.12f)), false);
            Shapes.Make(PrimitiveType.Cube, "Screen", root, new Vector3(0f, 0f, -0.52f), new Vector3(20.5f, 10.5f, 0.05f),
                Mats.Unlit("Prop_Screen", new Color(0.12f, 0.1f, 0.3f)), false);
            Sign(root, line1, new Vector3(0f, 1.8f, -0.6f), 0.55f, Palette.Gold);
            Sign(root, line2, new Vector3(0f, -2.6f, -0.6f), 0.22f, new Color(1f, 0.5f, 0.75f));
        }

        public static void Blimp(Transform g, Vector3 center, float radius, float height, string brand)
        {
            var orbit = new GameObject("Blimp").transform;
            orbit.SetParent(g, false);
            orbit.localPosition = center + Vector3.up * height;
            var body = new GameObject("Body").transform;
            body.SetParent(orbit, false);
            body.localPosition = new Vector3(radius, 0f, 0f);
            var skin = M("Prop_Blimp", new Color(0.85f, 0.85f, 0.88f));
            var red = M("Prop_BlimpRed", new Color(0.85f, 0.2f, 0.2f));
            LowPoly.Make(LowPoly.Ico(2), "Envelope", body, Vector3.zero, new Vector3(9f, 9f, 26f), skin);
            Shapes.Make(PrimitiveType.Cube, "Gondola", body, new Vector3(0f, -5f, 2f), new Vector3(2.5f, 1.6f, 5f), red, false);
            for (int i = 0; i < 4; i++)
            {
                var fin = Shapes.Make(PrimitiveType.Cube, "Fin", body, Vector3.zero, new Vector3(0.3f, 5f, 4f), red, false);
                fin.transform.localRotation = Quaternion.Euler(0f, 0f, 45f + i * 90f);
                fin.transform.localPosition = fin.transform.localRotation * new Vector3(0f, 4f, 0f) + new Vector3(0f, 0f, -11f);
            }
            var s1 = Sign(body, brand, new Vector3(-4.6f, 0.5f, 0f), 0.5f, new Color(0.85f, 0.2f, 0.2f));
            if (s1 != null) s1.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
            var s2 = Sign(body, brand, new Vector3(4.6f, 0.5f, 0f), 0.5f, new Color(0.85f, 0.2f, 0.2f));
            if (s2 != null) s2.transform.localEulerAngles = new Vector3(0f, -90f, 0f);
            orbit.gameObject.AddComponent<Spinner>().degreesPerSecond = 4f;
        }

        // ---------------------------------------------------------------- horizon

        public static void Mountain(Transform g, Vector3 pos, float height, float width, Material mat, int seed)
        {
            LowPoly.Make(LowPoly.Cone(7), "Mountain", g, pos + Vector3.up * height * 0.5f, new Vector3(width, height, width * 0.9f), mat,
                new Vector3(0f, seed * 37f, 0f));
            LowPoly.Make(LowPoly.Rock(seed % 3), "Foothill", g, pos + new Vector3(width * 0.35f, height * 0.12f, 0f),
                new Vector3(width * 0.5f, height * 0.35f, width * 0.5f), mat);
        }

        public static void Cloud(Transform g, Vector3 pos, float size, System.Random rng)
        {
            var white = M("Prop_Cloud", new Color(0.97f, 0.97f, 1f));
            int n = 3 + rng.Next(3);
            for (int i = 0; i < n; i++)
            {
                var off = new Vector3(((float)rng.NextDouble() - 0.5f) * size * 2f, ((float)rng.NextDouble() - 0.3f) * size * 0.4f,
                    ((float)rng.NextDouble() - 0.5f) * size);
                float s = size * (0.6f + (float)rng.NextDouble() * 0.6f);
                LowPoly.Make(LowPoly.Ico(1), "Puff", g, pos + off, new Vector3(s * 1.3f, s * 0.8f, s), white);
            }
        }

        /// <summary>Mountains, clouds, light towers and a blimp around an arena. center/radius in local space.</summary>
        public static void Backdrop(Transform g, Vector3 center, float radius, int seed, string blimpBrand)
        {
            var rng = new System.Random(seed);
            Material[] hills = { M("Prop_Hill1", new Color(0.42f, 0.28f, 0.2f)), M("Prop_Hill2", new Color(0.33f, 0.3f, 0.32f)),
                                 M("Prop_Hill3", new Color(0.5f, 0.35f, 0.22f)) };
            for (int i = 0; i < 16; i++)
            {
                float a = (i / 16f) * Mathf.PI * 2f + (float)rng.NextDouble() * 0.3f;
                float d = radius + 60f + (float)rng.NextDouble() * 80f;
                float h = 40f + (float)rng.NextDouble() * 60f;
                Mountain(g, center + new Vector3(Mathf.Sin(a) * d, -16f, Mathf.Cos(a) * d), h, h * 1.4f, hills[rng.Next(hills.Length)], i);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = radius * 0.5f + (float)rng.NextDouble() * (radius + 100f);
                Cloud(g, center + new Vector3(Mathf.Sin(a) * d, 70f + (float)rng.NextDouble() * 40f, Mathf.Cos(a) * d),
                    8f + (float)rng.NextDouble() * 8f, rng);
            }
            Blimp(g, center, radius + 40f, 55f, blimpBrand);
        }

        // ---------------------------------------------------------------- helpers

        public static GameObject Sign(Transform g, string text, Vector3 localPos, float size, Color color)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) return null;
            var go = new GameObject("Sign_" + text);
            go.transform.SetParent(g, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return go;
        }
    }
}
