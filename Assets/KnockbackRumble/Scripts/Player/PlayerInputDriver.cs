using UnityEngine;
using UnityEngine.InputSystem;

namespace KnockbackRumble
{
    /// <summary>
    /// Reads keyboard/mouse and gamepad and drives the local player's dummy.
    ///   Move: WASD / left stick           Jump: Space / A (twice with Double Jump)
    ///   Dive: Left Ctrl or C / B           Aim (zoom): hold right mouse / LT
    ///   Fire: left mouse / RT              Shove: E or F / X or RB
    ///   Gadget: G / LB                     Payout Streak: Q / Y
    ///   Esc (or Back/Select) opens the pause menu.
    /// </summary>
    public class PlayerInputDriver : MonoBehaviour
    {
        /// <summary>How long you keep facing the crosshair after shooting.</summary>
        const float FaceAimTime = 0.8f;

        Contestant me;
        ThirdPersonCamera cam;
        Camera unityCam;
        float faceAimUntil;

        public void Init(Contestant contestant, ThirdPersonCamera camera)
        {
            me = contestant;
            cam = camera;
            unityCam = camera.GetComponent<Camera>();
        }

        void Start() => MenuUI.SetPaused(false);

        void OnDestroy()
        {
            if (cam != null) cam.Aiming = false;
        }

        void Update()
        {
            if (me == null || cam == null) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;

            // Esc (or Back on a gamepad) opens the pause menu, which frees the mouse.
            if ((kb != null && kb.escapeKey.wasPressedThisFrame) ||
                (pad != null && pad.selectButton.wasPressedThisFrame))
                MenuUI.SetPaused(!MenuUI.Paused);

            if (MenuUI.Paused)
            {
                me.Motor.MoveInput = Vector3.zero;
                cam.Aiming = false;
                return;
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

            var weapons = me.Weapons;

            // Aim / zoom (hold)
            bool aim = !weapons.Disabled && weapons.Held != null &&
                       ((mouse != null && mouse.rightButton.isPressed) || (pad != null && pad.leftTrigger.isPressed));
            cam.Aiming = aim;
            if (weapons.Held != null) cam.AimFov = weapons.Held.zoomFov;

            // Fire (hold to keep shooting)
            bool fire = (mouse != null && mouse.leftButton.isPressed) || (pad != null && pad.rightTrigger.isPressed);
            bool firePressed = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (pad != null && pad.rightTrigger.wasPressedThisFrame);
            if (firePressed && weapons.Held != null && weapons.Ammo == 0 && !weapons.Disabled) Sfx.Ui("empty_click");
            if (fire && weapons.CanFire)
            {
                weapons.TryFire(AimDirection());
                faceAimUntil = Time.time + FaceAimTime;
            }

            // Shove
            bool shove = (kb != null && (kb.eKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)) ||
                         (pad != null && (pad.buttonWest.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame));
            if (shove) weapons.TryShove();

            // Gadget
            bool gadget = (kb != null && kb.gKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame);
            if (gadget && weapons.TryGadget(AimDirection())) faceAimUntil = Time.time + FaceAimTime;

            // Payout Streak
            bool streak = (kb != null && kb.qKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
            if (streak && !me.CallReadyStreak() && me.ReadyStreaks.Count == 0)
            {
                me.KnockoffsToNextStreak(out var next);
                var info = Loadouts.StreakInfo(next);
                if (info != null) Announcer.ShowToast("No streak ready yet. " + info.name + ": " + info.cost + " knock-offs in a row");
            }

            // Face the crosshair while aiming or just after shooting; otherwise face where you run.
            me.Motor.FacingInput = aim || Time.time < faceAimUntil ? cam.FlatForward : Vector3.zero;
        }

        /// <summary>Shoot toward whatever is under the crosshair.</summary>
        Vector3 AimDirection()
        {
            Transform camT = unityCam != null ? unityCam.transform : cam.transform;
            Vector3 origin = camT.position;
            Vector3 forward = camT.forward;
            Vector3 point = origin + forward * 300f;

            float best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(origin, forward, 300f, ~0, QueryTriggerInteraction.Ignore))
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
