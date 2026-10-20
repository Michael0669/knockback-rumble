using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// A floating ammo crate. Touch it to refill your loadout weapon (and your gadget with
    /// the Scavenger perk). It respawns after a while. The host decides who got it.
    /// </summary>
    public class WeaponPickup : MonoBehaviour
    {
        public static readonly List<WeaponPickup> All = new List<WeaponPickup>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        [Tooltip("Kept for old scenes; every crate is an ammo crate now")]
        public string weaponId = "ammo";
        public float respawnTime = 20f;

        public bool Available { get; private set; } = true;

        /// <summary>Same on every machine (crates are numbered by position when the match starts).</summary>
        public int Index;

        Transform crate;
        float timer, nextRequest;

        /// <summary>Called by the level builder (edit mode) to make the trigger and crate mesh.</summary>
        public void Setup(string id)
        {
            weaponId = id;
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.6f, 2f, 1.6f);
            box.center = new Vector3(0f, 1f, 0f);

            var go = Shapes.Make(PrimitiveType.Cube, "Crate", transform, new Vector3(0f, 1f, 0f), Vector3.one * 0.8f,
                Mats.Get("Crate_Ammo", new Color(0.45f, 0.55f, 0.25f)), false);
            Shapes.Make(PrimitiveType.Cube, "Band", go.transform, Vector3.zero, new Vector3(1.05f, 0.25f, 1.05f),
                Mats.Get("Dummy_Black", Palette.Black), false);
            crate = go.transform;
        }

        void Awake() => crate = transform.Find("Crate");
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        void Update()
        {
            if (!Available)
            {
                // Only the host decides when crates come back (it tells everyone else).
                if (!Net.IsHost) return;
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    SetAvailable(true);
                    if (MatchController.Instance != null) MatchController.Instance.HostCrateRespawned(this);
                }
                return;
            }
            if (crate != null)
            {
                crate.localPosition = new Vector3(0f, 1f + Mathf.Sin(Time.time * 2f + transform.position.z) * 0.15f, 0f);
                crate.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            }
        }

        void OnTriggerEnter(Collider other) => TryGrab(other);
        void OnTriggerStay(Collider other) => TryGrab(other);

        void TryGrab(Collider other)
        {
            if (!Available || Time.time < nextRequest) return;
            var c = other.GetComponentInParent<Contestant>();
            if (c == null || !c.IsLocal) return; // each machine only grabs for its own dummies
            if (c.Eliminated || c.Weapons.Disabled) return;
            if (!c.Weapons.NeedsRefill(Scavenges(c))) return; // already full, leave it for someone else
            nextRequest = Time.time + 0.5f;

            if (MatchController.Instance != null) MatchController.Instance.RequestCrate(this, c);
            else Take(c);
        }

        static bool Scavenges(Contestant c) => c.Loadout != null && c.Loadout.Has(Perk.Scavenger);

        /// <summary>The crate was taken by c (on every machine). Only the owner of c gets the ammo.</summary>
        public void Take(Contestant c)
        {
            SetAvailable(false);
            timer = respawnTime;
            if (c == null || !c.IsLocal) return;
            bool gadget = Scavenges(c);
            c.Weapons.Refill(gadget);
            if (c.IsPlayer) Announcer.ShowToast(gadget ? "AMMO + GADGET REFILLED!" : "AMMO REFILLED!");
        }

        public void ResetCrate()
        {
            SetAvailable(true);
            timer = 0f;
            nextRequest = 0f;
        }

        public void SetAvailable(bool on)
        {
            Available = on;
            if (crate != null) crate.gameObject.SetActive(on);
        }
    }
}
