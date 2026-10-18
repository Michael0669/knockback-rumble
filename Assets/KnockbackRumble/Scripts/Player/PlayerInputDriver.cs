using UnityEngine;
using UnityEngine.InputSystem;

namespace KnockbackRumble
{
    /// <summary>
    /// Reads keyboard/mouse and gamepad and drives the local player's dummy.
    ///   Move: WASD / left stick      Jump: Space / A       Dive: Left Ctrl or C / B
    ///   Fire: Left mouse / RT        Shove: E, F or right mouse / X or RB
    ///   Esc frees the mouse; click to grab it again.
    /// </summary>
    public class PlayerInputDriver : MonoBehaviour
    {
        Contestant me;
        ThirdPersonCamera cam;
        Camera unityCam;

        public void Init(Contestant contestant, ThirdPersonCamera camera)
        {
            me = contestant;
            cam = camera;
            unityCam = camera.GetComponent<Camera>();
        }

        void Start() => SetCursorLocked(true);

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            if (me == null || cam == null) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;

            if (kb != null && kb.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            if (!locked && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                SetCursorLocked(true);
                return; // don't fire on the click that grabs the mouse
            }

            // Move, relative to the camera
            Vector2 m = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed) m.y += 1f;
                if (kb.sKey.isPressed) m.y -= 1f;
                if (kb.dKey.isPressed) m.x += 1f;
                if (kb.aKey.isPressed) m.x -= 1f;
            }
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f) m += stick;
            }
            m = Vector2.ClampMagnitude(m, 1f);
            me.Motor.MoveInput = cam.FlatRight * m.x + cam.FlatForward * m.y;

            // Jump (also mashes you out of a stun faster)
            if ((kb != null && kb.spaceKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame))
                me.Motor.QueueJump();

            // Dive
            if ((kb != null && (kb.leftCtrlKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame)) ||
                (pad != null && pad.buttonEast.wasPressedThisFrame))
                me.Motor.QueueDive();

            // With a gun you face where the camera looks; otherwise you face where you run.
            me.Motor.FacingInput = me.Weapons.Held != null ? cam.FlatForward : Vector3.zero;

            // Fire (hold to keep shooting)
            bool fire = (locked && mouse != null && mouse.leftButton.isPressed) || (pad != null && pad.rightTrigger.isPressed);
            if (fire && me.Weapons.CanFire)
                me.Weapons.TryFire(AimDirection());

            // Shove
            bool shove = (kb != null && (kb.eKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)) ||
                         (locked && mouse != null && mouse.rightButton.wasPressedThisFrame) ||
                         (pad != null && (pad.buttonWest.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame));
            if (shove) me.Weapons.TryShove();
        }

        /// <summary>Shoot toward whatever is under the crosshair.</summary>
        Vector3 AimDirection()
        {
            Transform camT = unityCam != null ? unityCam.transform : cam.transform;
            Vector3 origin = camT.position;
            Vector3 forward = camT.forward;
            Vector3 point = origin + forward * 200f;

            float best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(origin, forward, 200f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.attachedRigidbody == me.Motor.Body) continue;
                if (hit.distance < 1.5f) continue; // ignore stuff between the camera and the dummy
                if (hit.distance < best) { best = hit.distance; point = hit.point; }
            }

            Vector3 dir = point - me.Weapons.FirePoint;
            return dir.sqrMagnitude < 0.01f ? forward : dir.normalized;
        }
    }
}
