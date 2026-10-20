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

        /// <summary>
        /// Builds the crash-test dummy: rounded low-poly body, googly eyes, crash-test target decals,
        /// black joint rings, two-part arms and legs with hands and feet. Bones (pivots) are the same
        /// ones the animation drives: model, armL/R (shoulders), legL/R (hips).
        /// </summary>
        public void Build(Color bodyColor)
        {
            motor = GetComponent<DummyMotor>();
            var body = Mats.Get("Dummy_" + ColorUtility.ToHtmlStringRGB(bodyColor), bodyColor);
            var shade = Mats.Get("Dummy_Shade_" + ColorUtility.ToHtmlStringRGB(bodyColor), bodyColor * 0.72f);
            var yellow = Mats.Get("Dummy_Yellow", Palette.DummyYellow);
            var black = Mats.Get("Dummy_Black", Palette.Black);
            var eyeWhite = Mats.Get("Dummy_EyeWhite", new Color(0.97f, 0.97f, 0.95f));

            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            model.localPosition = new Vector3(0f, 0.9f, 0f); // pivot at the body's center so tumbles look right

            // Torso: chest, belly, pelvis
            LowPoly.Make(LowPoly.Ico(1), "Chest", model, new Vector3(0f, DummyRig.ChestY, 0f), new Vector3(0.6f, 0.55f, 0.4f), body);
            LowPoly.Make(LowPoly.Ico(1), "Belly", model, new Vector3(0f, DummyRig.BellyY, 0f), new Vector3(0.5f, 0.35f, 0.34f), body);
            Shapes.Make(PrimitiveType.Cube, "Pelvis", model, new Vector3(0f, DummyRig.PelvisY, 0f), new Vector3(0.42f, 0.2f, 0.26f), shade, false);
            LowPoly.Make(LowPoly.Torus(12, 4, 0.2f), "Waist", model, new Vector3(0f, -0.02f, 0f), new Vector3(0.48f, 0.12f, 0.34f), black);
            // Chest target decal
            LowPoly.Make(LowPoly.Cylinder(12), "TargetChest", model, new Vector3(0f, 0.32f, 0.205f), new Vector3(0.2f, 0.01f, 0.2f), yellow, new Vector3(90f, 0f, 0f));
            Shapes.Make(PrimitiveType.Cube, "TargetChestH", model, new Vector3(0f, 0.32f, 0.212f), new Vector3(0.2f, 0.02f, 0.01f), black, false);
            Shapes.Make(PrimitiveType.Cube, "TargetChestV", model, new Vector3(0f, 0.32f, 0.212f), new Vector3(0.02f, 0.2f, 0.01f), black, false);

            // Neck and head
            LowPoly.Make(LowPoly.Cylinder(8), "Neck", model, new Vector3(0f, 0.56f, 0f), new Vector3(0.14f, 0.12f, 0.14f), black);
            var head = new GameObject("Head").transform;
            head.SetParent(model, false);
            head.localPosition = new Vector3(0f, DummyRig.HeadY, 0f);
            LowPoly.Make(LowPoly.Ico(1), "Skull", head, Vector3.zero, new Vector3(0.44f, 0.48f, 0.44f), body);
            foreach (float side in new[] { -1f, 1f })
            {
                LowPoly.Make(LowPoly.Cylinder(12), "Target", head, new Vector3(side * 0.224f, 0.02f, 0f), new Vector3(0.18f, 0.01f, 0.18f), yellow,
                    new Vector3(0f, 0f, 90f));
                Shapes.Make(PrimitiveType.Cube, "TargetH", head, new Vector3(side * 0.23f, 0.02f, 0f), new Vector3(0.01f, 0.02f, 0.18f), black, false);
                Shapes.Make(PrimitiveType.Cube, "TargetV", head, new Vector3(side * 0.23f, 0.02f, 0f), new Vector3(0.01f, 0.18f, 0.02f), black, false);
                // Googly eyes
                LowPoly.Make(LowPoly.Ico(1), "Eye", head, new Vector3(side * 0.085f, 0.06f, 0.18f), new Vector3(0.11f, 0.12f, 0.08f), eyeWhite);
                LowPoly.Make(LowPoly.Ico(0), "Pupil", head, new Vector3(side * 0.08f, 0.055f, 0.215f), new Vector3(0.05f, 0.05f, 0.03f), black);
            }
            Shapes.Make(PrimitiveType.Cube, "Mouth", head, new Vector3(0f, -0.09f, 0.205f), new Vector3(0.1f, 0.02f, 0.02f), black, false);

            // Arms (shoulder pivots) and legs (hip pivots)
            armL = Arm("ArmL", -1f, body, shade, black);
            armR = Arm("ArmR", 1f, body, shade, black);
            legL = Leg("LegL", -1f, body, shade, black);
            legR = Leg("LegR", 1f, body, shade, black);

            Rig = new DummyRig { model = model, head = head, armL = armL, armR = armR, legL = legL, legR = legR, body = body };

            // The hand: weapon models are built pointing along +Z, so this turns them to point down the arm.
            hand = new GameObject("Hand").transform;
            hand.SetParent(armR, false);
            hand.localPosition = new Vector3(0f, DummyRig.HandY, 0f);
            hand.localRotation = Quaternion.Euler(90f, 0f, 0f);

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

        Transform Arm(string name, float side, Material body, Material shade, Material black)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(model, false);
            pivot.localPosition = new Vector3(side * 0.36f, 0.42f, 0f);
            LowPoly.Make(LowPoly.Ico(1), "Shoulder", pivot, Vector3.zero, Vector3.one * 0.18f, black);
            Shapes.Make(PrimitiveType.Capsule, "UpperArm", pivot, new Vector3(0f, DummyRig.UpperArmY, 0f), new Vector3(0.14f, 0.15f, 0.14f), body, false);
            LowPoly.Make(LowPoly.Torus(10, 4, 0.3f), "Elbow", pivot, new Vector3(0f, -0.3f, 0f), new Vector3(0.15f, 0.08f, 0.15f), black);
            Shapes.Make(PrimitiveType.Capsule, "Forearm", pivot, new Vector3(0f, DummyRig.ForearmY, 0f), new Vector3(0.12f, 0.13f, 0.12f), shade, false);
            LowPoly.Make(LowPoly.Ico(1), "Hand", pivot, new Vector3(0f, DummyRig.HandY, 0f), new Vector3(0.15f, 0.16f, 0.13f), body);
            return pivot;
        }

        Transform Leg(string name, float side, Material body, Material shade, Material black)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(model, false);
            pivot.localPosition = new Vector3(side * 0.13f, -0.18f, 0f);
            Shapes.Make(PrimitiveType.Capsule, "Thigh", pivot, new Vector3(0f, DummyRig.ThighY, 0f), new Vector3(0.18f, 0.17f, 0.18f), body, false);
            LowPoly.Make(LowPoly.Torus(10, 4, 0.3f), "Knee", pivot, new Vector3(0f, -0.36f, 0f), new Vector3(0.18f, 0.08f, 0.18f), black);
            Shapes.Make(PrimitiveType.Capsule, "Shin", pivot, new Vector3(0f, DummyRig.ShinY, 0f), new Vector3(0.15f, 0.15f, 0.15f), shade, false);
            Shapes.Make(PrimitiveType.Cube, "Foot", pivot, new Vector3(0f, DummyRig.FootY, 0.05f), new Vector3(0.16f, 0.1f, 0.28f), black, false);
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
