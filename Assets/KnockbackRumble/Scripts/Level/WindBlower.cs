using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Giant fans that blow dummies sideways across a bridge, in gusts (shared clock).
    /// Anyone standing inside the box gets pushed; little wind streaks show the gusts.
    /// </summary>
    public class WindBlower : MonoBehaviour
    {
        public Vector3 size = new Vector3(10f, 6f, 30f);
        public Vector3 wind = new Vector3(5f, 0f, 0f);
        public float gustPeriod = 4f;
        [Range(0f, 1f)] public float calm = 0.25f;
        public Transform[] fans;

        float Strength
        {
            get
            {
                float s = 0.5f + 0.5f * Mathf.Sin(Net.Time / gustPeriod * Mathf.PI * 2f);
                return Mathf.Lerp(calm, 1f, s * s);
            }
        }

        void FixedUpdate()
        {
            Vector3 push = wind * Strength;
            Vector3 half = size * 0.5f;
            foreach (var body in DentBody.All)
            {
                if (body == null || !body.IsLocal) continue;
                Vector3 local = transform.InverseTransformPoint(body.transform.position);
                if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y || Mathf.Abs(local.z) > half.z) continue;
                body.Motor.AddConveyor(push);
            }
        }

        void Update()
        {
            float s = Strength;
            if (fans != null)
                foreach (var f in fans)
                    if (f != null) f.Rotate(0f, (200f + 900f * s) * Time.deltaTime, 0f, Space.Self);

            // Wind streaks
            if (Random.value < s * 0.9f)
            {
                Vector3 half = size * 0.5f;
                Vector3 p = transform.TransformPoint(new Vector3(-half.x, Random.Range(0f, half.y), Random.Range(-half.z, half.z)));
                Puffs.Spark(p, wind.normalized * (8f + 10f * s) + Vector3.up * 1.5f, 0.07f, 0.9f,
                    new Color(1f, 1f, 1f));
            }
        }
    }
}
