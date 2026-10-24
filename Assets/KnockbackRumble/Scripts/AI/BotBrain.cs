using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Bot for every event. "skill" (0..1) scales speed, aim and reaction time.
    ///  - The Gauntlet: follows the course waypoints, jumps gaps and walls.
    ///  - Cash Grab: goes for loose money bags, hunts whoever carries one, runs away when it has one.
    ///  - Crumble Plaza: stays near the middle on solid floor and shoves people toward the holes.
    /// In every event it shoots whoever is in range (with rules per weapon), shoves anyone
    /// close, throws its gadget now and then, refills ammo at crates and calls in its streaks.
    /// </summary>
    public class BotBrain : MonoBehaviour
    {
        Contestant me;
        CourseInfo course;
        float skill;
        int waypoint;
        float lateral;
        float fireTimer, shoveTimer, stuckTimer, retargetTimer, gadgetTimer, streakTimer, wanderTimer;
        Vector3 lastPos, wanderPoint;
        DentBody target;
        MiniGame mini;
        bool raceLike;

        bool Arena => course != null && !raceLike;

        public void Init(Contestant contestant, CourseInfo courseInfo, float skillLevel)
        {
            me = contestant;
            course = courseInfo;
            mini = courseInfo != null ? MiniGames.Get(courseInfo.kind) : null;
            raceLike = courseInfo != null && RaceManager.IsRaceKind(courseInfo.kind);
            skill = Mathf.Clamp01(skillLevel);
            lateral = Random.Range(-1.5f, 1.5f);
            fireTimer = Random.Range(1f, 2.5f);
            gadgetTimer = Random.Range(4f, 9f);
            ResetPath();
        }

        /// <summary>Pick the waypoint nearest ahead of us (after respawns or restarts).</summary>
        public void ResetPath()
        {
            waypoint = 0;
            lastPos = transform.position;
            stuckTimer = 0f;
            wanderTimer = 0f;
            if (course == null || course.waypoints == null) return;
            while (waypoint < course.waypoints.Length && course.waypoints[waypoint].z < transform.position.z - 1f)
                waypoint++;
        }

        void Update()
        {
            if (me == null || course == null) return;
            float dt = Time.deltaTime;
            var motor = me.Motor;
            Vector3 pos = transform.position;

            if (me.Finished || me.Eliminated) MaybeHeckle(dt);
            if (me.Finished)
            {
                // Walk on past the line to a spot on the finish pad, then celebrate.
                Vector3 to = Shapes.Flat(course.FinishParkPoint(me) - pos);
                motor.FacingInput = Vector3.zero;
                if (to.magnitude > 1f) { motor.MoveInput = to.normalized * 0.75f; return; }
                motor.MoveInput = Vector3.zero;
                if (motor.Grounded && Random.value < dt * 0.8f) motor.QueueJump();
                return;
            }
            if (me.Eliminated)
            {
                // In the Losers' Lounge: hop around.
                motor.MoveInput = Vector3.zero;
                motor.FacingInput = Vector3.zero;
                if (motor.Grounded && Random.value < dt * 0.8f) motor.QueueJump();
                return;
            }

            // Mini-games can tell bots to freeze (red light!).
            if (mini != null && !motor.InputLocked && mini.BotHold(me, skill))
            {
                motor.MoveInput = Vector3.zero;
                motor.FacingInput = Vector3.zero;
                if (motor.Stunned && Random.value < dt * 6f * skill) motor.QueueJump();
                lastPos = pos;
                stuckTimer = 0f;
                return;
            }

            Vector3 steerPoint = Arena ? ArenaGoal(pos, dt) : GauntletGoal(pos);

            // Out of ammo? Detour to a nearby crate.
            if (me.Weapons.Held != null && me.Weapons.Ammo == 0)
            {
                float reach = Arena ? 22f : 9f;
                foreach (var p in WeaponPickup.All)
                {
                    if (!p.Available) continue;
                    Vector3 to = p.transform.position - pos;
                    if (Mathf.Abs(to.y) > 1.5f || Shapes.Flat(to).magnitude > reach) continue;
                    if (!Arena && (to.z < -1f || to.z > 9f)) continue;
                    steerPoint = p.transform.position;
                    break;
                }
            }

            Vector3 dir = Shapes.Flat(steerPoint - pos);
            Vector3 fallback = Arena ? Shapes.Flat(course.center - pos) : Vector3.forward;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : (fallback.sqrMagnitude > 0.01f ? fallback.normalized : Vector3.forward);
            float speed = Mathf.Lerp(0.82f, 1f, skill);
            if (Arena && Shapes.Flat(steerPoint - pos).magnitude < 1.2f) speed = 0f;

            if (motor.Grounded)
            {
                bool gapAhead = !Physics.Raycast(pos + Vector3.up * 0.5f + dir * 0.9f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
                bool wallAhead = Physics.Raycast(pos + Vector3.up * 0.4f, dir, 1.1f, ~0, QueryTriggerInteraction.Ignore);
                if (Arena && gapAhead && speed > 0f)
                {
                    // Only jump a hole if there's floor on the other side; otherwise turn back toward the middle.
                    bool landing = Physics.Raycast(pos + Vector3.up * 0.5f + dir * 3.6f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
                    if (landing) motor.QueueJump();
                    else
                    {
                        dir = fallback.sqrMagnitude > 0.01f ? fallback.normalized : -dir;
                        wanderTimer = 0f;
                    }
                }
                else if (gapAhead || wallAhead) motor.QueueJump();
            }
            else if (motor.Stunned && Random.value < dt * 6f * skill)
            {
                motor.QueueJump(); // mash to recover
            }
            motor.MoveInput = dir * speed;
            if (mini != null && !motor.InputLocked) mini.BotExtra(me, skill, dt);

            // Unstick
            if (speed > 0f && Shapes.Flat(pos - lastPos).magnitude < 0.5f) stuckTimer += dt;
            else { stuckTimer = 0f; lastPos = pos; }
            if (stuckTimer > 1.2f)
            {
                motor.QueueJump();
                lateral = Random.Range(-1.5f, 1.5f);
                wanderTimer = 0f;
                stuckTimer = 0f;
                lastPos = pos;
            }

            Fight(dt);
        }

        float heckleTimer = -1f;

        /// <summary>Done for the event: now and then, heckle someone still playing.</summary>
        void MaybeHeckle(float dt)
        {
            if (heckleTimer < 0f) heckleTimer = Random.Range(5f, 10f);
            heckleTimer -= dt;
            if (heckleTimer > 0f) return;
            heckleTimer = Random.Range(8f, 16f);
            var race = RaceManager.Instance;
            if (race == null || race.State != RaceManager.Phase.Racing) return;
            var playing = new System.Collections.Generic.List<Contestant>();
            foreach (var c in race.Contestants)
                if (c != null && c != me && !c.Finished && !c.Eliminated) playing.Add(c);
            if (playing.Count == 0) return;
            Heckles.Send(me, playing[Random.Range(0, playing.Count)], (HeckleKind)Random.Range(1, 5));
        }

        // ---------- Where to go ----------

        Vector3 GauntletGoal(Vector3 pos)
        {
            if (course.waypoints == null || course.waypoints.Length == 0) return pos + Vector3.forward * 10f;

            // Respawned behind our current waypoint? Re-find the path.
            if (waypoint > 0 && pos.z < course.waypoints[waypoint - 1].z - 6f) ResetPath();
            while (waypoint < course.waypoints.Length && Reached(pos, course.waypoints[waypoint])) waypoint++;
            Vector3 goal = waypoint < course.waypoints.Length ? course.waypoints[waypoint] : pos + Vector3.forward * 10f;

            Vector3 toGoal = Shapes.Flat(goal - pos);
            Vector3 fwd = toGoal.sqrMagnitude > 0.01f ? toGoal.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            return goal + right * lateral;
        }

        static bool Reached(Vector3 pos, Vector3 wp) =>
            pos.z > wp.z - 0.5f || Shapes.Flat(wp - pos).magnitude < 2.5f;

        Vector3 ArenaGoal(Vector3 pos, float dt)
        {
            var show = ShowManager.Instance;
            if (mini != null)
            {
                var goal = mini.BotGoal(me, pos, skill);
                if (goal.HasValue) return goal.Value;
            }

            if (course.kind == EventKind.CashGrab && show != null)
            {
                var mine = show.BagOf(me);
                if (mine != null)
                {
                    // Keep away from the nearest threat, but don't run off the edge.
                    var threat = Nearest(pos, 14f);
                    Vector3 away = threat != null ? Shapes.Flat(pos - threat.transform.position).normalized * 6f : Vector3.zero;
                    Vector3 goal = pos + away;
                    return ClampToArena(goal, 0.65f);
                }

                // Nearest loose bag, else the nearest carrier.
                Vector3? best = null;
                float bestDist = float.MaxValue;
                foreach (var b in show.Bags)
                {
                    if (b == null) continue;
                    if (b.Carried && b.Carrier == me) continue;
                    float d = Shapes.Flat(b.transform.position - pos).magnitude * (b.Carried ? 1.4f : 1f);
                    if (d < bestDist) { bestDist = d; best = b.Carried && b.Carrier != null ? b.Carrier.transform.position : b.transform.position; }
                }
                if (best.HasValue) return best.Value;
            }

            // Crumble (or nothing to chase): wander around the middle.
            wanderTimer -= dt;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(1.5f, 3.5f);
                float r = course.radius * (course.kind == EventKind.Crumble ? 0.35f : 0.5f);
                Vector2 c = Random.insideUnitCircle * r;
                wanderPoint = course.center + new Vector3(c.x, 0f, c.y);

                // Crumble: someone close? Go shove them.
                var victim = Nearest(pos, 6f);
                if (victim != null && Random.value < 0.6f) wanderPoint = victim.transform.position;
            }
            return wanderPoint;
        }

        Vector3 ClampToArena(Vector3 p, float share)
        {
            Vector3 off = Shapes.Flat(p - course.center);
            float max = course.radius * share;
            if (off.magnitude > max) off = off.normalized * max;
            return course.center + off;
        }

        Contestant Nearest(Vector3 pos, float range)
        {
            var race = RaceManager.Instance;
            if (race == null) return null;
            Contestant best = null;
            float bestDist = range;
            foreach (var c in race.Contestants)
            {
                if (c == null || c == me || c.Eliminated) continue;
                float d = Vector3.Distance(pos, c.transform.position);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        // ---------- Fighting ----------

        float RangeFor(WeaponDef def)
        {
            if (def == null) return 0f;
            switch (def.kind)
            {
                case WeaponKind.Shotgun: return def.range * 0.85f;
                case WeaponKind.Rocket: return 22f;
                default: return def == WeaponDef.LongShot ? 70f : def == WeaponDef.HandCannon || def == WeaponDef.BonkRifle ? 30f : 18f;
            }
        }

        void Fight(float dt)
        {
            var holder = me.Weapons;
            fireTimer -= dt;
            shoveTimer -= dt;
            retargetTimer -= dt;
            gadgetTimer -= dt;

            if (retargetTimer <= 0f)
            {
                target = FindTarget(Mathf.Max(RangeFor(holder.Held), 6f));
                retargetTimer = 0.4f;
            }

            bool canShoot = holder.Held != null && holder.Ammo > 0;
            // Waiting for GO: stand still facing forward (don't spin around to aim at the dummy behind you).
            if (me.Motor.InputLocked) { me.Motor.FacingInput = Vector3.zero; return; }
            me.Motor.FacingInput = canShoot && target != null ? Shapes.Flat(target.transform.position - transform.position) : Vector3.zero;

            if (canShoot && target != null && fireTimer <= 0f && holder.CanFire &&
                Vector3.Distance(holder.FirePoint, target.Center) <= RangeFor(holder.Held))
            {
                Vector3 aimAt = holder.Held.kind == WeaponKind.Rocket
                    ? target.transform.position + target.Motor.Velocity * 0.25f   // feet, with a little lead
                    : target.Center;
                float spread = holder.Held == WeaponDef.LongShot ? Mathf.Lerp(0.06f, 0.01f, skill) : Mathf.Lerp(0.18f, 0.04f, skill);
                Vector3 aim = (aimAt - holder.FirePoint).normalized + Random.insideUnitSphere * spread;
                holder.TryFire(aim);
                bool hose = holder.Held == WeaponDef.SprayNPray || holder.Held == WeaponDef.LeafBlower;
                fireTimer = hose ? holder.Held.cooldown * 1.5f : holder.Held.cooldown + Random.Range(0.9f, 2.2f) * (1.5f - skill);
                if (holder.Held == WeaponDef.LongShot) fireTimer += 1.5f; // snipers take their time
            }

            // Gadget: lob it at someone mid-range (grenade) or drop it when someone's close (mine).
            if (gadgetTimer <= 0f)
            {
                gadgetTimer = Random.Range(1f, 2f);
                if (holder.CanThrow && target != null && Random.value < 0.35f + 0.3f * skill)
                {
                    float d = Vector3.Distance(transform.position, target.transform.position);
                    var gd = holder.Gadget;
                    bool use = gd == Gadget.StickyMine || gd == Gadget.BananaPeel || gd == Gadget.BouncePad ? d < 7f
                             : gd == Gadget.AirHorn ? d < 5f
                             : d > 5f && d < 15f;
                    if (use)
                    {
                        holder.TryGadget((target.Center - holder.FirePoint).normalized);
                        gadgetTimer = Random.Range(6f, 12f);
                    }
                }
            }

            // Streaks: call them in a moment after earning them.
            if (me.ReadyStreaks.Count > 0)
            {
                streakTimer -= dt;
                if (streakTimer <= 0f)
                {
                    me.CallReadyStreak();
                    streakTimer = Random.Range(1f, 3f);
                }
            }
            else streakTimer = Random.Range(1f, 3f);

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
            float bestScore = float.MaxValue;
            Vector3 from = me.Weapons.FirePoint;
            var show = ShowManager.Instance;
            foreach (var b in DentBody.All)
            {
                if (b == null || b.gameObject == gameObject) continue;
                var c = b.GetComponent<Contestant>();
                if (c != null && (c.Finished || c.Eliminated)) continue;
                float d = Vector3.Distance(from, b.Center);
                if (d >= range) continue;
                // On the race course, only bother with dummies ahead of you (or right next to you).
                if (!Arena && b.transform.position.z < transform.position.z - 2f) continue;
                if (!HasLineOfSight(from, b)) continue;
                // Bag carriers are juicy targets.
                float score = d;
                if (c != null && show != null && show.BagOf(c) != null) score *= 0.5f;
                if (score >= bestScore) continue;
                best = b;
                bestScore = score;
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
