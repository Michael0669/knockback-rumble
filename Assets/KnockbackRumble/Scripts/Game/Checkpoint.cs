using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Touch it and you respawn here after falling off.</summary>
    public class Checkpoint : MonoBehaviour
    {
        public int index;

        void OnTriggerEnter(Collider other)
        {
            var c = other.GetComponentInParent<Contestant>();
            if (c != null && RaceManager.Instance != null) RaceManager.Instance.ReachCheckpoint(c, this);
        }
    }
}
