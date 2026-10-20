using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Turns slowly around its own Y axis (the blimp's orbit, signs, decorations).</summary>
    public class Spinner : MonoBehaviour
    {
        public float degreesPerSecond = 10f;

        void Update() => transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }
}
