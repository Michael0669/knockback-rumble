using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Gary the Shithawk swooping over the arena during a Gary Strike (just for show).</summary>
    public class FlyBy : MonoBehaviour
    {
        Vector3 from, to;
        float age;
        const float Duration = 3.4f;
        Transform wingL, wingR;

        public static void Spawn(Vector3 near)
        {
            var go = new GameObject("Gary");
            var white = Mats.Get("White", Palette.White);
            var beak = Mats.Get("Gold", Palette.Gold);
            Shapes.Make(PrimitiveType.Capsule, "Body", go.transform, Vector3.zero, new Vector3(1.2f, 1.6f, 1.2f), white, false, new Vector3(90f, 0f, 0f));
            Shapes.Make(PrimitiveType.Sphere, "Head", go.transform, new Vector3(0f, 0.4f, 1.5f), Vector3.one * 1f, white, false);
            Shapes.Make(PrimitiveType.Cube, "Beak", go.transform, new Vector3(0f, 0.3f, 2.2f), new Vector3(0.3f, 0.25f, 0.7f), beak, false);
            var f = go.AddComponent<FlyBy>();
            f.wingL = Shapes.Make(PrimitiveType.Cube, "WingL", go.transform, new Vector3(-2f, 0.2f, 0f), new Vector3(3.5f, 0.15f, 1.2f), white, false).transform;
            f.wingR = Shapes.Make(PrimitiveType.Cube, "WingR", go.transform, new Vector3(2f, 0.2f, 0f), new Vector3(3.5f, 0.15f, 1.2f), white, false).transform;
            f.from = near + new Vector3(-60f, 22f, -20f);
            f.to = near + new Vector3(60f, 22f, 20f);
            go.transform.position = f.from;
            go.transform.rotation = Quaternion.LookRotation(f.to - f.from);
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / Duration;
            transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(age * 3f) * 1.5f;
            float flap = Mathf.Sin(age * 14f) * 30f;
            if (wingL != null) wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
            if (wingR != null) wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
