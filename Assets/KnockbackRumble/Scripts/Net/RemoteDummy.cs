using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Shows a dummy that's simulated on another machine. Its owner sends ~20 snapshots
    /// a second; we draw it slightly in the past (Delay) and blend between snapshots so
    /// movement looks smooth even though updates arrive in steps.
    /// </summary>
    public class RemoteDummy : MonoBehaviour
    {
        public const float Delay = 0.1f;
        const float MaxExtrapolate = 0.25f;
        const float TeleportDistance = 8f;

        struct Snap
        {
            public float t;
            public Vector3 pos;
            public float yaw;
            public Vector3 vel;
            public byte flags;
        }

        public const byte FlagGrounded = 1, FlagStunned = 2, FlagDiving = 4;

        readonly List<Snap> snaps = new List<Snap>(24);
        Contestant c;
        byte weapon = 255;

        public void Init(Contestant contestant)
        {
            c = contestant;
            c.Motor.SetRemote(true);
        }

        public void Push(float time, Vector3 pos, float yaw, Vector3 vel, byte flags, float dent, byte weaponId)
        {
            if (snaps.Count > 0)
            {
                var last = snaps[snaps.Count - 1];
                if (time <= last.t) return; // late or duplicate packet
                if ((pos - last.pos).sqrMagnitude > TeleportDistance * TeleportDistance) snaps.Clear(); // respawn: snap, don't slide
            }
            snaps.Add(new Snap { t = time, pos = pos, yaw = yaw, vel = vel, flags = flags });
            if (snaps.Count > 20) snaps.RemoveAt(0);

            c.Dent.SetRemoteDent(dent);
            if (weaponId != weapon)
            {
                weapon = weaponId;
                c.Weapons.SetRemote(WeaponDef.ById(weaponId));
            }
        }

        void Update()
        {
            if (snaps.Count == 0) return;
            float rt = Net.Time - Delay;

            // Throw away snapshots we've fully passed (keep one before "now" to blend from).
            while (snaps.Count > 2 && snaps[1].t <= rt) snaps.RemoveAt(0);

            Snap a = snaps[0];
            Vector3 pos;
            float yaw;
            Vector3 vel = a.vel;
            byte flags = a.flags;

            if (rt <= a.t || snaps.Count == 1)
            {
                float ahead = Mathf.Clamp(rt - a.t, 0f, MaxExtrapolate);
                pos = a.pos + a.vel * ahead;
                yaw = a.yaw;
            }
            else
            {
                Snap b = snaps[1];
                if (rt >= b.t)
                {
                    float ahead = Mathf.Min(rt - b.t, MaxExtrapolate);
                    pos = b.pos + b.vel * ahead;
                    yaw = b.yaw;
                    vel = b.vel;
                    flags = b.flags;
                }
                else
                {
                    float k = (rt - a.t) / Mathf.Max(0.0001f, b.t - a.t);
                    pos = Vector3.Lerp(a.pos, b.pos, k);
                    yaw = Mathf.LerpAngle(a.yaw, b.yaw, k);
                    vel = Vector3.Lerp(a.vel, b.vel, k);
                    flags = k < 0.5f ? a.flags : b.flags;
                }
            }

            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            c.Motor.SetRemoteState(vel, (flags & FlagGrounded) != 0, (flags & FlagStunned) != 0, (flags & FlagDiving) != 0);
        }
    }
}
