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
        Transform model, legL, legR, armL, armR, gun;
        Renderer gunRenderer;
        TrailRenderer trail;
        float phase, tumbleAngle, shoveTimer;
        Vector3 tumbleAxis = Vector3.right;
        bool wasStunned;

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

            var gunGo = Shapes.Make(PrimitiveType.Cube, "Gun", armR, new Vector3(0f, -0.6f, 0.2f), new Vector3(0.14f, 0.14f, 0.5f), black, false);
            gun = gunGo.transform;
            gunRenderer = gunGo.GetComponent<Renderer>();
            gunGo.SetActive(false);

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
            if (gun == null) return;
            gun.gameObject.SetActive(def != null);
            if (def != null) gunRenderer.sharedMaterial = Mats.Get("Weapon_" + def.id, def.color);
        }

        public void PlayShove() => shoveTimer = 0.2f;

        void LateUpdate()
        {
            if (model == null || motor == null) return;
            float dt = Time.deltaTime;
            Vector3 v = motor.Velocity;
            float speed = new Vector3(v.x, 0f, v.z).magnitude;

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

        void SetLimbs(float legLx, float legRx, float armLx, float armRx, float spread)
        {
            legL.localRotation = Quaternion.Euler(legLx, 0f, -spread);
            legR.localRotation = Quaternion.Euler(legRx, 0f, spread);
            armL.localRotation = Quaternion.Euler(armLx, 0f, -spread);
            armR.localRotation = Quaternion.Euler(armRx, 0f, spread);
        }
    }
}
