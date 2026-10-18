using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Stops a hazard from hitting the same dummy every frame.</summary>
    public class HitCooldown
    {
        readonly Dictionary<DentBody, float> lastHit = new Dictionary<DentBody, float>();

        public bool TryHit(DentBody body, float cooldown)
        {
            if (lastHit.TryGetValue(body, out float t) && Time.time - t < cooldown) return false;
            lastHit[body] = Time.time;
            return true;
        }
    }
}
