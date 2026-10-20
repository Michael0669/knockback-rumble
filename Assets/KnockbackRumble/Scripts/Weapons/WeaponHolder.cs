using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Your loadout's primary weapon (limited ammo per round, refilled by ammo crates),
    /// the always-on Shove, and your gadget charges.
    /// </summary>
    public class WeaponHolder : MonoBehaviour
    {
        public WeaponDef Held { get; private set; }
        public int Ammo { get; private set; }
        public Gadget Gadget { get; private set; }
        public int GadgetCharges { get; private set; }
        public int GadgetMax { get; private set; }
        public float ShoveCooldown => Mathf.Max(0f, shoveTimer);
        public float GadgetCooldown => Mathf.Max(0f, gadgetTimer);

        /// <summary>Knocked out of the round (e.g. sitting in the Losers' Lounge): no shooting.</summary>
        public bool Disabled { get; set; }

        DummyMotor motor;
        DummyVisual visual;
        float fireTimer, shoveTimer, gadgetTimer;

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
            gadgetTimer -= Time.deltaTime;
        }

        bool CanAct => motor != null && !motor.Stunned && !motor.InputLocked && !Disabled;
        public bool CanFire => Held != null && Ammo > 0 && fireTimer <= 0f && CanAct;
        public bool CanShove => shoveTimer <= 0f && CanAct;
        public bool CanThrow => Gadget != Gadget.None && GadgetCharges > 0 && gadgetTimer <= 0f && CanAct;

        /// <summary>Arm the dummy from a loadout at the start of a round.</summary>
        public void Equip(Loadout loadout)
        {
            Give(loadout.PrimaryDef);
            Gadget = loadout.gadget;
            var info = Loadouts.GadgetInfo(Gadget);
            GadgetMax = GadgetCharges = info != null ? info.cost : 0;
        }

        public void Give(WeaponDef def)
        {
            Held = def;
            Ammo = def.ammo;
            fireTimer = 0.15f;
            if (visual != null) visual.ShowWeapon(def);
        }

        /// <summary>Ammo crate: top up the primary (and the gadget with the Scavenger perk).</summary>
        public void Refill(bool gadgetToo)
        {
            if (Held != null) Ammo = Held.ammo;
            if (gadgetToo) GadgetCharges = GadgetMax;
        }

        public bool NeedsRefill(bool gadgetToo) =>
            (Held != null && Ammo < Held.ammo) || (gadgetToo && GadgetCharges < GadgetMax);

        /// <summary>Remote dummies: just show what their owner says they're holding.</summary>
        public void SetRemote(WeaponDef def)
        {
            Held = def;
            Ammo = def != null ? def.ammo : 0;
            if (visual != null) visual.ShowWeapon(def);
        }

        public void Clear()
        {
            Held = null;
            Ammo = 0;
            GadgetCharges = 0;
            if (visual != null) visual.ShowWeapon(null);
        }

        public bool TryFire(Vector3 aimDirection)
        {
            if (!CanFire) return false;
            fireTimer = Held.cooldown;
            Ammo--;
            var def = Held;
            Weapons.Fire(def, gameObject, FirePoint, aimDirection);
            ReportFire(def, aimDirection);
            return true;
        }

        public bool TryShove()
        {
            if (!CanShove) return false;
            shoveTimer = WeaponDef.Shove.cooldown;
            Weapons.Fire(WeaponDef.Shove, gameObject, FirePoint, transform.forward);
            ReportFire(WeaponDef.Shove, transform.forward);
            if (visual != null) visual.PlayShove();
            return true;
        }

        /// <summary>Throw / drop the loadout gadget toward aimDirection.</summary>
        public bool TryGadget(Vector3 aimDirection)
        {
            if (!CanThrow) return false;
            GadgetCharges--;
            gadgetTimer = 0.6f;
            var me = GetComponent<Contestant>();
            if (me != null) Gadgets.Use(me, Gadget, FirePoint, aimDirection);
            return true;
        }

        void ReportFire(WeaponDef def, Vector3 direction)
        {
            var match = MatchController.Instance;
            var me = GetComponent<Contestant>();
            if (match != null && me != null) match.ReportFire(me, def, FirePoint, direction);
        }
    }
}
