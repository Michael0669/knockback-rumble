using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Prototype HUD drawn with IMGUI (no Canvas setup needed): timer, qualify count,
    /// Dent meter, weapon/ammo, crosshair, announcer subtitles, name tags with
    /// speech bubbles, and the results screen. Replaced by real UI in the art pass.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        const float RefHeight = 1080f;
        GUIStyle big, mid, small, center;
        Texture2D white;
        float helpUntil;

        void Start() => helpUntil = Time.time + 12f;

        void EnsureStyles()
        {
            if (big != null) return;
            white = Texture2D.whiteTexture;
            big = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            mid = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.UpperLeft };
            center = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }

        void OnGUI()
        {
            var race = RaceManager.Instance;
            if (race == null) return;
            EnsureStyles();

            float scale = Screen.height / RefHeight;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float W = Screen.width / scale;
            float H = RefHeight;

            DrawNameTags(race, scale);
            DrawTopBar(race, W);
            DrawAnnouncer(W);
            DrawPlayerPanel(race, W, H);
            DrawToast(W, H);
            if (Cursor.lockState == CursorLockMode.Locked && race.State != RaceManager.Phase.Results) DrawCrosshair(W, H);
            if (race.State == RaceManager.Phase.Countdown) DrawCountdown(race, W, H);
            if (race.State == RaceManager.Phase.Results) DrawResults(race, W, H);
            DrawHelp();
        }

        // ---------- Pieces ----------

        void DrawTopBar(RaceManager race, float W)
        {
            string course = race.Course != null ? race.Course.courseName.ToUpperInvariant() : "RACE";
            int secs = Mathf.FloorToInt(race.State == RaceManager.Phase.Countdown ? 0f : race.Clock);
            string clock = (secs / 60) + ":" + (secs % 60).ToString("00");
            Label(new Rect(W / 2f - 300f, 10f, 600f, 40f), course + "   " + clock, mid, Color.white);
            int q = Mathf.Min(race.Finishers.Count, race.QualifySlots);
            Label(new Rect(W / 2f - 300f, 48f, 600f, 34f), "QUALIFIED  " + q + " / " + race.QualifySlots, mid, Palette.Gold);
        }

        void DrawAnnouncer(float W)
        {
            if (Announcer.Line == null || Time.time - Announcer.LineTime > 3.8f) return;
            string text = Announcer.Speaker + ":  " + Announcer.Line;
            var r = new Rect(W / 2f - 520f, 92f, 1040f, 70f);
            Box(r, new Color(0f, 0f, 0f, 0.55f));
            Label(r, text, center, Announcer.Speaker == Announcer.Trina ? new Color(1f, 0.6f, 0.8f) : new Color(1f, 0.85f, 0.4f));
        }

        void DrawPlayerPanel(RaceManager race, float W, float H)
        {
            var p = race.Player;
            if (p == null) return;

            // Dent meter, bottom left
            float dent = p.Dent.Dent;
            Color dc = Color.Lerp(Color.white, Color.yellow, Mathf.Clamp01(dent / 60f));
            dc = Color.Lerp(dc, Palette.Hazard, Mathf.Clamp01((dent - 60f) / 90f));
            Box(new Rect(30f, H - 150f, 260f, 120f), new Color(0f, 0f, 0f, 0.5f));
            Label(new Rect(30f, H - 150f, 260f, 30f), "DENT", mid, Color.white);
            Label(new Rect(30f, H - 120f, 260f, 80f), Mathf.RoundToInt(dent) + "%", big, dc);

            // Weapon, bottom right
            var w = p.Weapons;
            Box(new Rect(W - 330f, H - 150f, 300f, 120f), new Color(0f, 0f, 0f, 0.5f));
            if (w.Held != null)
            {
                Label(new Rect(W - 330f, H - 145f, 300f, 40f), w.Held.name.ToUpperInvariant(), mid, w.Held.color);
                Label(new Rect(W - 330f, H - 105f, 300f, 40f), "AMMO  " + w.Ammo, mid, Color.white);
            }
            else
            {
                Label(new Rect(W - 330f, H - 145f, 300f, 40f), "NO WEAPON", mid, new Color(1f, 1f, 1f, 0.6f));
                Label(new Rect(W - 330f, H - 105f, 300f, 40f), "grab a crate", mid, new Color(1f, 1f, 1f, 0.6f));
            }
            string shove = w.ShoveCooldown > 0f ? "SHOVE  ..." : "SHOVE  READY";
            Label(new Rect(W - 330f, H - 68f, 300f, 34f), shove, mid, w.ShoveCooldown > 0f ? Color.gray : Palette.Gold);
        }

        void DrawCrosshair(float W, float H)
        {
            Box(new Rect(W / 2f - 1.5f, H / 2f - 10f, 3f, 20f), Color.white);
            Box(new Rect(W / 2f - 10f, H / 2f - 1.5f, 20f, 3f), Color.white);
        }

        void DrawToast(float W, float H)
        {
            if (Announcer.Toast == null || Time.time - Announcer.ToastTime > 2f) return;
            Label(new Rect(W / 2f - 400f, H * 0.62f, 800f, 60f), Announcer.Toast, mid, Palette.Gold);
        }

        void DrawCountdown(RaceManager race, float W, float H)
        {
            int n = Mathf.CeilToInt(race.Clock);
            string text = n > 3 ? "GET READY" : n.ToString();
            Label(new Rect(W / 2f - 300f, H * 0.35f, 600f, 120f), text, big, Palette.Gold);
        }

        void DrawResults(RaceManager race, float W, float H)
        {
            var r = new Rect(W / 2f - 330f, H * 0.2f, 660f, 120f + race.Results.Count * 38f);
            Box(r, new Color(0f, 0f, 0f, 0.75f));
            bool youQ = race.IsQualified(race.Player);
            Label(new Rect(r.x, r.y + 10f, r.width, 60f), youQ ? "QUALIFIED!" : "ELIMINATED", big, youQ ? Palette.Gold : Palette.Hazard);

            float y = r.y + 80f;
            for (int i = 0; i < race.Results.Count; i++)
            {
                var c = race.Results[i];
                bool q = race.IsQualified(c);
                string line = (i + 1) + ".  " + c.DisplayName +
                              "    " + (q ? "QUALIFIED" : "OUT") +
                              "    knock-offs " + c.Knockoffs + "   falls " + c.Falls;
                var col = c.IsPlayer ? Palette.Gold : (q ? Color.white : new Color(1f, 1f, 1f, 0.5f));
                Label(new Rect(r.x + 20f, y, r.width - 40f, 34f), line, center, col);
                y += 38f;
            }
            Label(new Rect(r.x, y + 4f, r.width, 34f), "Press R (or Start) to run it back", center, Palette.Gold);
        }

        void DrawNameTags(RaceManager race, float scale)
        {
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var c in race.Contestants)
            {
                if (c == null || c.IsPlayer) continue;
                Vector3 sp = cam.WorldToScreenPoint(c.HeadPosition);
                if (sp.z <= 0f || sp.z > 45f) continue;
                float x = sp.x / scale;
                float y = (Screen.height - sp.y) / scale;
                string tag = c.DisplayName + "  " + Mathf.RoundToInt(c.Dent.Dent) + "%";
                Label(new Rect(x - 120f, y - 24f, 240f, 24f), tag, center, Color.white);

                if (c.BarkText != null && Time.time - c.BarkTime < 2.5f)
                {
                    var br = new Rect(x - 150f, y - 82f, 300f, 54f);
                    Box(br, new Color(1f, 1f, 1f, 0.92f));
                    Label(br, c.BarkText, center, Color.black);
                }
            }
        }

        void DrawHelp()
        {
            if (Time.time > helpUntil) return;
            const string help =
                "WASD move   SPACE jump (mash to recover)   CTRL/C dive\n" +
                "LMB fire   E / F / RMB shove   ESC free mouse\n" +
                "Fire a rocket at your feet + jump = rocket jump";
            Box(new Rect(20f, 20f, 560f, 84f), new Color(0f, 0f, 0f, 0.5f));
            var s = new GUIStyle(small) { fontSize = 18 };
            s.normal.textColor = Color.white;
            GUI.Label(new Rect(30f, 26f, 550f, 80f), help, s);
        }

        // ---------- Helpers ----------

        void Label(Rect r, string text, GUIStyle style, Color color)
        {
            var sr = new Rect(r.x + 2f, r.y + 2f, r.width, r.height);
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.7f * color.a);
            GUI.Label(sr, text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        void Box(Rect r, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }
    }
}
