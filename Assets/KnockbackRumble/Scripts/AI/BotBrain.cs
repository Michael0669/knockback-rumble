using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Simple race bot: follows the course waypoints, jumps gaps and walls, grabs
    /// nearby weapon crates, shoots whoever is in range and shoves anyone close.
    /// "skill" (0..1) scales speed, aim and reaction time.
    /// </summary>
    public class BotBrain : MonoBehaviour
    {
        Contestant me;
        CourseInfo course;
        float skill;
        int waypoint;
        float lateral;
        float fireTimer, shoveTimer, stuckTimer, retargetTimer;
        Vector3 lastPos;
        DentBody target;

        public void Init(Contestant contestant, CourseInfo courseInfo, float skillLevel)
        {
            me = contestant;
            course = courseInfo;
            skill = Mathf.Clamp01(skillLevel);
            lateral = Random.Range(-1.5f, 1.5f);
            fireTimer = Random.Range(1f, 2.5f);
            ResetPath();
        }

        /// <summary>Pick the waypoint nearest ahead of us (after respawns or restarts).</summary>
        public void ResetPath()
        {
            waypoint = 0;
            if (course == null || course.waypoints == null) return;
            while (waypoint < course.waypoints.Length && course.waypoints[waypoint].z < transform.position.z - 1f)
                waypoint++;
            lastPos = transform.position;
            stuckTimer = 0f;
        }

        void Update()
        {
            if (me == null || course == null || course.waypoints == null || course.waypoints.Length == 0) return;
            float dt = Time.deltaTime;
            var motor = me.Motor;
            Vector3 pos = transform.position;

            if (me.Finished)
            {
                // Celebrate: hop in place and shove anyone nearby
                motor.MoveInput = Vector3.zero;
                if (motor.Grounded && Random.value < dt * 0.8f) motor.QueueJump();
                return;
            }

            // Respawned behind our current waypoint? Re-find the path.
            if (waypoint > 0 && pos.z < course.waypoints[waypoint - 1].z - 6f) ResetPath();

            // Advance along the route
            while (waypoint < course.waypoints.Length && Reached(pos, course.waypoints[waypoint])) waypoint++;
            Vector3 goal = waypoint < course.waypoints.Length ? course.waypoints[waypoint] : pos + Vector3.forward * 10f;

            Vector3 toGoal = Shapes.Flat(goal - pos);
            Vector3 fwd = toGoal.sqrMagnitude > 0.01f ? toGoal.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 steerPoint = goal + right * lateral;

            // Detour for a nearby crate if we're empty-handed
            if (me.Weapons.Held == null)
            {
                foreach (var p in WeaponPickup.All)
                {
                    if (!p.Available) continue;
                    Vector3 to = p.transform.position - pos;
                    if (to.z > -1f && to.z < 9f && Shapes.Flat(to).magnitude < 7f && Mathf.Abs(to.y) < 1.5f)
                    {
                        steerPoint = p.transform.position;
                        break;
                    }
                }
            }

            Vector3 dir = Shapes.Flat(steerPoint - pos);
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : fwd;
            motor.MoveInput = dir * Mathf.Lerp(0.82f, 1f, skill);

            // Jump gaps and walls
            if (motor.Grounded)
            {
                bool gapAhead = !Physics.Raycast(pos + Vector3.up * 0.5f + dir * 0.9f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
                bool wallAhead = Physics.Raycast(pos + Vector3.up * 0.4f, dir, 1.1f, ~0, QueryTriggerInteraction.Ignore);
                if (gapAhead || wallAhead) motor.QueueJump();
            }
            else if (motor.Stunned && Random.value < dt * 6f * skill)
            {
                motor.QueueJump(); // mash to recover
            }

            // Unstick
            if (Shapes.Flat(pos - lastPos).magnitude < 0.5f) stuckTimer += dt;
            else { stuckTimer = 0f; lastPos = pos; }
            if (stuckTimer > 1.2f)
            {
                motor.QueueJump();
                lateral = Random.Range(-1.5f, 1.5f);
                stuckTimer = 0f;
                lastPos = pos;
            }

            Fight(dt, dir);
        }

        static bool Reached(Vector3 pos, Vector3 wp) =>
            pos.z > wp.z - 0.5f || Shapes.Flat(wp - pos).magnitude < 2.5f;

        void Fight(float dt, Vector3 moveDir)
        {
            var holder = me.Weapons;
            fireTimer -= dt;
            shoveTimer -= dt;
            retargetTimer -= dt;

            if (retargetTimer <= 0f)
            {
                target = FindTarget(holder.Held != null && holder.Held.kind == WeaponKind.Rocket ? 22f : 16f);
                retargetTimer = 0.4f;
            }

            // Face the target while holding a gun, otherwise face where we run
            me.Motor.FacingInput = (holder.Held != null && target != null)
                ? Shapes.Flat(target.transform.position - transform.position)
                : Vector3.zero;

            if (holder.Held != null && target != null && fireTimer <= 0f && holder.CanFire)
            {
                Vector3 aimAt = holder.Held.kind == WeaponKind.Rocket
                    ? target.transform.position + target.Motor.Velocity * 0.25f   // feet, with a little lead
                    : target.Center;
                float spread = Mathf.Lerp(0.18f, 0.04f, skill);
                Vector3 aim = (aimAt - holder.FirePoint).normalized + Random.insideUnitSphere * spread;
                holder.TryFire(aim);
                fireTimer = holder.Held != null ? holder.Held.cooldown + Random.Range(0.9f, 2.2f) * (1.5f - skill) : 0.5f;
            }

            if (shoveTimer <= 0f)
            {
                shoveTimer = Random.Range(0.4f, 1.0f);
                foreach (var b in DentBody.All)
                {
                    if (b == null || b.gameObject == gameObject) continue;
                    Vector3 to = b.transform.position - transform.position;
                    if (to.magnitude < 1.9f && Vector3.Dot(Shapes.Flat(to).normalized, transform.forward) > 0.3f)
                    {
                        holder.TryShove();
                        shoveTimer = Random.Range(0.8f, 1.6f);
                        break;
                    }
                }
            }
        }

        DentBody FindTarget(float range)
        {
            DentBody best = null;
            float bestDist = range;
            Vector3 from = me.Weapons.FirePoint;
            foreach (var b in DentBody.All)
            {
                if (b == null || b.gameObject == gameObject) continue;
                var c = b.GetComponent<Contestant>();
                if (c != null && c.Finished) continue;
                float d = Vector3.Distance(from, b.Center);
                if (d >= bestDist) continue;
                if (!HasLineOfSight(from, b)) continue;
                best = b;
                bestDist = d;
            }
            return best;
        }

        bool HasLineOfSight(Vector3 from, DentBody b)
        {
            Vector3 to = b.Center - from;
            foreach (var hit in Physics.RaycastAll(from, to.normalized, to.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                var rb = hit.collider.attachedRigidbody;
                if (rb != null && (rb.gameObject == gameObject || rb.gameObject == b.gameObject)) continue;
                if (hit.collider.GetComponentInParent<DentBody>() != null) continue; // other dummies don't block the view
                return false;
            }
            return true;
        }
    }
}
