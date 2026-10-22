using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace KnockbackRumble
{
    /// <summary>
    /// A rigged character model from the downloaded pack (Characters/Humans_Master: 12 people on one
    /// skeleton, each made of 4 parts). Shows one character's parts, tints their main clothing in the
    /// player's color, scales it to dummy size and plays its animations (idle, run, shoot, punch,
    /// hit...) by blending clips with a small Playables graph. DummyVisual drives it every frame.
    /// </summary>
    public class ModelCharacter : MonoBehaviour
    {
        public const string PrefabPath = "Characters/Humans_Master";
        const float TargetHeight = 1.85f;

        enum Anim { Idle, IdleGun, Run, RunGun, Air, Hit, Punch, Wave, Count }

        static readonly string[] ClipNames = { "Idle", "Idle_Gun_Pointing", "Run", "Run_Shoot", "Run", "HitRecieve", "Punch_Right", "Wave" };
        static readonly bool[] Loops = { true, true, true, true, true, true, false, true };

        /// <summary>Where the gun goes: follows the right wrist, pointing where the dummy faces.</summary>
        public Transform GunHolder { get; private set; }

        Transform wrist, rootT;
        Animator animator;
        PlayableGraph graph;
        bool graphOk;
        AnimationMixerPlayable mixer;
        readonly AnimationClipPlayable[] clips = new AnimationClipPlayable[(int)Anim.Count];
        readonly float[] lengths = new float[(int)Anim.Count];
        readonly float[] times = new float[(int)Anim.Count];
        readonly float[] weights = new float[(int)Anim.Count];
        readonly bool[] has = new bool[(int)Anim.Count];
        float punchTimer;

        public static bool Available => Art.Prefab(PrefabPath) != null;

        /// <summary>Build the model under parent (the dummy's tumble pivot, 0.9 m above the feet). Null if it can't.</summary>
        public static ModelCharacter Create(Transform parent, CharacterDef def, Color playerColor, float pivotHeight)
        {
            if (def == null || def.parts == null || def.parts.Length == 0) return null;
            var prefab = Art.Prefab(PrefabPath);
            if (prefab == null) return null;

            var holder = new GameObject("Character_" + def.name);
            holder.transform.SetParent(parent, false);
            var mc = holder.AddComponent<ModelCharacter>();
            var inst = Instantiate(prefab, holder.transform, false);
            inst.name = "Model";
            mc.rootT = inst.transform;
            foreach (var col in inst.GetComponentsInChildren<Collider>(true)) Destroy(col);

            // Only this character's parts; everything else (other people, the sword, the backpack) off.
            var wanted = new HashSet<string>(def.parts);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                bool on = wanted.Contains(r.gameObject.name);
                r.gameObject.SetActive(on);
                if (!on) continue;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                // The player's color on the main piece of clothing.
                if (!string.IsNullOrEmpty(def.tint))
                {
                    var mats = r.materials; // instanced copies, just for this dummy
                    for (int i = 0; i < mats.Length; i++)
                        if (mats[i] != null && mats[i].name.Replace(" (Instance)", "").Trim() == def.tint) Tint(mats[i], playerColor);
                    r.materials = mats;
                }
            }

            // Scale to dummy height, feet on the ground.
            var b = Art.LocalBounds(holder.transform);
            float h = b.size.y;
            float s = h > 0.01f ? TargetHeight / h : 1f;
            inst.transform.localScale *= s;
            holder.transform.localPosition = new Vector3(0f, -pivotHeight - b.min.y * s, 0f);
            if (def.yaw != 0f) holder.transform.localRotation = Quaternion.Euler(0f, def.yaw, 0f);

            mc.wrist = FindDeep(inst.transform, "Wrist.R");
            var gh = new GameObject("GunHolder").transform;
            gh.SetParent(holder.transform, false);
            mc.GunHolder = gh;
            mc.SetupAnimation(inst);
            return mc;
        }

        static void Tint(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var f = FindDeep(t.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }

        static AnimationClip[] allClips;

        static AnimationClip FindClip(string name)
        {
            if (allClips == null) allClips = Resources.LoadAll<AnimationClip>(PrefabPath);
            foreach (var c in allClips)
            {
                if (c == null) continue;
                string n = c.name;
                int bar = n.LastIndexOf('|');
                if (bar >= 0) n = n.Substring(bar + 1);
                if (n == name) return c;
            }
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => allClips = null;

        void SetupAnimation(GameObject inst)
        {
            animator = inst.GetComponent<Animator>();
            if (animator == null) animator = inst.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = null;

            graph = PlayableGraph.Create("Character");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(graph, "Anim", animator);
            mixer = AnimationMixerPlayable.Create(graph, (int)Anim.Count);
            output.SetSourcePlayable(mixer);
            int found = 0;
            for (int i = 0; i < (int)Anim.Count; i++)
            {
                var clip = FindClip(ClipNames[i]);
                if (clip == null) continue;
                clips[i] = AnimationClipPlayable.Create(graph, clip);
                clips[i].SetApplyFootIK(false);
                graph.Connect(clips[i], 0, mixer, i);
                lengths[i] = Mathf.Max(0.05f, clip.length);
                has[i] = true;
                found++;
            }
            graphOk = found > 0;
            if (!graphOk) Debug.LogWarning("[Knockback Rumble] No animations found in Resources/" + PrefabPath);
            weights[(int)Anim.Idle] = 1f;
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        public void Punch()
        {
            punchTimer = has[(int)Anim.Punch] ? Mathf.Min(0.45f, lengths[(int)Anim.Punch]) : 0f;
            times[(int)Anim.Punch] = 0f;
        }

        /// <summary>Pick and play the right animation for what the dummy is doing. Call once a frame.</summary>
        public void Drive(float speed, bool grounded, bool stunned, bool diving, bool holdingGun, bool celebrating, float dt)
        {
            if (!graphOk) return;
            Anim want;
            punchTimer -= dt;
            if (stunned) want = Anim.Hit;
            else if (punchTimer > 0f) want = Anim.Punch;
            else if (celebrating && speed < 0.5f) want = Anim.Wave;
            else if (!grounded || diving) want = Anim.Air;
            else if (speed > 0.6f) want = holdingGun ? Anim.RunGun : Anim.Run;
            else want = holdingGun ? Anim.IdleGun : Anim.Idle;
            if (!has[(int)want]) want = has[(int)Anim.Idle] ? Anim.Idle : want;

            float blend = dt / 0.12f;
            for (int i = 0; i < (int)Anim.Count; i++)
            {
                if (!has[i]) continue;
                weights[i] = Mathf.MoveTowards(weights[i], i == (int)want ? 1f : 0f, blend);
                float rate = 1f;
                if (i == (int)Anim.Run || i == (int)Anim.RunGun) rate = Mathf.Clamp(speed / 6f, 0.6f, 1.6f);
                if (i == (int)Anim.Air) rate = 0.15f;
                times[i] += dt * rate;
                if (Loops[i]) times[i] = Mathf.Repeat(times[i], lengths[i]);
                else times[i] = Mathf.Min(times[i], lengths[i]);
                clips[i].SetTime(times[i]);
                mixer.SetInputWeight(i, weights[i]);
            }
            graph.Evaluate(0f);
        }

        /// <summary>After the animation: put the gun in the right hand, pointing the way the dummy faces.</summary>
        public void PlaceGun(Quaternion facing)
        {
            if (GunHolder == null) return;
            Vector3 at = wrist != null ? wrist.position : transform.position + Vector3.up * 1.3f + facing * Vector3.forward * 0.4f;
            GunHolder.SetPositionAndRotation(at, facing);
        }
    }
}
