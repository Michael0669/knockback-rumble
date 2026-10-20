using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Low-poly particle effects: every particle is a tiny faceted icosphere (smoke puffs,
    /// fireballs, sparks, confetti). One shared ParticleSystem per look, so thousands of
    /// puffs cost almost nothing. Call from anywhere:
    ///   Puffs.Smoke(pos, vel, size, life)    grey faceted smoke that drifts up
    ///   Puffs.Fire(pos, vel, size, life)     yellow -> orange -> red, fading
    ///   Puffs.Spark(pos, vel, size, life, color)
    ///   Puffs.Confetti(pos, count)
    /// </summary>
    public static class Puffs
    {
        static readonly Dictionary<string, ParticleSystem> systems = new Dictionary<string, ParticleSystem>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => systems.Clear();

        public static void Smoke(Vector3 pos, Vector3 vel, float size, float life, bool dark = false) =>
            Emit(dark ? "smoke_dark" : "smoke", pos, vel, size, life, Color.white);

        public static void Fire(Vector3 pos, Vector3 vel, float size, float life) => Emit("fire", pos, vel, size, life, Color.white);

        public static void Spark(Vector3 pos, Vector3 vel, float size, float life, Color color) => Emit("spark", pos, vel, size, life, color);

        static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.3f, 0.4f), new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.85f, 1f),
            new Color(0.5f, 1f, 0.4f), new Color(0.9f, 0.4f, 1f), Color.white,
        };

        public static void Confetti(Vector3 pos, int count, float speed = 9f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 v = Random.onUnitSphere;
                v.y = Mathf.Abs(v.y) + 0.6f;
                Emit("confetti", pos, v.normalized * speed * Random.Range(0.5f, 1.1f), Random.Range(0.12f, 0.22f),
                    Random.Range(1.6f, 2.6f), ConfettiColors[Random.Range(0, ConfettiColors.Length)]);
            }
        }

        /// <summary>A puff of smoke clouds in every direction (explosions, landings, dust).</summary>
        public static void SmokeBurst(Vector3 pos, int count, float spread, float size, float life, bool dark = false)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 v = Random.insideUnitSphere * spread;
                v.y = Mathf.Abs(v.y) * 0.6f + spread * 0.2f;
                Smoke(pos + Random.insideUnitSphere * size * 0.3f, v, size * Random.Range(0.7f, 1.3f), life * Random.Range(0.75f, 1.25f), dark);
            }
        }

        static void Emit(string kind, Vector3 pos, Vector3 vel, float size, float life, Color color)
        {
            var ps = Get(kind);
            if (ps == null) return;
            var p = new ParticleSystem.EmitParams
            {
                position = pos,
                velocity = vel,
                startSize = size,
                startLifetime = life,
                startColor = color,
                rotation3D = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)),
                applyShapeToPosition = false,
            };
            ps.Emit(p, 1);
        }

        static ParticleSystem Get(string kind)
        {
            if (systems.TryGetValue(kind, out var ps) && ps != null) return ps;
            if (!Application.isPlaying) return null;

            var go = new GameObject("Puffs_" + kind);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;
            main.startSpeed = 0f;
            main.startLifetime = 1f;
            main.startSize = 1f;
            main.startRotation3D = true;

            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            var curve = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f));

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.separateAxes = true;
            rot.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
            rot.y = new ParticleSystem.MinMaxCurve(-2f, 2f);
            rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = new ParticleSystem.MinMaxCurve(500f);
            limit.dampen = 0f;

            Material mat;
            switch (kind)
            {
                case "smoke":
                    main.gravityModifier = -0.05f;
                    limit.drag = new ParticleSystem.MinMaxCurve(2.5f);
                    curve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f));
                    mat = Mats.Get("Fx_SmokeLight", new Color(0.78f, 0.77f, 0.75f));
                    break;
                case "smoke_dark":
                    main.gravityModifier = -0.06f;
                    limit.drag = new ParticleSystem.MinMaxCurve(2.5f);
                    curve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f));
                    mat = Mats.Get("Fx_SmokeDark", new Color(0.22f, 0.21f, 0.2f));
                    break;
                case "fire":
                {
                    main.gravityModifier = -0.15f;
                    limit.drag = new ParticleSystem.MinMaxCurve(4f);
                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    var g = new Gradient();
                    g.SetKeys(
                        new[] { new GradientColorKey(new Color(1f, 0.97f, 0.7f), 0f), new GradientColorKey(new Color(1f, 0.65f, 0.1f), 0.3f),
                                new GradientColorKey(new Color(0.85f, 0.2f, 0.05f), 0.7f), new GradientColorKey(new Color(0.25f, 0.1f, 0.05f), 1f) },
                        new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
                    col.color = new ParticleSystem.MinMaxGradient(g);
                    mat = Mats.Unlit("FxGlow", Color.white);
                    break;
                }
                case "confetti":
                {
                    main.gravityModifier = 0.35f;
                    limit.drag = new ParticleSystem.MinMaxCurve(1.5f);
                    curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f));
                    rot.x = new ParticleSystem.MinMaxCurve(-8f, 8f);
                    rot.y = new ParticleSystem.MinMaxCurve(-8f, 8f);
                    mat = Mats.Unlit("FxGlow", Color.white);
                    break;
                }
                default: // spark
                {
                    main.gravityModifier = 1.2f;
                    limit.drag = new ParticleSystem.MinMaxCurve(0.6f);
                    curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
                    mat = Mats.Unlit("FxGlow", Color.white);
                    break;
                }
            }
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = LowPoly.Ico(0);
            r.alignment = ParticleSystemRenderSpace.World;
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            ps.Play();
            systems[kind] = ps;
            return ps;
        }
    }
}
