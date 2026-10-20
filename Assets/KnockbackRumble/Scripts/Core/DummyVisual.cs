using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Greybox crash-test dummy built from primitives, with procedural animation:
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

        public void Build(Color bodyColor)
        {
            motor = GetComponent<DummyMotor>();
            var body = Mats.Get("Dummy_" + ColorUtility.ToHtmlStringRGB(bodyColor), bodyColor);
            var yellow = Mats.Get("Dummy_Yellow", Palette.DummyYellow);
            var black = Mats.Get("Dummy_Black", Palette.Black);

            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            model.localPosition = new Vector3(0f, 0.9f, 0f); // pivot at the body's center so tumbles look right

            Shapes.Make(PrimitiveType.Capsule, "Torso", model, new Vector3(0f, 0.15f, 0f), new Vector3(0.75f, 0.5f, 0.5f), body, false);
            Shapes.Make(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 0.75f, 0f), new Vector3(0.5f, 0.5f, 0.5f), body, false);
            Shapes.Make(PrimitiveType.Cube, "Visor", model, new Vector3(0f, 0.78f, 0.2f), new Vector3(0.36f, 0.08f, 0.14f), black, false);
            // Crash-test target decals on the sides of the head and chest
            Shapes.Make(PrimitiveType.Cylinder, "TargetL", model, new Vector3(-0.245f, 0.75f, 0f), new Vector3(0.22f, 0.01f, 0.22f), yellow, false, new Vector3(0f, 0f, 90f));
            Shapes.Make(PrimitiveType.Cylinder, "TargetR", model, new Vector3(0.245f, 0.75f, 0f), new Vector3(0.22f, 0.01f, 0.22f), yellow, false, new Vector3(0f, 0f, 90f));
            Shapes.Make(PrimitiveType.Cylinder, "TargetChest", model, new Vector3(0f, 0.3f, 0.25f), new Vector3(0.24f, 0.01f, 0.24f), yellow, false, new Vector3(90f, 0f, 0f));

            legL = Limb("LegL", new Vector3(-0.18f, -0.2f, 0f), new Vector3(0.24f, 0.35f, 0.24f), -0.35f, body);
            legR = Limb("LegR", new Vector3(0.18f, -0.2f, 0f), new Vector3(0.24f, 0.35f, 0.24f), -0.35f, body);
            armL = Limb("ArmL", new Vector3(-0.45f, 0.38f, 0f), new Vector3(0.18f, 0.3f, 0.18f), -0.3f, body);
            armR = Limb("ArmR", new Vector3(0.45f, 0.38f, 0f), new Vector3(0.18f, 0.3f, 0.18f), -0.3f, body);

            // The hand: weapon models are built pointing along +Z, so this turns them to point down the arm.
            hand = new GameObject("Hand").transform;
            hand.SetParent(armR, false);
            hand.localPosition = new Vector3(0f, -0.6f, 0f);
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

        Transform Limb(string name, Vector3 pivotPos, Vector3 scale, float drop, Material mat)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(model, false);
            pivot.localPosition = pivotPos;
            Shapes.Make(PrimitiveType.Capsule, name + "Mesh", pivot, new Vector3(0f, drop, 0f), scale, mat, false);
            return pivot;
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
