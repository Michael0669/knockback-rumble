using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// A platform on a hydraulic piston that rises and sinks on the shared clock.
    /// A row of these with offset phases makes a wave you have to time your jumps on.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PistonPillar : MonoBehaviour
    {
        public float baseY;
        public float amplitude = 1.4f;
        public float period = 3.2f;
        public float phase;

        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void FixedUpdate()
        {
            float y = baseY + amplitude * Mathf.Sin((Net.Time / period) * Mathf.PI * 2f + phase);
            Vector3 p = transform.position;
            rb.MovePosition(new Vector3(p.x, y, p.z));
        }
    }
}
