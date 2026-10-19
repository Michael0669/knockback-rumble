using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>A floating weapon crate. Touch it to grab the weapon; it respawns after a few seconds.</summary>
    public class WeaponPickup : MonoBehaviour
    {
        public static readonly List<WeaponPickup> All = new List<WeaponPickup>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        [Tooltip("pistol, rocket or random")]
        public string weaponId = "random";
        public float respawnTime = 20f;

        public bool Available { get; private set; } = true;

        Transform crate;
        float timer;

        /// <summary>Called by the level builder (edit mode) to make the trigger and crate mesh.</summary>
        public void Setup(string id)
        {
            weaponId = id;
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.6f, 2f, 1.6f);
            box.center = new Vector3(0f, 1f, 0f);

            Color c = id == "pistol" ? WeaponDef.PopPistol.color
                    : id == "rocket" ? WeaponDef.RocketLauncher.color
                    : Palette.Gold;
            var go = Shapes.Make(PrimitiveType.Cube, "Crate", transform, new Vector3(0f, 1f, 0f), Vector3.one * 0.8f,
                Mats.Get("Crate_" + id, c), false);
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
                timer -= Time.deltaTime;
                if (timer <= 0f) SetAvailable(true);
                return;
            }
            if (crate != null)
            {
                crate.localPosition = new Vector3(0f, 1f + Mathf.Sin(Time.time * 2f + transform.position.z) * 0.15f, 0f);
                crate.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!Available) return;
            var holder = other.GetComponentInParent<WeaponHolder>();
            if (holder == null) return;
            var def = WeaponDef.FromId(weaponId);
            if (holder.Held == def && holder.Ammo >= def.ammo) return; // already full, leave it for someone else
            holder.Give(def);

            var c = holder.GetComponent<Contestant>();
            if (c != null && c.IsPlayer) Announcer.ShowToast("Got the " + def.name + "!");

            SetAvailable(false);
            timer = respawnTime;
        }

        public void SetAvailable(bool on)
        {
            Available = on;
            if (crate != null) crate.gameObject.SetActive(on);
        }
    }
}
