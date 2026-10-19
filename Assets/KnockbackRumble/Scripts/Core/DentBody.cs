using System;
using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// The Dent meter. Nobody has health: every hit adds Dent, and the higher your
    /// Dent, the farther the next hit launches you. You only lose by falling off.
    /// </summary>
    [RequireComponent(typeof(DummyMotor))]
    public class DentBody : MonoBehaviour
    {
        public static readonly List<DentBody> All = new List<DentBody>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        public const float MaxDent = 200f;

        [Tooltip("Extra knockback multiplier per 100% Dent (1.2 = a hit at 100% launches 2.2x as far)")]
        public float dentScaling = 1.2f;

        public float Dent { get; private set; }
        /// <summary>False for dummies simulated on another machine (their hits get sent there).</summary>
        public bool IsLocal { get; set; } = true;
        public DummyMotor Motor { get; private set; }
        public GameObject LastAttacker { get; private set; }
        public string LastWeapon { get; private set; }
        public float LastHitTime { get; private set; } = -99f;
        /// <summary>When you last blasted yourself (rocket jumps), for "self-own" commentary.</summary>
        public float LastSelfHitTime { get; private set; } = -99f;
        public Vector3 Center => transform.position + Vector3.up * 0.9f;

        /// <summary>Fired on every hit: (victim, knockback strength, attacker or null, weapon/hazard name).</summary>
        public event Action<DentBody, float, GameObject, string> Launched;

        void Awake() => Motor = GetComponent<DummyMotor>();
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        /// <param name="direction">Push direction; does not need to be normalized.</param>
        /// <param name="baseKnock">Launch speed at 0% Dent.</param>
        /// <param name="dentAdd">Dent added by this hit (halved if you hit yourself).</param>
        /// <param name="attacker">Who caused it (null for hazards).</param>
        /// <param name="minLift">Minimum upward share of the launch direction.</param>
        public void Hit(Vector3 direction, float baseKnock, float dentAdd, GameObject attacker, string weapon, float minLift = 0.25f)
        {
            if (!IsLocal)
            {
                // This dummy lives on another machine. If one of OUR dummies caused the hit, tell its owner.
                // Hazards (no attacker) and other machines' shots are handled on the owner's machine instead.
                var source = attacker != null ? attacker.GetComponent<DentBody>() : null;
                if (source != null && source.IsLocal && MatchController.Instance != null)
                    MatchController.Instance.SendHit(this, direction, baseKnock, dentAdd, attacker, weapon, minLift);
                return;
            }

            bool self = attacker == gameObject;
            if (self) dentAdd *= 0.5f;

            Dent = Mathf.Min(MaxDent, Dent + dentAdd);

            Vector3 dir = direction.sqrMagnitude < 0.0001f ? Vector3.up : direction.normalized;
            if (dir.y < minLift)
            {
                dir.y = minLift;
                dir.Normalize();
            }

            float knock = baseKnock * (1f + Dent / 100f * dentScaling);
            // Your own blasts never stun you, so rocket jumps stay controllable.
            float stun = self ? 0f : Mathf.Clamp(knock * 0.05f, 0.2f, 1.8f);
            Motor.Launch(dir * knock, stun);

            if (!self)
            {
                LastAttacker = attacker;
                LastWeapon = weapon;
                LastHitTime = Time.time;
            }
            else
            {
                LastSelfHitTime = Time.time;
            }

            Launched?.Invoke(this, knock, attacker, weapon);
        }

        /// <summary>Remote dummies just display the Dent their owner reports.</summary>
        public void SetRemoteDent(float dent) => Dent = dent;

        public void ResetDent()
        {
            Dent = 0f;
            LastAttacker = null;
            LastWeapon = null;
            LastHitTime = -99f;
            LastSelfHitTime = -99f;
        }
    }
}
