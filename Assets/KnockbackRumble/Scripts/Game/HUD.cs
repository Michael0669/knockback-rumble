using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// In-game HUD drawn with IMGUI: event name and timer, your cash with payout popups,
    /// the cash leaderboard, Dent meter, loadout panel (weapon, ammo, gadget, streaks),
    /// crosshair / sniper scope, announcer subtitles, countdown, and the event and show
    /// results screens.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        const float RefHeight = 1080f;
        GUIStyle big, mid, small, center, left, right, huge;
        Texture2D white;

        void EnsureStyles()
        {
            if (big != null) return;
            white = Texture2D.whiteTexture;
            huge = new GUIStyle(GUI.skin.label) { font = UiSkin.Display, fontSize = 96, alignment = TextAnchor.MiddleCenter };
            big = new GUIStyle(GUI.skin.label) { font = UiSkin.Display, fontSize = 64, alignment = TextAnchor.MiddleCenter };
            mid = new GUIStyle(GUI.skin.label) { font = UiSkin.Display, fontSize = 30, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(GUI.skin.label) { font = UiSkin.BodyBold, fontSize = 20, alignment = TextAnchor.UpperLeft };
            center = new GUIStyle(GUI.skin.label) { font = UiSkin.Body, fontSize = 22, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            left = new GUIStyle(GUI.skin.label) { font = UiSkin.Body, fontSize = 22, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            right = new GUIStyle(left) { alignment = TextAnchor.MiddleRight };
        }

        static float hitMarkerTime = -99f, hitMarkerSize;

        /// <summary>Flash the hit marker (you hit someone). Bigger hits = bigger, redder marker.</summary>
        public static void HitMarker(float knock)
        {
            hitMarkerTime = Time.time;
            hitMarkerSize = Mathf.Clamp01(knock / 25f);
        }

        void OnGUI()
        {
            var race = RaceManager.Instance;
            var show = ShowManager.Instance;
            if (race == null || show == null || race.State == RaceManager.Phase.Idle) return;
            UiSkin.Ensure();
            EnsureStyles();

            float scale = Screen.height / RefHeight;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float W = Screen.width / scale;
            float H = RefHeight;
            var p = race.Player;

            // Before each event: a loading screen with the rules, so nobody gets thrown in blind.
            if (race.Loading)
            {
                DrawLoading(race, show, p, W, H);
                return;
            }
            bool results = race.State == RaceManager.Phase.Results;

            var cam = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
            bool scoped = !results && !MenuUI.Paused && p != null && cam != null && cam.Aiming && cam.ZoomAmount > 0.8f &&
                          p.Weapons.Held != null && p.Weapons.Held.zoomFov <= 30f;
            if (scoped) DrawScope(W, H);

            DrawTopBar(race, show, W);
            DrawAnnouncer(W);
            if (!results)
            {
                DrawCash(race, show, p);
                DrawLeaderboard(race, show, W);
                DrawPlayerPanel(race, show, p, W, H);
                DrawToast(W, H);
                if (!MenuUI.Paused && p != null && !p.Eliminated && !scoped) DrawCrosshair(W, H);
                if (!MenuUI.Paused && p != null) DrawHitMarker(W, H);
                if (p != null && p.Eliminated) DrawEliminated(W, H);
            }
            if (race.State == RaceManager.Phase.Countdown) DrawCountdown(race, show, W, H);
            if (results)
            {
                if (show.ShowOver) DrawShowResults(race, show, W, H);
                else DrawRoundResults(race, show, W, H);
            }
        }

        // ---------- Top ----------

        void DrawTopBar(RaceManager race, ShowManager show, float W)
        {
            string title = Events.Title(race.Kind) + "   " + (show.RoundIndex + 1) + "/" + show.RoundCount;
            var bar = new Rect(W / 2f - 280f, 12f, 560f, 52f);
            UiSkin.Pill(bar, UiSkin.PillDark);
            Label(new Rect(bar.x + 24f, bar.y, 380f, bar.height), title, left, UiSkin.Gold, UiSkin.Display, 28);

            float secs = race.State == RaceManager.Phase.Countdown ? race.TimeLimit : race.TimeLeft;
            int s = Mathf.CeilToInt(secs);
            string clock = (s / 60) + ":" + (s % 60).ToString("00");
            Color cc = race.State == RaceManager.Phase.Racing && secs < 10f && Mathf.Repeat(Time.time * 2f, 1f) < 0.5f ? UiSkin.Red : UiSkin.Cream;
            Label(new Rect(bar.xMax - 140f, bar.y, 116f, bar.height), clock, right, cc, UiSkin.Display, 32);

            // Event-specific status under the bar
            string status = null;
            if (race.Kind == EventKind.Gauntlet && race.State == RaceManager.Phase.Racing)
                status = "FINISHED  " + race.Finishers.Count + " / " + race.FinishTarget;
            else if (race.Kind == EventKind.Crumble && race.State == RaceManager.Phase.Racing)
                status = "STILL STANDING  " + race.AliveCount;
            else if (race.Kind == EventKind.CashGrab && race.State == RaceManager.Phase.Racing)
            {
                int loose = 0;
                foreach (var b in show.Bags) if (b != null && !b.Carried) loose++;
                status = loose > 0 ? loose + (loose == 1 ? " BAG" : " BAGS") + " ON THE FLOOR" : "ALL BAGS TAKEN - GO GET 'EM";
            }
            if (status != null) Label(new Rect(W / 2f - 300f, 66f, 600f, 34f), status, mid, UiSkin.Cream, UiSkin.Display, 24);
        }

        void DrawAnnouncer(float W)
        {
            if (Announcer.Line == null || Time.time - Announcer.LineTime > 3.8f) return;
            string text = Announcer.Speaker + ":  " + Announcer.Line;
            var r = new Rect(W / 2f - 520f, 104f, 1040f, 66f);
            UiSkin.Pill(r, UiSkin.PillDark);
            Label(r, text, center, Announcer.Speaker == Announcer.Trina ? new Color(1f, 0.6f, 0.8f) : new Color(1f, 0.85f, 0.4f));
        }

        // ---------- Cash ----------

        void DrawCash(RaceManager race, ShowManager show, Contestant p)
        {
            if (p == null) return;
            var r = new Rect(22f, 16f, 320f, 92f);
            UiSkin.Panel(r);
            Label(new Rect(r.x + 22f, r.y + 6f, 280f, 26f), "SHOW CASH", left, UiSkin.LabelBlue, UiSkin.Display, 20);
            Label(new Rect(r.x + 22f, r.y + 28f, 280f, 60f), Money(show.Cash(p)), left, UiSkin.Gold, UiSkin.Display, 48);

            // Popups slide down and fade under the counter.
            float y = r.yMax + 8f;
            for (int i = show.Popups.Count - 1; i >= 0; i--)
            {
                var pop = show.Popups[i];
                float age = Time.time - pop.time;
                if (age > 2.6f) continue;
                float a = Mathf.Clamp01(2.6f - age);
                float slide = Mathf.Clamp01(age * 8f);
                bool gain = pop.amount > 0;
                var row = new Rect(r.x + 8f - (1f - slide) * 30f, y, 300f, 34f);
                UiSkin.Pill(row, new Color(gain ? 0.12f : 0.35f, gain ? 0.3f : 0.08f, 0.12f, 0.85f * a));
                string text = (gain ? "+" : "-") + Money(Mathf.Abs(pop.amount)) + "  " + ShowManager.ReasonLabel(pop.reason);
                Label(new Rect(row.x + 16f, row.y, row.width - 24f, row.height), text, left,
                    new Color(gain ? 0.6f : 1f, gain ? 1f : 0.5f, gain ? 0.55f : 0.45f, a), UiSkin.Display, 21);
                y += 38f;
            }

            // Holding a bag?
            if (show.BagOf(p) != null)
            {
                var bag = new Rect(22f, Mathf.Max(y, r.yMax + 8f) + 4f, 320f, 40f);
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 6f);
                UiSkin.Pill(bag, new Color(UiSkin.Gold.r * pulse, UiSkin.Gold.g * pulse, UiSkin.Gold.b * pulse));
                Label(bag, "YOU HAVE A BAG  +$" + ShowManager.BagPerSecond + "/s", mid, UiSkin.Ink, UiSkin.Display, 22, false);
            }
        }

        void DrawLeaderboard(RaceManager race, ShowManager show, float W)
        {
            var order = show.Ranking(race.Contestants);
            int rows = Mathf.Min(order.Count, 5);
            int you = race.Player != null ? order.IndexOf(race.Player) : -1;
            bool extra = you >= rows;
            float rowH = 32f;
            float h = 44f + (rows + (extra ? 1 : 0)) * rowH + 10f;
            float top = Net.Online ? 70f : 16f; // leave room for the online strip
            var r = new Rect(W - 330f, top, 310f, h);
            UiSkin.Panel(r);
            Label(new Rect(r.x + 20f, r.y + 8f, 270f, 30f), "RICHEST DUMMIES", left, UiSkin.LabelBlue, UiSkin.Display, 20);
            float y = r.y + 42f;
            for (int i = 0; i < rows; i++) { LeaderRow(order[i], i + 1, r.x, y, show); y += rowH; }
            if (extra) LeaderRow(race.Player, you + 1, r.x, y, show);
        }

        void LeaderRow(Contestant c, int place, float x, float y, ShowManager show)
        {
            if (c == null) return;
            Color col = c.IsPlayer ? UiSkin.Gold : c.Eliminated ? new Color(1f, 1f, 1f, 0.45f) : UiSkin.Cream;
            Box(new Rect(x + 18f, y + 9f, 14f, 14f), c.Color);
            Label(new Rect(x + 40f, y, 30f, 32f), place.ToString(), left, col, UiSkin.Display, 20);
            Label(new Rect(x + 66f, y, 140f, 32f), c.DisplayName, left, col, UiSkin.BodyBold, 19);
            Label(new Rect(x + 190f, y, 100f, 32f), Money(show.Cash(c)), right, col, UiSkin.Display, 20);
        }

        // ---------- Bottom ----------

        void DrawPlayerPanel(RaceManager race, ShowManager show, Contestant p, float W, float H)
        {
            if (p == null) return;

            // Dent meter, bottom left
            float dent = p.Dent.Dent;
            Color dc = Color.Lerp(Color.white, Color.yellow, Mathf.Clamp01(dent / 60f));
            dc = Color.Lerp(dc, Palette.Hazard, Mathf.Clamp01((dent - 60f) / 90f));
            UiSkin.Panel(new Rect(22f, H - 160f, 240f, 140f));
            Label(new Rect(30f, H - 150f, 224f, 30f), "DENT", mid, Color.white);
            Label(new Rect(30f, H - 120f, 224f, 80f), Mathf.RoundToInt(dent) + "%", big, dc);
            Label(new Rect(30f, H - 54f, 224f, 26f), "higher dent = you fly farther", center, UiSkin.Muted, UiSkin.BodyBold, 15, false);

            // Loadout, bottom right
            var w = p.Weapons;
            var r = new Rect(W - 420f, H - 228f, 398f, 208f);
            UiSkin.Panel(r);
            float x = r.x + 22f, y = r.y + 14f;
            if (w.Held != null)
            {
                Label(new Rect(x, y, 260f, 40f), w.Held.name.ToUpperInvariant(), left, w.Held.color, UiSkin.Display, 30);
                Color ammoCol = w.Ammo == 0 ? UiSkin.Red : UiSkin.Cream;
                Label(new Rect(r.xMax - 150f, y, 128f, 40f), w.Ammo + " / " + w.Held.ammo, right, ammoCol, UiSkin.Display, 30);
                // Ammo pips
                int max = Mathf.Max(1, w.Held.ammo);
                float pipW = Mathf.Min(26f, (r.width - 44f) / max - 4f);
                for (int i = 0; i < max; i++)
                    Box(new Rect(x + i * (pipW + 4f), y + 44f, pipW, 10f), i < w.Ammo ? w.Held.color : new Color(1f, 1f, 1f, 0.15f));
                if (w.Ammo == 0) Label(new Rect(x, y + 56f, 354f, 24f), "OUT OF AMMO - find a crate", left, UiSkin.Red, UiSkin.BodyBold, 18);
            }
            y += 84f;

            // Gadget
            var gi = Loadouts.GadgetInfo(w.Gadget);
            if (gi != null)
            {
                Label(new Rect(x, y, 70f, 30f), "[G]", left, UiSkin.Muted, UiSkin.Display, 20);
                Label(new Rect(x + 46f, y, 220f, 30f), gi.name.ToUpperInvariant(), left, gi.color, UiSkin.Display, 22);
                for (int i = 0; i < w.GadgetMax; i++)
                    UiSkin.Image(new Rect(r.xMax - 40f - i * 28f, y + 4f, 22f, 22f), UiSkin.Dot, i < w.GadgetCharges ? gi.color : new Color(1f, 1f, 1f, 0.15f));
            }
            y += 34f;

            // Streak
            if (p.ReadyStreaks.Count > 0)
            {
                var si = Loadouts.StreakInfo(p.ReadyStreaks[0]);
                var pill = new Rect(x - 6f, y, r.width - 32f, 38f);
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 7f);
                UiSkin.Pill(pill, new Color(UiSkin.Gold.r * pulse, UiSkin.Gold.g * pulse, UiSkin.Gold.b * pulse));
                Label(pill, "[Q]  CALL IN " + (si != null ? si.name.ToUpperInvariant() : "STREAK"), mid, UiSkin.Ink, UiSkin.Display, 22, false);
            }
            else
            {
                int left2 = p.KnockoffsToNextStreak(out var next);
                var si = Loadouts.StreakInfo(next);
                string s = si != null
                    ? "STREAK  " + p.StreakCount + " / " + si.cost + "  ->  " + si.name.ToUpperInvariant()
                    : "STREAK  " + p.StreakCount + "  (all earned)";
                Label(new Rect(x, y, 354f, 34f), s, left, left2 == 1 ? UiSkin.Gold : UiSkin.Cream, UiSkin.Display, 20);
            }
            y += 42f;

            string shove = w.ShoveCooldown > 0f ? "[E] SHOVE  ..." : "[E] SHOVE  READY";
            Label(new Rect(x, y, 354f, 28f), shove, left, w.ShoveCooldown > 0f ? Color.gray : UiSkin.Cream, UiSkin.Display, 18);
        }

        void DrawCrosshair(float W, float H)
        {
            Box(new Rect(W / 2f - 1.5f, H / 2f - 10f, 3f, 20f), Color.white);
            Box(new Rect(W / 2f - 10f, H / 2f - 1.5f, 20f, 3f), Color.white);
        }

        void DrawHitMarker(float W, float H)
        {
            float age = Time.time - hitMarkerTime;
            if (age > 0.18f) return;
            float a = 1f - age / 0.18f;
            float gap = 9f + hitMarkerSize * 6f + age * 40f, len = 10f + hitMarkerSize * 8f;
            Color col = Color.Lerp(Color.white, UiSkin.Red, hitMarkerSize);
            col.a = a;
            var old = GUI.matrix;
            Vector2 c = new Vector2(W / 2f, H / 2f);
            for (int i = 0; i < 4; i++)
            {
                GUI.matrix = old * UiSkin.Around(c, 45f + 90f * i, 1f);
                Box(new Rect(c.x - 1.5f, c.y - gap - len, 3f, len), col);
            }
            GUI.matrix = old;
        }

        void DrawScope(float W, float H)
        {
            // A dark ring around a round window, with thin crosshairs across.
            float d = H * 0.86f;
            var circle = new Rect(W / 2f - d / 2f, H / 2f - d / 2f, d, d);
            Color black = new Color(0f, 0f, 0f, 0.92f);
            Box(new Rect(0f, 0f, circle.x, H), black);
            Box(new Rect(circle.xMax, 0f, W - circle.xMax, H), black);
            Box(new Rect(circle.x, 0f, d, circle.y), black);
            Box(new Rect(circle.x, circle.yMax, d, H - circle.yMax), black);
            // The vignette art darkens the corners of the window so it reads as round.
            for (int i = 0; i < 3; i++) UiSkin.Image(circle, UiSkin.Vignette, Color.black);
            Box(new Rect(circle.x, H / 2f - 1f, d, 2f), new Color(0f, 0f, 0f, 0.85f));
            Box(new Rect(W / 2f - 1f, circle.y, 2f, d), new Color(0f, 0f, 0f, 0.85f));
            Box(new Rect(W / 2f - 3f, H / 2f - 3f, 6f, 6f), UiSkin.Red);
        }

        void DrawToast(float W, float H)
        {
            if (Announcer.Toast == null || Time.time - Announcer.ToastTime > 2.2f) return;
            Label(new Rect(W / 2f - 500f, H * 0.62f, 1000f, 60f), Announcer.Toast, mid, UiSkin.Gold);
        }

        void DrawEliminated(float W, float H)
        {
            var r = new Rect(W / 2f - 340f, H * 0.2f, 680f, 92f);
            UiSkin.Pill(r, new Color(0.35f, 0.08f, 0.12f, 0.9f));
            Label(new Rect(r.x, r.y + 4f, r.width, 50f), "ELIMINATED", mid, UiSkin.Cream, UiSkin.Display, 40);
            Label(new Rect(r.x, r.y + 50f, r.width, 34f), "Enjoy the Losers' Lounge. Heckle them on voice chat.", center, UiSkin.Muted,
                UiSkin.BodyBold, 19, false);
        }

        void DrawCountdown(RaceManager race, ShowManager show, float W, float H)
        {
            int n = Mathf.CeilToInt(race.Clock);
            float cx = W / 2f;
            var panel = new Rect(cx - 460f, H * 0.24f, 920f, 250f);
            UiSkin.Panel(panel);
            Label(new Rect(panel.x, panel.y + 18f, panel.width, 40f), "EVENT " + (show.RoundIndex + 1) + " OF " + show.RoundCount, mid,
                UiSkin.LabelBlue, UiSkin.Display, 28);
            Label(new Rect(panel.x, panel.y + 56f, panel.width, 80f), Events.Title(race.Kind), big, UiSkin.Gold);
            var obj = new GUIStyle(center) { fontSize = 22 };
            Label(new Rect(panel.x + 40f, panel.y + 136f, panel.width - 80f, 60f), Events.Objective(race.Kind), obj, UiSkin.Cream);
            string count = n > 3 ? "GET READY" : n.ToString();
            Label(new Rect(panel.x, panel.yMax - 2f, panel.width, 110f), count, n > 3 ? big : huge, UiSkin.Gold);
        }

        // ---------- Results ----------

        void DrawRoundResults(RaceManager race, ShowManager show, float W, float H)
        {
            float rowH = 40f;
            var r = new Rect(W / 2f - 380f, 150f, 760f, 170f + race.Results.Count * rowH + 70f);
            UiSkin.Panel(r);
            Label(new Rect(r.x, r.y + 16f, r.width, 40f), Events.Title(race.Kind) + "  RESULTS", mid, UiSkin.LabelBlue, UiSkin.Display, 30);
            int place = race.PlaceOf(race.Player);
            string head = race.Player == null ? "STANDINGS" : place == 1 ? "YOU'RE IN THE LEAD!" : "YOU'RE #" + place;
            Label(new Rect(r.x, r.y + 56f, r.width, 64f), head, big, place == 1 ? UiSkin.Gold : UiSkin.Cream);

            float y = r.y + 130f;
            HeaderRow(r.x, y, "THIS EVENT", "SHOW TOTAL");
            y += 34f;
            for (int i = 0; i < race.Results.Count; i++)
            {
                ResultRow(race.Results[i], i + 1, r.x, y, r.width, show);
                y += rowH;
            }

            float secsLeft = Mathf.Max(0f, show.NextRoundAt - Time.time);
            string next = Events.Title(ShowManager.KindFor(show.RoundIndex + 1));
            string footer = "NEXT: " + next + " in " + Mathf.CeilToInt(secsLeft) +
                            (Net.IsHost ? "   (R to start now" + (Net.Online ? ", L for the lobby)" : ")") : "");
            Label(new Rect(r.x, y + 12f, r.width, 40f), footer, mid, UiSkin.Gold, UiSkin.Display, 24);
        }

        void DrawShowResults(RaceManager race, ShowManager show, float W, float H)
        {
            float rowH = 40f;
            bool hasPlayer = race.Player != null;
            float rewardsH = hasPlayer ? 130f + (show.NewUnlocks.Count > 0 ? 34f : 0f) : 0f;
            var r = new Rect(W / 2f - 400f, 110f, 800f, 150f + 34f + race.Results.Count * rowH + rewardsH + 70f);
            UiSkin.Panel(r);
            var winner = race.Results.Count > 0 ? race.Results[0] : null;
            Label(new Rect(r.x, r.y + 16f, r.width, 40f), "THE SHOW IS OVER", mid, UiSkin.LabelBlue, UiSkin.Display, 30);
            string head = winner == null ? "NOBODY WINS" : winner.IsPlayer ? "YOU WIN THE SHOW!" : winner.DisplayName.ToUpperInvariant() + " WINS!";
            float wob = 1f + 0.03f * Mathf.Sin(Time.time * 4f);
            var old = GUI.matrix;
            var headRect = new Rect(r.x, r.y + 56f, r.width, 72f);
            GUI.matrix = old * UiSkin.Around(headRect.center, 0f, wob);
            Label(headRect, head, big, UiSkin.Gold);
            GUI.matrix = old;

            float y = r.y + 140f;
            HeaderRow(r.x, y, "LAST EVENT", "TOTAL");
            y += 34f;
            for (int i = 0; i < race.Results.Count; i++)
            {
                ResultRow(race.Results[i], i + 1, r.x, y, r.width, show);
                y += rowH;
            }

            if (hasPlayer)
            {
                y += 10f;
                var box = new Rect(r.x + 30f, y, r.width - 60f, rewardsH - 10f);
                UiSkin.Card(box);
                Label(new Rect(box.x + 24f, box.y + 10f, box.width - 48f, 36f),
                    "+" + Money(show.RewardCash) + " to your wallet     +" + show.RewardXp + " XP", left, UiSkin.Gold, UiSkin.Display, 28);
                string lvl = show.LevelsGained > 0
                    ? "LEVEL UP!  " + show.LevelBefore + "  ->  " + Profile.Level
                    : "LEVEL " + Profile.Level + "   " + Profile.Xp + " / " + Profile.XpToNext + " XP";
                Label(new Rect(box.x + 24f, box.y + 48f, box.width - 48f, 34f), lvl, left,
                    show.LevelsGained > 0 ? UiSkin.Green : UiSkin.Cream, UiSkin.Display, 24);
                float fill = Profile.XpToNext > 0 ? Mathf.Clamp01((float)Profile.Xp / Profile.XpToNext) : 1f;
                var barR = new Rect(box.x + 24f, box.y + 88f, box.width - 48f, 14f);
                UiSkin.Pill(barR, UiSkin.PillDark);
                if (fill > 0.02f) UiSkin.Pill(new Rect(barR.x, barR.y, Mathf.Max(16f, barR.width * fill), barR.height), UiSkin.Green);
                if (show.NewUnlocks.Count > 0)
                    Label(new Rect(box.x + 24f, box.y + 108f, box.width - 48f, 30f), "UNLOCKED:  " + string.Join(",  ", show.NewUnlocks),
                        left, UiSkin.Gold, UiSkin.BodyBold, 20);
                y += rewardsH;
            }

            string next = !Net.Online ? "R (or Start) for a new show  -  Esc for the menu"
                        : Net.IsHost ? "R (or Start) for a new show  -  L to go back to the lobby"
                        : "Waiting for the host...";
            Label(new Rect(r.x, y + 12f, r.width, 40f), next, mid, UiSkin.Gold, UiSkin.Display, 24);
        }

        void HeaderRow(float x, float y, string colA, string colB)
        {
            Label(new Rect(x + 60f, y, 260f, 30f), "CONTESTANT", left, UiSkin.Muted, UiSkin.Display, 18);
            Label(new Rect(x + 360f, y, 120f, 30f), "K.O.", right, UiSkin.Muted, UiSkin.Display, 18);
            Label(new Rect(x + 480f, y, 130f, 30f), colA, right, UiSkin.Muted, UiSkin.Display, 18);
            Label(new Rect(x + 610f, y, 130f, 30f), colB, right, UiSkin.Muted, UiSkin.Display, 18);
        }

        void ResultRow(Contestant c, int place, float x, float y, float width, ShowManager show)
        {
            if (c == null) return;
            if (c.IsPlayer) UiSkin.Pill(new Rect(x + 24f, y + 2f, width - 48f, 36f), new Color(1f, 0.79f, 0.16f, 0.18f));
            Color col = c.IsPlayer ? UiSkin.Gold : UiSkin.Cream;
            Label(new Rect(x + 36f, y, 30f, 40f), place.ToString(), left, col, UiSkin.Display, 24);
            Box(new Rect(x + 70f, y + 13f, 14f, 14f), c.Color);
            Label(new Rect(x + 94f, y, 260f, 40f), c.DisplayName, left, col, UiSkin.Body, 22);
            Label(new Rect(x + 360f, y, 120f, 40f), c.Knockoffs.ToString(), right, col, UiSkin.Display, 22);
            int rc = show.RoundCash(c);
            Label(new Rect(x + 480f, y, 130f, 40f), (rc >= 0 ? "+" : "-") + Money(Mathf.Abs(rc)), right,
                rc >= 0 ? new Color(0.6f, 1f, 0.55f) : new Color(1f, 0.5f, 0.45f), UiSkin.Display, 22);
            Label(new Rect(x + 610f, y, 130f, 40f), Money(show.Cash(c)), right, UiSkin.Gold, UiSkin.Display, 24);
        }

        // ---------- Loading screen ----------

        static readonly string[] LoadingLines =
        {
            "Hosing off the last contestants...", "Bribing the safety inspector...", "Sobering up Rusty...",
            "Counting the cash. Twice...", "Feeding Gary the Shithawk...", "Signing your organ donor cards...",
            "Unclogging Lord Dookington III...", "Tightening loose bolts. Some of them...", "Removing evidence...",
        };

        static readonly string[,] Controls =
        {
            { "WASD", "Move" }, { "SPACE", "Jump" }, { "L-CLICK", "Fire" }, { "R-CLICK", "Aim" },
            { "E", "Shove" }, { "G", "Gadget" }, { "Q", "Streak" },
        };

        void DrawLoading(RaceManager race, ShowManager show, Contestant p, float W, float H)
        {
            UiSkin.Fill(new Rect(0f, 0f, W, H), new Color(0.07f, 0.05f, 0.12f, 1f));
            UiSkin.StageBackground(W, H);

            float total = ShowManager.LoadingTimeFor(show.RoundIndex);
            float remaining = race.Clock - race.countdownTime;
            float progress = Mathf.Clamp01(1f - remaining / total);
            float cx = W / 2f;

            // ---- Event name and the one-line goal ----
            Label(new Rect(cx - 400f, 90f, 800f, 36f), "EVENT " + (show.RoundIndex + 1) + " OF " + show.RoundCount, mid,
                UiSkin.LabelBlue, UiSkin.Display, 30);
            float pop = 1f + 0.02f * Mathf.Sin(Time.time * 2.4f);
            var titleRect = new Rect(cx - 600f, 132f, 1200f, 110f);
            var old = GUI.matrix;
            GUI.matrix = old * UiSkin.Around(titleRect.center, Mathf.Sin(Time.time * 0.9f) * 1.2f, pop);
            Label(titleRect, Events.Title(race.Kind), huge, UiSkin.Gold);
            GUI.matrix = old;
            Label(new Rect(cx - 700f, 252f, 1400f, 50f), Events.Goal(race.Kind), center, UiSkin.Cream, UiSkin.Display, 40);

            // ---- Three rule cards ----
            var cards = Events.RuleCards(race.Kind);
            int n = cards.GetLength(0);
            float cw = 380f, ch = 190f, gap = 36f;
            float x0 = cx - (n * cw + (n - 1) * gap) / 2f;
            for (int i = 0; i < n; i++)
            {
                var r = new Rect(x0 + i * (cw + gap), 350f, cw, ch);
                UiSkin.Panel(r);
                Label(new Rect(r.x, r.y + 30f, r.width, 80f), cards[i, 0], big, UiSkin.Gold);
                Label(new Rect(r.x + 10f, r.y + 116f, r.width - 20f, 40f), cards[i, 1], center, UiSkin.Cream, UiSkin.Display, 28);
            }

            // ---- Where you stand (after the first event) ----
            if (show.RoundIndex > 0 && p != null)
            {
                int place = show.Ranking(race.Contestants).IndexOf(p) + 1;
                Label(new Rect(cx - 600f, 580f, 1200f, 44f), "You're #" + place + " with " + Money(show.Cash(p)), center,
                    place == 1 ? UiSkin.Gold : UiSkin.Cream, UiSkin.Display, 32);
            }
            else
            {
                Label(new Rect(cx - 600f, 580f, 1200f, 44f), "Most cash after " + show.RoundCount + " events wins.", center,
                    UiSkin.Cream, UiSkin.Display, 32);
            }

            // ---- Controls: one row ----
            int rows = Controls.GetLength(0);
            float kw = 168f, kgap = 14f;
            float kx = cx - (rows * kw + (rows - 1) * kgap) / 2f;
            for (int i = 0; i < rows; i++)
            {
                var r = new Rect(kx + i * (kw + kgap), 700f, kw, 92f);
                UiSkin.Card(r);
                Label(new Rect(r.x, r.y + 10f, r.width, 40f), Controls[i, 0], center, UiSkin.Gold, UiSkin.Display, 26);
                Label(new Rect(r.x, r.y + 48f, r.width, 32f), Controls[i, 1], center, UiSkin.Cream, UiSkin.BodyBold, 22);
            }

            // ---- Loading bar ----
            float bw = Mathf.Min(1200f, W - 80f);
            var bar = new Rect(cx - bw / 2f, H - 110f, bw, 34f);
            string line = LoadingLines[Mathf.Abs(Mathf.FloorToInt(race.RaceStartTime * 7f) + Mathf.FloorToInt(progress * 4f)) % LoadingLines.Length];
            Label(new Rect(bar.x, bar.y - 44f, bar.width - 120f, 38f), line, left, UiSkin.Muted, UiSkin.Display, 24);
            Label(new Rect(bar.xMax - 120f, bar.y - 44f, 120f, 38f), Mathf.FloorToInt(progress * 100f) + "%", right, UiSkin.Gold, UiSkin.Display, 26);
            UiSkin.Pill(bar, UiSkin.PillDark);
            float fillW = Mathf.Max(30f, bar.width * progress);
            UiSkin.Pill(new Rect(bar.x, bar.y, fillW, bar.height), UiSkin.Yellow);
            UiSkin.StripeBand(new Rect(bar.x + 12f, bar.y + 8f, Mathf.Max(0f, fillW - 24f), bar.height - 16f), Time.time * 0.8f);
        }

        // ---------- Helpers ----------

        public static string Money(int amount) => "$" + amount.ToString("N0");

        void Label(Rect r, string text, GUIStyle style, Color color) => Label(r, text, style, color, null, 0, true);

        void Label(Rect r, string text, GUIStyle style, Color color, Font font, int size, bool shadow = true)
        {
            var oldFont = style.font;
            int oldSize = style.fontSize;
            if (font != null) style.font = font;
            if (size > 0) style.fontSize = size;
            var old = style.normal.textColor;
            if (shadow)
            {
                style.normal.textColor = new Color(0f, 0f, 0f, 0.7f * color.a);
                GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, style);
            }
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
            style.font = oldFont;
            style.fontSize = oldSize;
        }

        void Box(Rect r, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }
    }
}
