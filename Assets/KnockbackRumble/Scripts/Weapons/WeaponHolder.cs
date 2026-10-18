using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Holds one picked-up weapon (limited ammo, no reloads) plus the always-on Shove.</summary>
    public class WeaponHolder : MonoBehaviour
    {
        public WeaponDef Held { get; private set; }
        public int Ammo { get; private set; }
        public float ShoveCooldown => Mathf.Max(0f, shoveTimer);

        DummyMotor motor;
        DummyVisual visual;
        float fireTimer, shoveTimer;

        public Vector3 FirePoint => transform.position + Vector3.up * 1.35f + transform.forward * 0.55f;

        void Awake()
        {
            motor = GetComponent<DummyMotor>();
            visual = GetComponent<DummyVisual>();
        }

        void Update()
        {
            fireTimer -= Time.deltaTime;
            shoveTimer -= Time.deltaTime;
        }

        bool CanAct => motor != null && !motor.Stunned && !motor.InputLocked;
        public bool CanFire => Held != null && Ammo > 0 && fireTimer <= 0f && CanAct;
        public bool CanShove => shoveTimer <= 0f && CanAct;

        public void Give(WeaponDef def)
        {
            Held = def;
            Ammo = def.ammo;
            fireTimer = 0.15f;
            if (visual != null) visual.ShowWeapon(def);
        }

        public void Clear()
        {
            Held = null;
            Ammo = 0;
            if (visual != null) visual.ShowWeapon(null);
        }

        public bool TryFire(Vector3 aimDirection)
        {
            if (!CanFire) return false;
            fireTimer = Held.cooldown;
            Ammo--;
            Weapons.Fire(Held, gameObject, FirePoint, aimDirection);
            if (Ammo <= 0) Clear();
            return true;
        }

        public bool TryShove()
        {
            if (!CanShove) return false;
            shoveTimer = WeaponDef.Shove.cooldown;
            Weapons.Fire(WeaponDef.Shove, gameObject, FirePoint, transform.forward);
            if (visual != null) visual.PlayShove();
            return true;
        }
    }
}
