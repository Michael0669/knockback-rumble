using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Shared rules for the "everyone gets the same gun, unlimited ammo" games
    /// (Shotgun Wedding, Gone With The Wind, Duck Season, Moon Base...). Bottomless Clip
    /// keeps your own loadout gun (Gun = null). Knock-offs pay a bonus.
    /// </summary>
    public abstract class FrenzyGame : MiniGame
    {
        /// <summary>The gun everyone gets (null = keep your loadout's gun).</summary>
        protected abstract WeaponDef Gun { get; }
        /// <summary>Extra cash per knock-off on top of the usual $100.</summary>
        protected virtual int KoBonus => 50;

        public override bool InfiniteAmmo => true;
        public override bool StreaksAllowed => false;
        public override string PrizeLabel => "KNOCK-OFF BONUS";

        float nextCheck;

        public override void SetupLocal(Contestant c)
        {
            base.SetupLocal(c);
            if (Gun != null) c.Weapons.Give(Gun);
        }

        public override void Tick()
        {
            if (Gun == null || Time.time < nextCheck) return;
            nextCheck = Time.time + 0.25f;
            // Anyone who picked something else up (or respawned) gets the game's gun back.
            foreach (var c in LocalAlive())
                if (c.Weapons.Held != Gun) c.Weapons.Give(Gun);
        }

        public override void HostOnFell(Contestant victim, Contestant attacker)
        {
            if (attacker != null && KoBonus > 0) Award(attacker, KoBonus);
        }

        /// <summary>Bots: go after the closest rival, but stay inside the arena.</summary>
        protected Vector3? ChaseClosest(Contestant me, Vector3 pos, float keepInside)
        {
            Contestant best = null;
            float bestD = float.MaxValue;
            foreach (var c in Alive())
            {
                if (c == me) continue;
                float d = (c.transform.position - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = c; }
            }
            Vector3 goal;
            if (best != null)
            {
                // Keep some distance with guns; close in with short-range ones.
                float want = Gun == WeaponDef.LeafBlower || Gun == WeaponDef.BoomStick ? 3f : 9f;
                Vector3 to = Shapes.Flat(best.transform.position - pos);
                goal = to.magnitude > want ? best.transform.position - to.normalized * want : pos - to.normalized * 2f;
            }
            else goal = Origin;
            Vector3 off = Shapes.Flat(goal - Origin);
            if (off.magnitude > keepInside) goal = Origin + off.normalized * keepInside + Vector3.up * (goal.y - Origin.y);
            return goal;
        }
    }
}
