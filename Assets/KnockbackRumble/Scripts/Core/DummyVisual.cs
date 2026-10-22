using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// The low-poly crash-test dummy, with procedural animation:
    /// leg/arm swing while running, a belly-flop pose while diving, and a full
    /// ragdoll-style tumble plus launch trail while stunned. Visual only; physics
    /// stays on the capsule.
    /// </summary>
    public class DummyVisual : MonoBehaviour
    {
        DummyMotor motor;
        Transform model, legL, legR, armL, armR, gun, hand;
        WeaponDef shownWeapon;
        TrailRenderer trail;
        float phase, tumbleAngle, shoveTimer;
        Vector3 tumbleAxis = Vector3.right;
        bool wasStunned;

        // Sounds and squash
        bool prevGrounded = true, prevDiving, whistled;
        float prevVy, airTime, squash;
        bool IsPlayer => contestant != null && contestant.IsPlayer;
        Contestant contestant;

        /// <summary>The bones outfits attach to.</summary>
        public DummyRig Rig { get; private set; }
        readonly System.Collections.Generic.List<GameObject> outfitParts = new System.Collections.Generic.List<GameObject>();
        public Outfit CurrentOutfit { get; private set; } = new Outfit();

        // Base-body parts that get hidden when an outfit item covers them
        readonly System.Collections.Generic.List<GameObject> hairParts = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<GameObject> beardParts = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<GameObject> vestParts = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<GameObject> pantsParts = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<GameObject> bootParts = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<GameObject> gloveParts = new System.Collections.Generic.List<GameObject>();

        static readonly Color[] SkinTones =
        {
            new Color(0.96f, 0.8f, 0.66f), new Color(0.9f, 0.7f, 0.54f), new Color(0.78f, 0.57f, 0.42f),
            new Color(0.6f, 0.42f, 0.3f), new Color(0.42f, 0.29f, 0.2f),
        };
        static readonly Color[] HairColors =
        {
            new Color(0.16f, 0.11f, 0.08f), new Color(0.35f, 0.22f, 0.12f), new Color(0.62f, 0.42f, 0.2f), new Color(0.1f, 0.09f, 0.09f),
            new Color(0.78f, 0.78f, 0.76f), // grey
        };
        static readonly Color CamoTan = new Color(0.62f, 0.55f, 0.42f), CamoOlive = new Color(0.42f, 0.45f, 0.3f),
            CamoBrown = new Color(0.38f, 0.3f, 0.22f), Olive = new Color(0.36f, 0.39f, 0.25f), DarkOlive = new Color(0.25f, 0.27f, 0.18f),
            BootBrown = new Color(0.26f, 0.19f, 0.13f), Sole = new Color(0.1f, 0.1f, 0.1f);

        Material[] camo;
        int camoSeed;

        /// <summary>
        /// Builds the chunky low-poly soldier: faceted camo pants, a plate carrier vest in the player's
        /// color, big bare arms with fingerless gloves, heavy boots and a small grumpy head with a random
        /// haircut and beard (seeded from the color, so everyone sees the same guy). Bones (pivots) are the
        /// ones the animation drives: model, armL/R (shoulders), legL/R (hips).
        /// </summary>
        /// <summary>Which character this dummy is (-1 = a random look seeded from its color).</summary>
        public int Character { get; private set; } = -1;
        Color builtColor;

        public void Build(Color bodyColor, int character = -1)
        {
            motor = GetComponent<DummyMotor>();
            builtColor = bodyColor;
            Character = Characters.Get(character) != null ? character : -1;
            BuildBody(bodyColor, Characters.Get(Character));
            if (trail == null) MakeTrail(bodyColor);
        }

        /// <summary>Switch to another character (rebuilds the body; the weapon in hand stays).</summary>
        public void SetCharacter(int character)
        {
            if (Characters.Get(character) == null || character == Character || model == null) return;
            var held = shownWeapon;
            model.gameObject.SetActive(false);
            Destroy(model.gameObject);
            model = null;
            gun = null;
            shownWeapon = null;
            hairParts.Clear(); beardParts.Clear(); vestParts.Clear(); pantsParts.Clear(); bootParts.Clear(); gloveParts.Clear();
            outfitParts.Clear();
            CurrentOutfit = new Outfit();
            Character = character;
            BuildBody(builtColor, Characters.Get(character));
            if (held != null) ShowWeapon(held);
        }

        void BuildBody(Color bodyColor, CharacterDef def)
        {
            string hex = ColorUtility.ToHtmlStringRGB(bodyColor);
            int hash = (Mathf.RoundToInt(bodyColor.r * 255f) * 73856093 ^ Mathf.RoundToInt(bodyColor.g * 255f) * 19349663
                        ^ Mathf.RoundToInt(bodyColor.b * 255f) * 83492791) & 0x7fffffff;
            var rng = new System.Random(hash);
            camoSeed = hash % 5;

            Color vestColor = Color.Lerp(bodyColor, Olive, 0.35f);
            var vest = Mats.Get("Soldier_Vest_" + hex, vestColor);
            var vestDark = Mats.Get("Soldier_VestDark_" + hex, vestColor * 0.75f);
            var band = Mats.Get("Soldier_Band_" + hex, bodyColor);
            int skinIndex = rng.Next(SkinTones.Length), hairIndex = rng.Next(4);
            if (def != null)
            {
                skinIndex = Mathf.Clamp(def.skin, 0, SkinTones.Length - 1);
                hairIndex = Mathf.Clamp(def.hairColor, 0, HairColors.Length - 1);
            }
            var skin = Mats.Get("Soldier_Skin" + skinIndex, SkinTones[skinIndex]);
            var skinDark = Mats.Get("Soldier_SkinDark" + skinIndex, SkinTones[skinIndex] * 0.85f);
            var hair = Mats.Get("Soldier_Hair" + hairIndex, HairColors[hairIndex]);
            var shirt = Mats.Get("Soldier_Shirt", Olive);
            var dark = Mats.Get("Soldier_DarkOlive", DarkOlive);
            var boot = Mats.Get("Soldier_Boot", BootBrown);
            var sole = Mats.Get("Soldier_Sole", Sole);
            var black = Mats.Get("Dummy_Black", Palette.Black);
            camo = new[] { Mats.Get("Soldier_CamoTan", CamoTan), Mats.Get("Soldier_CamoOlive", CamoOlive), Mats.Get("Soldier_CamoBrown", CamoBrown) };

            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            model.localPosition = new Vector3(0f, 0.9f, 0f); // pivot at the body's center so tumbles look right

            // Torso: olive tee under the vest, camo hips, a pouch belt
            LowPoly.Make(LowPoly.Chunk(0), "Chest", model, new Vector3(0f, DummyRig.ChestY, 0f), new Vector3(0.74f, 0.62f, 0.46f), shirt);
            LowPoly.Make(LowPoly.Chunk(1), "Belly", model, new Vector3(0f, 0.07f, 0f), new Vector3(0.6f, 0.38f, 0.4f), shirt);
            pantsParts.Add(Camo(model, "Hips", 2, new Vector3(0f, -0.1f, 0f), new Vector3(0.54f, 0.28f, 0.36f)));
            Shapes.Make(PrimitiveType.Cube, "Belt", model, new Vector3(0f, -0.01f, 0f), new Vector3(0.56f, 0.07f, 0.38f), dark, false);
            Shapes.Make(PrimitiveType.Cube, "Buckle", model, new Vector3(0f, -0.01f, 0.195f), new Vector3(0.08f, 0.06f, 0.02f), black, false);
            foreach (float side in new[] { -1f, 1f })
            {
                Shapes.Make(PrimitiveType.Cube, "BeltPouch", model, new Vector3(side * 0.2f, -0.03f, 0.17f), new Vector3(0.1f, 0.11f, 0.08f), dark, false);
                Shapes.Make(PrimitiveType.Cube, "HipPouch", model, new Vector3(side * 0.29f, -0.04f, 0f), new Vector3(0.07f, 0.12f, 0.14f), dark, false);
            }

            // Plate carrier in the player's color: wrap, front and back plates, magazine pouches, straps
            vestParts.Add(LowPoly.Make(LowPoly.Chunk(2), "Vest", model, new Vector3(0f, 0.25f, 0f), new Vector3(0.78f, 0.46f, 0.52f), vest));
            vestParts.Add(Shapes.Make(PrimitiveType.Cube, "FrontPlate", model, new Vector3(0f, 0.28f, 0.235f), new Vector3(0.5f, 0.38f, 0.07f), vest, false));
            vestParts.Add(Shapes.Make(PrimitiveType.Cube, "BackPlate", model, new Vector3(0f, 0.28f, -0.235f), new Vector3(0.5f, 0.4f, 0.07f), vest, false));
            for (int i = -1; i <= 1; i++)
                vestParts.Add(Shapes.Make(PrimitiveType.Cube, "MagPouch", model, new Vector3(i * 0.15f, 0.16f, 0.285f), new Vector3(0.12f, 0.14f, 0.06f), vestDark, false));
            vestParts.Add(Shapes.Make(PrimitiveType.Cube, "Radio", model, new Vector3(0.13f, 0.38f, 0.28f), new Vector3(0.07f, 0.12f, 0.05f), black, false));
            vestParts.Add(Shapes.Make(PrimitiveType.Cube, "Antenna", model, new Vector3(0.15f, 0.5f, 0.28f), new Vector3(0.012f, 0.16f, 0.012f), black, false));
            foreach (float side in new[] { -1f, 1f })
                vestParts.Add(Shapes.Make(PrimitiveType.Cube, "Strap", model, new Vector3(side * 0.17f, 0.5f, 0f), new Vector3(0.1f, 0.06f, 0.5f), vestDark, false));

            // Thick neck and a small, grumpy head
            LowPoly.Make(LowPoly.Cylinder(8), "Neck", model, new Vector3(0f, 0.56f, 0f), new Vector3(0.21f, 0.12f, 0.21f), skin);
            var head = new GameObject("Head").transform;
            head.SetParent(model, false);
            head.localPosition = new Vector3(0f, DummyRig.HeadY, 0f);
            LowPoly.Make(LowPoly.Chunk(3), "Skull", head, Vector3.zero, new Vector3(0.374f, 0.408f, 0.374f), skin);
            LowPoly.Make(LowPoly.Chunk(4), "Jaw", head, new Vector3(0f, -0.09f, 0.02f), new Vector3(0.33f, 0.22f, 0.33f), skin);
            LowPoly.Make(LowPoly.Chunk(1), "Nose", head, new Vector3(0f, -0.01f, 0.19f), new Vector3(0.07f, 0.08f, 0.07f), skinDark);
            foreach (float side in new[] { -1f, 1f })
            {
                LowPoly.Make(LowPoly.Ico(0), "Ear", head, new Vector3(side * 0.185f, 0f, 0f), new Vector3(0.05f, 0.08f, 0.06f), skinDark);
                LowPoly.Make(LowPoly.Ico(0), "Eye", head, new Vector3(side * 0.068f, 0.035f, 0.178f), new Vector3(0.045f, 0.035f, 0.02f), black);
                // Angry brows: the inner ends drop
                beardParts.Add(Shapes.Make(PrimitiveType.Cube, "Brow", head, new Vector3(side * 0.07f, 0.075f, 0.178f), new Vector3(0.1f, 0.03f, 0.04f), hair, false,
                    new Vector3(0f, 0f, side * 16f)));
            }
            Shapes.Make(PrimitiveType.Cube, "Mouth", head, new Vector3(0f, -0.09f, 0.185f), new Vector3(0.08f, 0.015f, 0.02f), black, false);

            int beardStyle = rng.Next(4), hairStyle = rng.Next(4);
            if (def != null) { beardStyle = def.beard; hairStyle = def.hairStyle; }
            switch (beardStyle) // beard
            {
                case 0: // full beard
                    beardParts.Add(LowPoly.Make(LowPoly.Chunk(2), "Beard", head, new Vector3(0f, -0.13f, 0.06f), new Vector3(0.32f, 0.2f, 0.28f), hair));
                    beardParts.Add(Shapes.Make(PrimitiveType.Cube, "Stache", head, new Vector3(0f, -0.055f, 0.19f), new Vector3(0.14f, 0.035f, 0.04f), hair, false));
                    break;
                case 1: // horseshoe mustache
                    beardParts.Add(Shapes.Make(PrimitiveType.Cube, "Stache", head, new Vector3(0f, -0.055f, 0.19f), new Vector3(0.15f, 0.035f, 0.04f), hair, false));
                    foreach (float side in new[] { -1f, 1f })
                        beardParts.Add(Shapes.Make(PrimitiveType.Cube, "StacheSide", head, new Vector3(side * 0.07f, -0.11f, 0.18f), new Vector3(0.03f, 0.09f, 0.035f), hair, false));
                    break;
                case 2: // stubble
                    beardParts.Add(LowPoly.Make(LowPoly.Chunk(4), "Stubble", head, new Vector3(0f, -0.1f, 0.025f), new Vector3(0.335f, 0.2f, 0.335f), skinDark));
                    break;
                // case 3: clean shaven
            }

            switch (hairStyle) // hair
            {
                case 0: // buzz cut
                    hairParts.Add(LowPoly.Make(LowPoly.Chunk(0), "Buzz", head, new Vector3(0f, 0.07f, -0.015f), new Vector3(0.39f, 0.3f, 0.39f), hair));
                    break;
                case 1: // mohawk
                    for (int i = 0; i < 4; i++)
                        hairParts.Add(Shapes.Make(PrimitiveType.Cube, "Mohawk", head, new Vector3(0f, 0.2f - i * 0.02f, 0.1f - i * 0.075f), new Vector3(0.06f, 0.1f, 0.08f), hair, false,
                            new Vector3(-20f + i * 15f, 0f, 0f)));
                    break;
                case 2: // patrol cap
                    hairParts.Add(LowPoly.Make(LowPoly.Cylinder(10), "Cap", head, new Vector3(0f, 0.13f, -0.01f), new Vector3(0.38f, 0.13f, 0.38f), camo[1]));
                    hairParts.Add(Shapes.Make(PrimitiveType.Cube, "Brim", head, new Vector3(0f, 0.08f, 0.2f), new Vector3(0.3f, 0.025f, 0.13f), dark, false,
                        new Vector3(-10f, 0f, 0f)));
                    break;
                // case 3: bald
            }

            // Arms (shoulder pivots) and legs (hip pivots)
            armL = Arm("ArmL", -1f, skin, shirt, band, dark);
            armR = Arm("ArmR", 1f, skin, shirt, band, dark);
            legL = Leg("LegL", -1f, dark, boot, sole);
            legR = Leg("LegR", 1f, dark, boot, sole);

            // Outfit items were made for the slimmer dummy, so they hang off scaled "item space" transforms
            Rig = new DummyRig
            {
                model = ItemSpace(model, new Vector3(1.2f, 1.12f, 1.2f)),
                head = ItemSpace(head, Vector3.one * 0.85f),
                armL = ItemSpace(armL, new Vector3(1.45f, 1.1f, 1.45f)),
                armR = ItemSpace(armR, new Vector3(1.45f, 1.1f, 1.45f)),
                legL = ItemSpace(legL, new Vector3(1.3f, 1f, 1.3f)),
                legR = ItemSpace(legR, new Vector3(1.3f, 1f, 1.3f)),
                body = vest,
            };

            // The hand: weapon models are built pointing along +Z, so this turns them to point down the arm.
            hand = new GameObject("Hand").transform;
            hand.SetParent(armR, false);
            hand.localPosition = new Vector3(0f, DummyRig.BodyHandY, 0f);
            hand.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        void MakeTrail(Color bodyColor)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.45f;
            trail.startWidth = 0.6f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.2f;
            trail.sharedMaterial = Mats.Unlit("Trail", Color.white);
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(bodyColor, 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;
            trail.emitting = false;
        }

        static Transform ItemSpace(Transform bone, Vector3 scale)
        {
            var t = new GameObject("Items").transform;
            t.SetParent(bone, false);
            t.localScale = scale;
            return t;
        }

        /// <summary>A chunky part in faceted camo.</summary>
        GameObject Camo(Transform parent, string name, int variant, Vector3 pos, Vector3 scale)
        {
            var mesh = LowPoly.Camo(LowPoly.Chunk(variant), "Chunk" + variant, camo.Length, camoSeed + variant, 3f);
            return LowPoly.MakeMulti(mesh, name, parent, pos, scale, camo);
        }

        Transform Arm(string name, float side, Material skin, Material shirt, Material band, Material glove)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(model, false);
            pivot.localPosition = new Vector3(side * 0.47f, 0.42f, 0f);
            LowPoly.Make(LowPoly.Chunk(1), "Sleeve", pivot, new Vector3(0f, -0.03f, 0f), new Vector3(0.33f, 0.3f, 0.32f), shirt);
            LowPoly.Make(LowPoly.Torus(10, 4, 0.3f), "Armband", pivot, new Vector3(0f, -0.13f, 0f), new Vector3(0.25f, 0.07f, 0.25f), band);
            LowPoly.Make(LowPoly.Chunk(2), "UpperArm", pivot, new Vector3(0f, -0.21f, 0f), new Vector3(0.22f, 0.3f, 0.22f), skin);
            LowPoly.Make(LowPoly.Chunk(3), "Forearm", pivot, new Vector3(0f, -0.44f, 0f), new Vector3(0.22f, 0.3f, 0.22f), skin);
            gloveParts.Add(LowPoly.Make(LowPoly.Chunk(4), "Glove", pivot, new Vector3(0f, -0.64f, 0f), new Vector3(0.21f, 0.2f, 0.2f), glove));
            gloveParts.Add(LowPoly.Make(LowPoly.Cylinder(8), "GloveCuff", pivot, new Vector3(0f, -0.56f, 0f), new Vector3(0.2f, 0.05f, 0.2f), glove));
            return pivot;
        }

        Transform Leg(string name, float side, Material dark, Material boot, Material sole)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(model, false);
            pivot.localPosition = new Vector3(side * 0.15f, -0.18f, 0f);
            pantsParts.Add(Camo(pivot, "Thigh", 0, new Vector3(0f, -0.2f, 0f), new Vector3(0.26f, 0.36f, 0.27f)));
            pantsParts.Add(Camo(pivot, "CargoPocket", 3, new Vector3(side * 0.12f, -0.25f, 0.02f), new Vector3(0.07f, 0.14f, 0.13f)));
            pantsParts.Add(LowPoly.Make(LowPoly.Chunk(1), "KneePad", pivot, new Vector3(0f, -0.38f, 0.1f), new Vector3(0.16f, 0.13f, 0.09f), dark));
            pantsParts.Add(Camo(pivot, "Shin", 1, new Vector3(0f, -0.48f, 0f), new Vector3(0.23f, 0.3f, 0.24f)));
            bootParts.Add(LowPoly.Make(LowPoly.Cylinder(8), "BootTop", pivot, new Vector3(0f, -0.58f, 0f), new Vector3(0.27f, 0.12f, 0.27f), boot));
            bootParts.Add(Shapes.Make(PrimitiveType.Cube, "Boot", pivot, new Vector3(0f, -0.64f, 0.06f), new Vector3(0.27f, 0.16f, 0.4f), boot, false));
            bootParts.Add(Shapes.Make(PrimitiveType.Cube, "Sole", pivot, new Vector3(0f, -0.725f, 0.06f), new Vector3(0.29f, 0.05f, 0.42f), sole, false));
            return pivot;
        }

        /// <summary>Dress the dummy (hat, mask, shirt, pants, shoes, gloves). Replaces the previous outfit.</summary>
        public void ApplyOutfit(Outfit outfit)
        {
            foreach (var go in outfitParts) if (go != null) Destroy(go);
            outfitParts.Clear();
            CurrentOutfit = outfit != null ? outfit.Copy() : new Outfit();
            if (Rig == null) return;
            for (int i = 0; i < Outfit.SlotCount; i++)
            {
                var item = Cosmetics.Find((CosmeticSlot)i, CurrentOutfit.ids[i]);
                if (item != null && item.build != null) item.build(new CosmeticBuilder(Rig, outfitParts));
            }
            var mask = Cosmetics.Find(CosmeticSlot.Mask, CurrentOutfit[CosmeticSlot.Mask]);
            bool faceCovered = mask != null && mask.name != "Shades" && mask.name != "Clown Nose";
            SetShown(hairParts, CurrentOutfit[CosmeticSlot.Hat] == 0);
            SetShown(beardParts, !faceCovered);
            SetShown(vestParts, CurrentOutfit[CosmeticSlot.Shirt] == 0);
            SetShown(pantsParts, CurrentOutfit[CosmeticSlot.Pants] == 0);
            SetShown(bootParts, CurrentOutfit[CosmeticSlot.Shoes] == 0);
            SetShown(gloveParts, CurrentOutfit[CosmeticSlot.Gloves] == 0);
        }

        static void SetShown(System.Collections.Generic.List<GameObject> parts, bool shown)
        {
            foreach (var go in parts) if (go != null) go.SetActive(shown);
        }

        public void ShowWeapon(WeaponDef def)
        {
            if (hand == null || def == shownWeapon) return;
            shownWeapon = def;
            if (gun != null) Destroy(gun.gameObject);
            gun = def != null ? WeaponModels.Build(def, hand).transform : null;
        }

        /// <summary>Where the barrel ends, for muzzle flashes (falls back to the hand).</summary>
        public Vector3 MuzzlePoint
        {
            get
            {
                if (hand == null) return transform.position + Vector3.up * 1.35f + transform.forward * 0.6f;
                float len = shownWeapon == WeaponDef.LongShot ? 1.15f : shownWeapon == WeaponDef.RocketLauncher ? 0.8f
                          : shownWeapon == WeaponDef.BoomStick ? 0.74f : 0.45f;
                float up = shownWeapon == WeaponDef.RocketLauncher ? 0.12f : 0.05f;
                return hand.TransformPoint(new Vector3(0f, up, len));
            }
        }

        public void PlayShove() => shoveTimer = 0.2f;

        void LateUpdate()
        {
            if (model == null || motor == null) return;
            float dt = Time.deltaTime;
            Vector3 v = motor.Velocity;
            float speed = new Vector3(v.x, 0f, v.z).magnitude;
            MovementFeel(v, dt);

            if (motor.Stunned)
            {
                if (!wasStunned) tumbleAxis = Random.onUnitSphere;
                tumbleAngle += Mathf.Lerp(400f, 900f, Mathf.Clamp01(v.magnitude / 20f)) * dt;
                model.localRotation = Quaternion.AngleAxis(tumbleAngle, tumbleAxis);
                SetLimbs(70f, -70f, 150f, -150f, 30f);
                trail.emitting = v.magnitude > 6f;
            }
            else
            {
                trail.emitting = false;
                tumbleAngle = 0f;
                Quaternion pose = motor.Diving ? Quaternion.Euler(80f, 0f, 0f) : Quaternion.identity;
                model.localRotation = Quaternion.Slerp(model.localRotation, pose, 14f * dt);

                if (motor.Diving)
                {
                    SetLimbs(10f, 10f, -160f, -160f, 0f);
                }
                else if (motor.Grounded)
                {
                    phase += speed * dt * 1.7f;
                    float swing = Mathf.Sin(phase) * Mathf.Clamp01(speed / 6f) * 45f;
                    SetLimbs(swing, -swing, -swing * 0.8f, swing * 0.8f, 0f);
                }
                else
                {
                    SetLimbs(-30f, 20f, -120f, -120f, 0f); // flailing jump pose
                }
            }

            // Holding a gun points the right arm forward; a shove punches both arms out.
            if (shoveTimer > 0f)
            {
                shoveTimer -= dt;
                armL.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                armR.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }
            else if (gun != null && gun.gameObject.activeSelf && !motor.Stunned)
            {
                armR.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            }

            wasStunned = motor.Stunned;
        }

        void MovementFeel(Vector3 v, float dt)
        {
            if (contestant == null) contestant = GetComponent<Contestant>();
            bool grounded = motor.Grounded;
            Vector3 pos = transform.position;
            float loud = IsPlayer ? 1f : 0.7f;

            if (prevGrounded && !grounded && v.y > 4f)
                Sfx.At("jump", pos, loud, 0.08f, 22f);
            else if (!grounded && !prevGrounded && !motor.Stunned && v.y - prevVy > 5f && v.y < 14f)
                Sfx.At("double_jump", pos, loud, 0.08f, 22f);

            if (!grounded) airTime += dt;
            if (grounded && !prevGrounded)
            {
                if (airTime > 0.3f)
                {
                    float impact = Mathf.Clamp01(-prevVy / 20f);
                    Sfx.At("land", pos, loud * (0.5f + impact), 0.1f, 22f);
                    squash = Mathf.Max(squash, 0.35f + impact * 0.4f);
                    if (IsPlayer && impact > 0.6f) ThirdPersonCamera.Shake(0.15f);
                }
                airTime = 0f;
            }

            if (motor.Diving && !prevDiving) Sfx.At("dive", pos, loud, 0.08f, 22f);

            // Falling off the map: slide whistle, once per fall.
            if (!grounded && v.y < -8f && pos.y < -2f && !whistled)
            {
                whistled = true;
                Sfx.At("fall_whistle", pos, IsPlayer ? 1.2f : 0.9f, 0.06f, 60f);
            }
            if (grounded || pos.y > 0f) whistled = false;

            // Squash on landing, stretch while flying up fast.
            squash = Mathf.MoveTowards(squash, 0f, dt * 3f);
            float stretch = !grounded && !motor.Stunned ? Mathf.Clamp01((v.y - 6f) / 12f) * 0.15f : 0f;
            float s = squash;
            model.localScale = new Vector3(1f + s * 0.35f - stretch * 0.5f, 1f - s * 0.4f + stretch, 1f + s * 0.35f - stretch * 0.5f);

            prevGrounded = grounded;
            prevDiving = motor.Diving;
            prevVy = v.y;
        }

        void SetLimbs(float legLx, float legRx, float armLx, float armRx, float spread)
        {
            legL.localRotation = Quaternion.Euler(legLx, 0f, -spread);
            legR.localRotation = Quaternion.Euler(legRx, 0f, spread);
            armL.localRotation = Quaternion.Euler(armLx, 0f, -spread);
            armR.localRotation = Quaternion.Euler(armRx, 0f, spread);
        }
    }
}
