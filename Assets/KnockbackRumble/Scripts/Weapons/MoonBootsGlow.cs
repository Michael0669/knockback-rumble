using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Little sparkles at your feet while Moon Boots last (removes itself).</summary>
    public class MoonBootsGlow : MonoBehaviour
    {
        float left = Gadgets.MoonBootsTime;

        void Update()
        {
            left -= Time.deltaTime;
            if (left <= 0f) { Destroy(this); return; }
            if (Random.value < 0.5f)
                Puffs.Spark(transform.position + Random.insideUnitSphere * 0.4f, Vector3.down * 0.5f + Random.insideUnitSphere, 0.07f, 0.5f,
                    new Color(0.7f, 0.8f, 1f));
        }
    }
}
