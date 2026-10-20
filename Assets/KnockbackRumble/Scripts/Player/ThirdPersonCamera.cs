using UnityEngine;
using UnityEngine.InputSystem;

namespace KnockbackRumble
{
    /// <summary>
    /// Over-the-shoulder orbit camera with wall collision. Mouse or right stick.
    /// While aiming (right mouse / left trigger) it pulls in and zooms to the weapon's zoom FOV;
    /// look speed slows down with the zoom so sniping stays steady.
    /// </summary>
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
        public float baseFov = 70f;
        public float aimDistance = 3.2f;
        public float zoomSpeed = 12f;

        /// <summary>Set by the player's input each frame.</summary>
        public bool Aiming { get; set; }
        /// <summary>Field of view to zoom to while aiming.</summary>
        public float AimFov { get; set; } = 52f;
        /// <summary>0 = hip, 1 = fully zoomed in.</summary>
        public float ZoomAmount { get; private set; }

        Camera unityCam;
        float currentDistance = -1f;

        public float Yaw { get; set; }
        public float Pitch { get; set; } = 12f;

        public Vector3 FlatForward => Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
        public Vector3 FlatRight => Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;

        public void SetTarget(Transform t)
        {
            target = t;
            if (t != null) Yaw = t.eulerAngles.y;
            Aiming = false;
            ZoomAmount = 0f;
            if (unityCam == null) unityCam = GetComponent<Camera>();
            if (unityCam != null) unityCam.fieldOfView = baseFov;
        }

        void LateUpdate()
        {
            if (target == null) return;
            if (unityCam == null) unityCam = GetComponent<Camera>();

            // Zoom
            float goal = Aiming ? 1f : 0f;
            ZoomAmount = Mathf.MoveTowards(ZoomAmount, goal, zoomSpeed * Time.deltaTime * 0.5f);
            float fov = Mathf.Lerp(baseFov, AimFov, ZoomAmount);
            if (unityCam != null) unityCam.fieldOfView = fov;
            float wantDistance = Mathf.Lerp(distance, aimDistance, ZoomAmount);
            if (currentDistance < 0f) currentDistance = wantDistance;
            currentDistance = Mathf.Lerp(currentDistance, wantDistance, 1f - Mathf.Exp(-zoomSpeed * Time.deltaTime));
            float lookScale = fov / baseFov;

            Vector2 look = Vector2.zero;
            if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
                look += Mouse.current.delta.ReadValue() * mouseSensitivity;
            if (Gamepad.current != null)
                look += Gamepad.current.rightStick.ReadValue() * gamepadSensitivity * Time.deltaTime;

            look *= lookScale;
            Yaw += look.x;
            Pitch = Mathf.Clamp(Pitch - look.y, minPitch, maxPitch);

            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * height;
            Vector3 offset = rot * new Vector3(shoulder, 0f, -currentDistance);
            float dist = offset.magnitude;
            Vector3 dir = offset / dist;

            // Pull in when a wall is between the dummy and the camera (dummies themselves don't block)
            float best = dist;
            foreach (var hit in Physics.SphereCastAll(pivot, collisionRadius, dir, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<DentBody>() != null) continue; // dummies never block the camera
                if (hit.distance <= 0f) continue;
                if (hit.distance < best) best = hit.distance;
            }

            transform.position = pivot + dir * Mathf.Max(0.4f, best - 0.05f);
            transform.rotation = rot;
        }
    }
}
