using UnityEngine;

namespace KnockbackRumble
{
    public class FinishLine : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var c = other.GetComponentInParent<Contestant>();
            if (c != null && RaceManager.Instance != null) RaceManager.Instance.ReachFinish(c);
        }
    }
}
