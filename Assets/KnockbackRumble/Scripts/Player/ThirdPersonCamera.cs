using UnityEngine;
using UnityEngine.InputSystem;

namespace KnockbackRumble
{
    /// <summary>Over-the-shoulder orbit camera with wall collision. Mouse or right stick.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 5.5f;
        public float height = 1.6f;
        public float shoulder = 0.7f;
        public float mouseSensitivity = 0.12f;
        public float gamepadSensitivity = 170f;
        public float minPitch = -30f;
        public float maxPitch = 65f;
        public float collisionRadius = 0.25f;

        public float Yaw { get; set; }
        public float Pitch { get; set; } = 12f;

        public Vector3 FlatForward => Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
        public Vector3 FlatRight => Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;

        public void SetTarget(Transform t)
        {
            target = t;
            if (t != null) Yaw = t.eulerAngles.y;
        }

        void LateUpdate()
        {
            if (target == null) return;

            Vector2 look = Vector2.zero;
            if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
                look += Mouse.current.delta.ReadValue() * mouseSensitivity;
            if (Gamepad.current != null)
                look += Gamepad.current.rightStick.ReadValue() * gamepadSensitivity * Time.deltaTime;

            Yaw += look.x;
            Pitch = Mathf.Clamp(Pitch - look.y, minPitch, maxPitch);

            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * height;
            Vector3 offset = rot * new Vector3(shoulder, 0f, -distance);
            float dist = offset.magnitude;
            Vector3 dir = offset / dist;

            // Pull in when a wall is between the dummy and the camera (dummies themselves don't block)
            float best = dist;
            foreach (var hit in Physics.SphereCastAll(pivot, collisionRadius, dir, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.attachedRigidbody != null && !hit.collider.attachedRigidbody.isKinematic) continue;
                if (hit.distance <= 0f) continue;
                if (hit.distance < best) best = hit.distance;
            }

            transform.position = pivot + dir * Mathf.Max(0.4f, best - 0.05f);
            transform.rotation = rot;
        }
    }
}
