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
            var white = Mats.Get("Gary_White", new Color(0.96f, 0.96f, 0.93f));
            var grey = Mats.Get("Gary_Grey", new Color(0.55f, 0.58f, 0.63f));
            var orange = Mats.Get("Gary_Orange", new Color(1f, 0.6f, 0.15f));
            var black = Mats.Get("Dummy_Black", Palette.Black);
            var t = go.transform;
            // Body, head, beak, beady eyes and an eyepatch (Gary has seen things)
            LowPoly.Make(LowPoly.Ico(1), "Body", t, Vector3.zero, new Vector3(1.4f, 1.2f, 2.6f), white);
            LowPoly.Make(LowPoly.Ico(1), "Head", t, new Vector3(0f, 0.55f, 1.35f), Vector3.one * 1.05f, white);
            LowPoly.Make(LowPoly.Cone(6), "Beak", t, new Vector3(0f, 0.45f, 2.05f), new Vector3(0.32f, 0.7f, 0.32f), orange, new Vector3(90f, 0f, 0f));
            LowPoly.Make(LowPoly.Ico(0), "EyeL", t, new Vector3(-0.42f, 0.75f, 1.6f), Vector3.one * 0.16f, black);
            Shapes.Make(PrimitiveType.Cube, "Eyepatch", t, new Vector3(0.44f, 0.75f, 1.6f), new Vector3(0.06f, 0.24f, 0.24f), black, false);
            Shapes.Make(PrimitiveType.Cube, "Strap", t, new Vector3(0f, 0.92f, 1.4f), new Vector3(1.08f, 0.06f, 0.08f), black, false,
                new Vector3(0f, 0f, -12f));
            LowPoly.Make(LowPoly.Cone(5), "Tail", t, new Vector3(0f, 0.1f, -1.6f), new Vector3(0.9f, 0.9f, 0.25f), grey, new Vector3(-90f, 0f, 0f));
            LowPoly.Make(LowPoly.Cone(4), "FootL", t, new Vector3(-0.3f, -0.65f, -0.5f), new Vector3(0.25f, 0.5f, 0.12f), orange, new Vector3(-100f, 0f, 0f));
            LowPoly.Make(LowPoly.Cone(4), "FootR", t, new Vector3(0.3f, -0.65f, -0.5f), new Vector3(0.25f, 0.5f, 0.12f), orange, new Vector3(-100f, 0f, 0f));
            t.localScale = Vector3.one * 1.6f;
            var f = go.AddComponent<FlyBy>();
            f.wingL = Wing(t, -1f, white, grey);
            f.wingR = Wing(t, 1f, white, grey);
            f.from = near + new Vector3(-60f, 22f, -20f);
            f.to = near + new Vector3(60f, 22f, 20f);
            go.transform.position = f.from;
            go.transform.rotation = Quaternion.LookRotation(f.to - f.from);
        }

        static Transform Wing(Transform body, float side, Material white, Material grey)
        {
            var pivot = new GameObject(side < 0f ? "WingL" : "WingR").transform;
            pivot.SetParent(body, false);
            pivot.localPosition = new Vector3(0.55f * side, 0.3f, 0.1f);
            Shapes.Make(PrimitiveType.Cube, "Inner", pivot, new Vector3(1.3f * side, 0f, 0f), new Vector3(2.6f, 0.12f, 1.3f), white, false);
            Shapes.Make(PrimitiveType.Cube, "Tip", pivot, new Vector3(3.2f * side, 0f, -0.2f), new Vector3(1.4f, 0.1f, 0.9f), grey, false,
                new Vector3(0f, -15f * side, 0f));
            return pivot;
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / Duration;
            transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(age * 3f) * 1.5f;
            float flap = Mathf.Sin(age * 14f) * 30f;
            if (wingL != null) wingL.localRotation = Quaternion.Euler(0f, 0f, -flap);
            if (wingR != null) wingR.localRotation = Quaternion.Euler(0f, 0f, flap);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
