using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Pushes anyone standing on it. Stripes scroll so you can see which way.</summary>
    public class Conveyor : MonoBehaviour
    {
        [Tooltip("World-space belt velocity")]
        public Vector3 velocity = new Vector3(0f, 0f, -3f);
        public Transform stripes;
        public float stripeLength = 28f;

        void OnCollisionStay(Collision collision)
        {
            var motor = collision.collider.GetComponentInParent<DummyMotor>();
            if (motor != null) motor.AddConveyor(velocity);
        }

        void Update()
        {
            if (stripes == null || stripeLength <= 0f) return;
            Vector3 dir = velocity.normalized;
            float half = stripeLength * 0.5f;
            foreach (Transform s in stripes)
            {
                s.position += velocity * Time.deltaTime;
                // Wrap along the belt
                float along = Vector3.Dot(s.position - stripes.position, dir);
                if (along > half) s.position -= dir * stripeLength;
            }
        }
    }
}
