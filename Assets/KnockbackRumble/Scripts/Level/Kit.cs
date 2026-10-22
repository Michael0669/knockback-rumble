using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// The building kit for course pieces, so nothing looks like a plain grey box:
    /// steel decks with panel seams, painted hazard edges and girders underneath,
    /// I-beams, crane gantries, safety railings, shipping containers, truss arches.
    /// Positions are local to the group you pass in.
    /// </summary>
    public static class Kit
    {
        static Material M(string name, Color c) => Mats.Get(name, c);
        public static Material Yellow => M("Stripe", Palette.Stripe);
        public static Material Black => M("Dummy_Black", Palette.Black);
        public static Material Seam => M("Kit_Seam", new Color(0.18f, 0.18f, 0.2f));
        public static Material Girder => M("Prop_DarkSteel", new Color(0.3f, 0.31f, 0.34f));
        public static Material SafetyYellow => M("Kit_SafetyYellow", new Color(1f, 0.72f, 0.1f));
        public static Material Steel => M("Gun_Steel", new Color(0.55f, 0.57f, 0.62f));

        public static GameObject Box(Transform g, string name, Vector3 pos, Vector3 size, Material mat, bool collider = false, Vector3? euler = null) =>
            Shapes.Make(PrimitiveType.Cube, name, g, pos, size, mat, collider, euler);

        /// <summary>
        /// A walkable steel deck: a 1 m slab whose top is at "top", with panel seams, painted
        /// yellow/black edges (flush with the surface) and two girders underneath.
        /// </summary>
        public static GameObject Deck(Transform g, string name, float x, float z0, float z1, float width, float top, Material mat,
            bool trim = true, bool girders = true)
        {
            float len = z1 - z0, zc = (z0 + z1) * 0.5f;
            var slab = Box(g, name, new Vector3(x, top - 0.5f, zc), new Vector3(width, 1f, len), mat, true);

            // Panel seams across the deck
            for (float z = z0 + 3f; z < z1 - 0.5f; z += 3f)
                Box(g, name + "_Seam", new Vector3(x, top + 0.004f, z), new Vector3(width - 0.1f, 0.01f, 0.07f), Seam);

            if (trim)
            {
                // Painted hazard edges: a yellow band with black dashes, on the surface (nothing sticks out)
                foreach (float side in new[] { -1f, 1f })
                {
                    float ex = x + side * (width * 0.5f - 0.22f);
                    Box(g, name + "_Edge", new Vector3(ex, top + 0.006f, zc), new Vector3(0.36f, 0.01f, len - 0.04f), Yellow);
                    for (float z = z0 + 0.6f; z < z1 - 0.6f; z += 1.6f)
                        Box(g, name + "_Dash", new Vector3(ex, top + 0.009f, z + 0.4f), new Vector3(0.37f, 0.01f, 0.8f), Black);
                }
                // Slab sides painted dark so the edge reads from below too
                Box(g, name + "_Lip", new Vector3(x, top - 0.1f, zc), new Vector3(width + 0.02f, 0.14f, len + 0.02f), Seam);
            }

            if (girders && len > 2f)
            {
                foreach (float side in new[] { -0.3f, 0.3f })
                    IBeam(g, new Vector3(x + side * width, top - 1.4f, z0 + 0.3f), new Vector3(x + side * width, top - 1.4f, z1 - 0.3f), 0.8f, Girder);
            }
            return slab;
        }

        /// <summary>An I-beam from a to b (h = beam height).</summary>
        public static Transform IBeam(Transform g, Vector3 a, Vector3 b, float h, Material mat)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            var t = new GameObject("IBeam").transform;
            t.SetParent(g, false);
            t.localPosition = (a + b) * 0.5f;
            Vector3 up = Mathf.Abs(d.normalized.y) > 0.9f ? Vector3.forward : Vector3.up;
            if (len > 0.001f) t.localRotation = Quaternion.LookRotation(d / len, up);
            Box(t, "Web", Vector3.zero, new Vector3(h * 0.14f, h, len), mat);
            Box(t, "FlangeTop", new Vector3(0f, h * 0.45f, 0f), new Vector3(h * 0.75f, h * 0.12f, len), mat);
            Box(t, "FlangeBottom", new Vector3(0f, -h * 0.45f, 0f), new Vector3(h * 0.75f, h * 0.12f, len), mat);
            return t;
        }

        /// <summary>
        /// A yellow crane gantry over the course: two I-beam legs from the pit up to a cross
        /// beam with knee braces, hazard stripes, warning lamps and a pulley block.
        /// Returns the pulley point (local), where a pendulum should hang from.
        /// </summary>
        public static Vector3 Gantry(Transform g, Vector3 center, float halfWidth, float top, float bottom = -14f)
        {
            var yellow = SafetyYellow;
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 foot = center + new Vector3(side * halfWidth, bottom, 0f);
                Vector3 head = center + new Vector3(side * halfWidth, top, 0f);
                IBeam(g, foot, head, 0.9f, yellow);
                // Knee brace
                IBeam(g, center + new Vector3(side * halfWidth, top - 2.5f, 0f), center + new Vector3(side * (halfWidth - 2.5f), top, 0f), 0.5f, yellow);
                // Stripes around the leg at head height
                for (int k = 0; k < 4; k++)
                    Box(g, "LegStripe", center + new Vector3(side * halfWidth, top - 4f - k * 0.5f, 0f), new Vector3(0.95f, 0.25f, 0.95f),
                        k % 2 == 0 ? Black : yellow);
                // Warning lamp on top
                LowPoly.Make(LowPoly.Ico(1), "Lamp", g, head + Vector3.up * 0.9f, Vector3.one * 0.45f, Mats.Unlit("Kit_Warn", new Color(1f, 0.45f, 0.1f)));
            }
            IBeam(g, center + new Vector3(-halfWidth - 0.5f, top + 0.45f, 0f), center + new Vector3(halfWidth + 0.5f, top + 0.45f, 0f), 0.9f, yellow);
            for (int k = -3; k <= 3; k++)
                Box(g, "BeamStripe", center + new Vector3(k * 1.6f, top + 0.45f, 0f), new Vector3(0.8f, 0.92f, 0.82f), Black);
            // Pulley block
            Box(g, "Block", center + new Vector3(0f, top - 0.35f, 0f), new Vector3(0.9f, 0.7f, 0.9f), Girder);
            LowPoly.Make(LowPoly.Cylinder(12), "Pulley", g, center + new Vector3(0f, top - 0.35f, 0f), new Vector3(1.1f, 1f, 1.1f), Steel,
                new Vector3(0f, 0f, 90f));
            return center + new Vector3(0f, top - 0.6f, 0f);
        }

        /// <summary>A safety railing along Z at height y, with an invisible collider so it still blocks.</summary>
        public static void Railing(Transform g, float x, float z0, float z1, float floorTop, bool collider = true)
        {
            float len = z1 - z0, zc = (z0 + z1) * 0.5f;
            if (collider)
            {
                var c = Box(g, "RailCollider", new Vector3(x, floorTop + 0.55f, zc), new Vector3(0.3f, 1.1f, len), Seam, true);
                c.GetComponent<Renderer>().enabled = false;
            }
            for (float z = z0 + 0.3f; z <= z1 - 0.3f + 0.01f; z += 2f)
                LowPoly.Make(LowPoly.Cylinder(8), "Post", g, new Vector3(x, floorTop + 0.55f, z), new Vector3(0.12f, 1.1f, 0.12f), SafetyYellow);
            LowPoly.Make(LowPoly.Cylinder(8), "TopRail", g, new Vector3(x, floorTop + 1.1f, zc), new Vector3(0.14f, len, 0.14f), SafetyYellow,
                new Vector3(90f, 0f, 0f));
            LowPoly.Make(LowPoly.Cylinder(8), "MidRail", g, new Vector3(x, floorTop + 0.6f, zc), new Vector3(0.09f, len, 0.09f), Steel,
                new Vector3(90f, 0f, 0f));
        }

        /// <summary>A safety railing from a to b (floor-level points), any direction.</summary>
        public static void RailingAlong(Transform g, Vector3 a, Vector3 b, bool collider = true)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.1f) return;
            var t = new GameObject("Railing").transform;
            t.SetParent(g, false);
            t.localPosition = (a + b) * 0.5f;
            t.localRotation = Quaternion.LookRotation(new Vector3(d.x, 0f, d.z).normalized, Vector3.up);
            Railing(t, 0f, -len * 0.5f, len * 0.5f, 0f, collider);
        }

        /// <summary>A shipping container (2.5 x 2.6 x length), corrugated sides, doors at one end.</summary>
        public static void Container(Transform g, Vector3 basePos, float yaw, Color color, float length = 6f, string label = null)
        {
            var t = new GameObject("Container").transform;
            t.SetParent(g, false);
            t.localPosition = basePos;
            t.localEulerAngles = new Vector3(0f, yaw, 0f);
            // The pack's shipping containers (red or green), stretched to the same size.
            if (Art.PropBox(color.r >= color.g ? "Environment/Container_Red" : "Environment/Container_Green", t, Vector3.zero, 0f,
                    new Vector3(2.5f, 2.6f, length)) != null)
            {
                if (!string.IsNullOrEmpty(label))
                {
                    var sign = Props.Sign(t, label, new Vector3(-1.32f, 1.6f, 0f), 0.16f, Color.white);
                    if (sign != null) sign.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
                }
                return;
            }
            var paint = M("Kit_Container_" + ColorUtility.ToHtmlStringRGB(color), color);
            var dark = M("Kit_Container_Dark_" + ColorUtility.ToHtmlStringRGB(color), color * 0.7f);
            Box(t, "Box", new Vector3(0f, 1.3f, 0f), new Vector3(2.5f, 2.6f, length), paint);
            for (float z = -length * 0.5f + 0.3f; z < length * 0.5f - 0.2f; z += 0.6f)
            {
                Box(t, "RibL", new Vector3(-1.27f, 1.3f, z), new Vector3(0.05f, 2.4f, 0.22f), dark);
                Box(t, "RibR", new Vector3(1.27f, 1.3f, z), new Vector3(0.05f, 2.4f, 0.22f), dark);
            }
            Box(t, "Frame", new Vector3(0f, 2.62f, 0f), new Vector3(2.56f, 0.08f, length + 0.04f), dark);
            Box(t, "DoorL", new Vector3(-0.62f, 1.3f, length * 0.5f + 0.02f), new Vector3(1.18f, 2.4f, 0.05f), dark);
            Box(t, "DoorR", new Vector3(0.62f, 1.3f, length * 0.5f + 0.02f), new Vector3(1.18f, 2.4f, 0.05f), dark);
            if (!string.IsNullOrEmpty(label))
            {
                var s = Props.Sign(t, label, new Vector3(-1.32f, 1.6f, 0f), 0.16f, Color.white);
                if (s != null) s.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
            }
        }

        /// <summary>A gold-and-white truss arch across the course with marquee bulbs.</summary>
        public static void Arch(Transform g, Vector3 center, float width, float height, Material frame, Color bulbColor)
        {
            var bulb = Mats.Unlit("Kit_Bulb_" + ColorUtility.ToHtmlStringRGB(bulbColor), bulbColor);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 foot = center + new Vector3(side * width * 0.5f, 0f, 0f);
                LowPoly.Make(LowPoly.Cylinder(10), "Pillar", g, foot + Vector3.up * height * 0.5f, new Vector3(0.7f, height, 0.7f), frame);
                LowPoly.Make(LowPoly.Cylinder(10), "Base", g, foot + Vector3.up * 0.25f, new Vector3(1.1f, 0.5f, 1.1f), Girder);
                for (float y = 1f; y < height; y += 0.8f)
                    LowPoly.Make(LowPoly.Ico(0), "Bulb", g, foot + new Vector3(0f, y, -0.38f), Vector3.one * 0.2f, bulb);
            }
            Box(g, "Header", center + new Vector3(0f, height + 0.6f, 0f), new Vector3(width + 1.2f, 1.6f, 0.8f), frame);
            Box(g, "HeaderFace", center + new Vector3(0f, height + 0.6f, -0.42f), new Vector3(width + 0.6f, 1.2f, 0.02f), Seam);
            for (float x = -width * 0.5f; x <= width * 0.5f + 0.01f; x += 0.8f)
            {
                LowPoly.Make(LowPoly.Ico(0), "Bulb", g, center + new Vector3(x, height - 0.25f, -0.4f), Vector3.one * 0.2f, bulb);
                LowPoly.Make(LowPoly.Ico(0), "Bulb", g, center + new Vector3(x, height + 1.45f, -0.4f), Vector3.one * 0.2f, bulb);
            }
        }
    }
}
