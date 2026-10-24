using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Physics-driven movement for a crash-test dummy. Input-agnostic: the player
    /// driver and the bot brain both just set MoveInput / FacingInput and queue jumps.
    /// Knockback arrives through Launch(), which also stuns (no control) for a moment.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class DummyMotor : MonoBehaviour
    {
        [Header("Run")]
        public float moveSpeed = 8f;
        public float groundAccel = 70f;
        public float airAccel = 28f;
        public float turnSpeed = 720f;

        [Header("Jump")]
        public float jumpSpeed = 10.5f;
        public float gravity = 28f;
        public float fallGravityMultiplier = 1.6f;
        public float maxFallSpeed = 40f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.15f;

        [Header("Dive")]
        public float diveSpeed = 12f;
        public float diveUp = 4f;
        public float diveRecoverTime = 0.35f;

        [Header("Knockback")]
        [Tooltip("How fast a stunned dummy skids to a stop on the ground")]
        public float stunnedGroundFriction = 10f;
        [Tooltip("Mashing jump while stunned shaves this much off the stun")]
        public float mashRecovery = 0.12f;

        /// <summary>World-space move direction, magnitude 0..1. Y is ignored.</summary>
        public Vector3 MoveInput { get; set; }
        /// <summary>World-space direction to face. Zero means "face where you move".</summary>
        public Vector3 FacingInput { get; set; }
        public bool InputLocked { get; set; }
        public float SpeedMultiplier { get; set; } = 1f;
        /// <summary>Extra jumps allowed in mid-air (Double Jump perk = 1).</summary>
        public int ExtraJumps { get; set; }
        /// <summary>Springs perk: higher jumps.</summary>
        public float JumpMultiplier { get; set; } = 1f;
        /// <summary>Moon Boots kill streak: floaty low gravity while this is in the future (Time.time).</summary>
        public float MoonBootsUntil { get; set; } = -1f;
        int airJumpsLeft;

        public bool Grounded { get => IsRemote ? remoteGrounded : grounded; private set => grounded = value; }
        public bool Diving { get => IsRemote ? remoteDiving : diving; private set => diving = value; }
        public bool Stunned => IsRemote ? remoteStunned : stunTimer > 0f;
        public float StunTimeLeft => stunTimer;
        public Vector3 Velocity => IsRemote ? remoteVelocity : (rb != null ? rb.linearVelocity : Vector3.zero);
        public Rigidbody Body => rb;

        /// <summary>Simulated on another machine: physics off, state comes from the network.</summary>
        public bool IsRemote { get; private set; }
        bool grounded, diving, remoteGrounded, remoteStunned, remoteDiving;
        Vector3 remoteVelocity;

        Rigidbody rb;
        CapsuleCollider capsule;
        float stunTimer, coyoteTimer, jumpBuffer, groundSuppress, diveLandTimer, conveyorTimer;
        bool diveQueued;
        Vector3 conveyorVel;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();

            rb.mass = 1f;
            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;

            capsule.height = 1.8f;
            capsule.radius = 0.4f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.sharedMaterial = new PhysicsMaterial("DummySlick")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        // ---------- Commands ----------

        public void SetRemote(bool remote)
        {
            IsRemote = remote;
            if (remote)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete; // must change before going kinematic
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.None;
            }
            else
            {
                rb.isKinematic = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        public void SetRemoteState(Vector3 velocity, bool isGrounded, bool isStunned, bool isDiving)
        {
            remoteVelocity = velocity;
            remoteGrounded = isGrounded;
            remoteStunned = isStunned;
            remoteDiving = isDiving;
        }

        public void QueueJump()
        {
            jumpBuffer = jumpBufferTime;
            if (Stunned) stunTimer = Mathf.Max(0f, stunTimer - mashRecovery);
        }

        public void QueueDive() => diveQueued = true;

        /// <summary>Instant velocity change from a hit or explosion.</summary>
        public void Launch(Vector3 impulse, float stun)
        {
            if (IsRemote) return;
            Vector3 v = rb.linearVelocity;
            if (impulse.y > 0f && v.y < 0f) v.y = 0f; // rocket jumps work even while falling
            v += impulse;
            rb.linearVelocity = v;

            if (impulse.y > 0.5f)
            {
                Grounded = false;
                groundSuppress = 0.15f;
            }
            if (stun > 0f)
            {
                stunTimer = Mathf.Max(stunTimer, stun);
                Diving = false;
            }
        }

        /// <summary>Conveyors call this every physics step while you stand on them.</summary>
        public void AddConveyor(Vector3 velocity)
        {
            conveyorVel = velocity;
            conveyorTimer = 0.1f;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (IsRemote) { transform.SetPositionAndRotation(position, rotation); return; }
            rb.position = position;
            rb.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            rb.linearVelocity = Vector3.zero;
            stunTimer = 0f;
            Diving = false;
            diveQueued = false;
            jumpBuffer = 0f;
        }

        // ---------- Simulation ----------

        void FixedUpdate()
        {
            if (IsRemote) return;
            float dt = Time.fixedDeltaTime;
            UpdateGround(dt);

            stunTimer = Mathf.Max(0f, stunTimer - dt);
            jumpBuffer -= dt;
            diveLandTimer -= dt;
            conveyorTimer -= dt;

            Vector3 v = rb.linearVelocity;
            float g = gravity * (v.y < 0f ? fallGravityMultiplier : 1f);
            if (Time.time < MoonBootsUntil) g *= 0.35f;
            v.y -= g * dt;

            Vector3 h = new Vector3(v.x, 0f, v.z);
            float vy = v.y;

            if (Stunned)
            {
                diveQueued = false;
                float friction = Grounded ? stunnedGroundFriction : 1.5f;
                h = Vector3.MoveTowards(h, Vector3.zero, friction * dt);
            }
            else
            {
                Vector3 move = InputLocked ? Vector3.zero : Shapes.Flat(MoveInput);
                if (move.sqrMagnitude > 1f) move.Normalize();
                if (diveLandTimer > 0f) move = Vector3.zero;

                float topSpeed = moveSpeed * SpeedMultiplier;

                if (Grounded)
                {
                    Vector3 desired = move * topSpeed;
                    if (conveyorTimer > 0f) desired += conveyorVel;
                    h = Vector3.MoveTowards(h, desired, groundAccel * dt);
                }
                else
                {
                    // Strong air control, but steering can't add speed beyond what you had
                    // (so launches keep their momentum and you can fight back mid-air).
                    float before = h.magnitude;
                    Vector3 nh = h + move * airAccel * dt;
                    float cap = Mathf.Max(before, topSpeed);
                    if (nh.magnitude > cap) nh = nh.normalized * cap;
                    if (nh.magnitude > topSpeed) nh = Vector3.MoveTowards(nh, nh.normalized * topSpeed, 2f * dt);
                    h = nh;
                }

                // Jump (with coyote time and input buffering)
                if (jumpBuffer > 0f && !InputLocked && (Grounded || coyoteTimer > 0f))
                {
                    vy = jumpSpeed * JumpMultiplier;
                    jumpBuffer = 0f;
                    coyoteTimer = 0f;
                    Grounded = false;
                    groundSuppress = 0.12f;
                }
                else if (jumpBuffer > 0f && !InputLocked && airJumpsLeft > 0)
                {
                    // Double Jump perk
                    vy = jumpSpeed * 0.9f * JumpMultiplier;
                    jumpBuffer = 0f;
                    airJumpsLeft--;
                }

                // Dive: a forward lunge you can use once per airtime
                if (diveQueued)
                {
                    diveQueued = false;
                    if (!Diving && !InputLocked)
                    {
                        Vector3 f = Shapes.Flat(transform.forward).normalized;
                        h = f * Mathf.Max(diveSpeed, h.magnitude);
                        vy = Mathf.Max(vy, diveUp);
                        Diving = true;
                        Grounded = false;
                        groundSuppress = 0.12f;
                    }
                }

                // Facing
                Vector3 face = Shapes.Flat(FacingInput);
                if (face.sqrMagnitude < 0.01f) face = move;
                if (face.sqrMagnitude > 0.01f && !Diving)
                {
                    Quaternion target = Quaternion.LookRotation(face.normalized, Vector3.up);
                    rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, target, turnSpeed * dt));
                }
            }

            if (Diving && Grounded)
            {
                Diving = false;
                diveLandTimer = diveRecoverTime;
            }

            vy = Mathf.Max(vy, -maxFallSpeed);
            rb.linearVelocity = new Vector3(h.x, vy, h.z);
        }

        void UpdateGround(float dt)
        {
            groundSuppress -= dt;
            if (groundSuppress > 0f)
            {
                Grounded = false;
            }
            else
            {
                float r = capsule.radius * 0.9f;
                Vector3 origin = rb.position + Vector3.up * (r + 0.05f);
                var hits = Physics.SphereCastAll(origin, r, Vector3.down, 0.2f, ~0, QueryTriggerInteraction.Ignore);
                bool found = false;
                foreach (var hit in hits)
                {
                    if (hit.collider.attachedRigidbody == rb) continue;
                    if (hit.distance <= 0f && hit.point == Vector3.zero) continue; // started overlapping
                    if (Vector3.Dot(hit.normal, Vector3.up) < 0.55f) continue;     // too steep
                    found = true;
                    break;
                }
                Grounded = found;
            }

            if (Grounded) { coyoteTimer = coyoteTime; airJumpsLeft = ExtraJumps; }
            else coyoteTimer -= dt;
        }
    }
}
