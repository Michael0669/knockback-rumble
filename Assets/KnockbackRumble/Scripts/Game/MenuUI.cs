using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Main menu, lobby ("The Green Room"), connecting screen, pause menu ("Commercial
    /// Break") and the small online status strip shown during races.
    /// Laid out on a 1600x1080 minimum canvas that scales with the window; the art and
    /// helpers live in UiSkin. Layout matches the mockups made by Tools/mockup.py.
    /// </summary>
    public class MenuUI : MonoBehaviour
    {
        public static bool Paused { get; private set; }

        public static void SetPaused(bool paused)
        {
            Paused = paused;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        enum Screen { Main, Connecting, Lobby, Race }

        string nameField = "";
        string ipField = "127.0.0.1";
        string portField = NetProtocol.DirectPort.ToString();
        bool showTools;
        Screen lastScreen = Screen.Main;

        void Start()
        {
            var steam = SteamService.Instance;
            nameField = steam != null && steam.Ready && !string.IsNullOrEmpty(steam.PersonaName) ? steam.PersonaName : "Dummy";
        }

        void Update()
        {
            var match = MatchController.Instance;
            bool inMatch = match != null && match.InMatch;
            if (!inMatch)
            {
                // Menus need the mouse.
                Paused = false;
                if (Cursor.lockState != CursorLockMode.None) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            }
        }

        void OnGUI()
        {
            var match = MatchController.Instance;
            var session = NetSession.Instance;
            if (match == null || session == null) return;

            UiSkin.Ensure();
            GUI.depth = -10; // above the HUD

            // A 1600x1080 minimum canvas, scaled to fit the window.
            float scale = Mathf.Min(UnityEngine.Screen.height / 1080f, UnityEngine.Screen.width / 1600f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float W = UnityEngine.Screen.width / scale;
            float H = UnityEngine.Screen.height / scale;
            float oy = Mathf.Max(0f, (H - 1080f) / 2f); // center the 1080-tall layout vertically

            Screen screen = match.InMatch ? Screen.Race
                          : session.State == SessionState.Connecting ? Screen.Connecting
                          : session.State == SessionState.Connected ? Screen.Lobby
                          : Screen.Main;
            if (screen != lastScreen)
            {
                GUI.FocusControl(null); // drop text-box focus between screens
                lastScreen = screen;
            }

            switch (screen)
            {
                case Screen.Race:
                    if (Net.Online) DrawOnlineStrip(session, W);
                    if (Paused) DrawPause(match, W, H, oy);
                    break;
                case Screen.Connecting:
                    UiSkin.StageBackground(W, H);
                    DrawConnecting(session, W, oy);
                    break;
                case Screen.Lobby:
                    UiSkin.StageBackground(W, H);
                    DrawLobby(session, match, W, oy);
                    break;
                default:
                    UiSkin.StageBackground(W, H);
                    DrawMain(session, match, W, H, oy);
                    break;
            }
        }

        // =====================================================================
        // Main menu
        // =====================================================================

        void DrawMain(NetSession session, MatchController match, float W, float H, float oy)
        {
            var steam = SteamService.Instance;
            bool steamReady = steam != null && steam.Ready;
            float cx = W / 2f;

            float lw = 860f;
            UiSkin.Image(new Rect(cx - lw / 2f, oy + 14f, lw, UiSkin.HeightFor(UiSkin.Logo, lw)), UiSkin.Logo);

            float px = cx - 340f, py = oy + 470f, pw = 680f, ph = 500f;
            UiSkin.Panel(new Rect(px, py, pw, ph));
            UiSkin.Heading(new Rect(px + 40f, py + 26f, 300f, 30f), "YOUR NAME", 24, UiSkin.LabelBlue);
            nameField = UiSkin.TextField(new Rect(px + 40f, py + 60f, 600f, 66f), nameField, 20, "name");

            if (UiSkin.Button(new Rect(px + 40f, py + 148f, 600f, 104f), "HOST A GAME", UiSkin.Yellow,
                    steamReady ? "Invite your Steam friends" : "Needs Steam running", 44, steamReady))
            {
                session.LocalName = CleanName();
                session.HostSteam();
            }

            if (UiSkin.Button(new Rect(px + 40f, py + 266f, 600f, 88f), "PLAY OFFLINE", UiSkin.Dark,
                    "You vs " + match.OfflineBots + " bots", 36))
            {
                match.OfflineName = CleanName();
                match.StartOffline();
            }

            var status = new Rect(px + 40f, py + 370f, 600f, 42f);
            UiSkin.Pill(status, UiSkin.PillDark);
            UiSkin.Image(new Rect(status.x + 16f, status.y + 13f, 16f, 16f), UiSkin.Dot, steamReady ? UiSkin.Green : UiSkin.Red);
            UiSkin.Text(new Rect(status.x + 44f, status.y, 540f, 42f),
                steamReady ? "Steam: logged in as " + steam.PersonaName : "Steam isn't running - online play is off",
                UiSkin.BodyBold, 20, UiSkin.Cream, TextAnchor.MiddleLeft, false);

            UiSkin.Text(new Rect(px + 40f, py + 420f, 600f, 30f), "Joining a friend? Accept their Steam invite, or right-click",
                UiSkin.BodyBold, 19, UiSkin.Muted, TextAnchor.MiddleCenter, false);
            UiSkin.Text(new Rect(px + 40f, py + 446f, 600f, 30f), "them in your Steam friends list and pick Join Game.",
                UiSkin.BodyBold, 19, UiSkin.Muted, TextAnchor.MiddleCenter, false);

            if (!string.IsNullOrEmpty(session.LastError))
            {
                var err = new Rect(cx - 450f, py + ph + 14f, 900f, 46f);
                UiSkin.Pill(err, UiSkin.Red);
                UiSkin.Text(err, session.LastError, UiSkin.BodyBold, 20, UiSkin.Cream, TextAnchor.MiddleCenter, false);
            }

            // Testing tools (direct connect) tucked away bottom-left.
            if (UiSkin.Button(new Rect(40f, H - 86f, 300f, 60f), showTools ? "HIDE TESTING TOOLS" : "TESTING TOOLS", UiSkin.Dark, null, 24))
                showTools = !showTools;
            if (showTools) DrawTools(session, H);

            if (UiSkin.Button(new Rect(W - 200f, H - 86f, 160f, 60f), "QUIT", UiSkin.Red, null, 26)) Quit();
        }

        void DrawTools(NetSession session, float H)
        {
            float tx = 40f, tw = 560f, th = 236f, ty = H - 86f - 14f - th;
            UiSkin.Panel(new Rect(tx, ty, tw, th));
            UiSkin.Heading(new Rect(tx + 30f, ty + 20f, 500f, 30f), "DIRECT CONNECT  (testing without Steam)", 22, UiSkin.LabelBlue);

            UiSkin.Text(new Rect(tx + 30f, ty + 64f, 50f, 52f), "IP", UiSkin.Body, 22, UiSkin.Cream);
            ipField = UiSkin.TextField(new Rect(tx + 76f, ty + 62f, 264f, 54f), ipField, 64, "ip");
            UiSkin.Text(new Rect(tx + 354f, ty + 64f, 70f, 52f), "Port", UiSkin.Body, 22, UiSkin.Cream);
            portField = UiSkin.TextField(new Rect(tx + 420f, ty + 62f, 110f, 54f), portField, 5, "port");

            if (UiSkin.Button(new Rect(tx + 30f, ty + 140f, 240f, 70f), "HOST DIRECT", UiSkin.Teal, null, 26))
            {
                session.LocalName = CleanName();
                session.HostDirect(Port());
            }
            if (UiSkin.Button(new Rect(tx + 290f, ty + 140f, 240f, 70f), "JOIN DIRECT", UiSkin.Teal, null, 26))
            {
                session.LocalName = CleanName();
                session.JoinDirect(ipField.Trim(), Port());
            }
        }

        // =====================================================================
        // Connecting
        // =====================================================================

        void DrawConnecting(NetSession session, float W, float oy)
        {
            float cx = W / 2f;
            float lw = 620f;
            UiSkin.Image(new Rect(cx - lw / 2f, oy + 60f, lw, UiSkin.HeightFor(UiSkin.Logo, lw)), UiSkin.Logo);

            var p = new Rect(cx - 320f, oy + 470f, 640f, 250f);
            UiSkin.Panel(p);
            string title = session.IsHost ? "OPENING THE STUDIO" : "CONNECTING";
            UiSkin.Heading(new Rect(p.x, p.y + 34f, p.width, 50f), title + Dots(), 40, UiSkin.Gold, TextAnchor.MiddleCenter);
            UiSkin.Text(new Rect(p.x, p.y + 86f, p.width, 36f), session.IsHost ? "Setting up your Steam lobby" : "Getting you into the green room",
                UiSkin.BodyBold, 22, UiSkin.Cream, TextAnchor.MiddleCenter, false);
            if (UiSkin.Button(new Rect(p.x + 170f, p.y + 150f, 300f, 66f), "CANCEL", UiSkin.Dark, null, 28)) session.Leave(null);
        }

        // =====================================================================
        // Lobby
        // =====================================================================

        void DrawLobby(NetSession session, MatchController match, float W, float oy)
        {
            float cx = W / 2f;
            var voice = NetVoice.Instance;

            float tw = 640f;
            UiSkin.Image(new Rect(cx - tw / 2f, oy + 30f, tw, UiSkin.HeightFor(UiSkin.TitleLobby, tw)), UiSkin.TitleLobby);
            var sub = new Rect(cx - 230f, oy + 148f, 460f, 42f);
            UiSkin.Pill(sub, UiSkin.PillDark);
            UiSkin.Heading(sub, session.UsingSteam ? "STEAM LOBBY  -  FRIENDS ONLY" : "DIRECT CONNECT  -  PORT " + Port(), 22,
                UiSkin.Cream, TextAnchor.MiddleCenter);

            // ---- Contestants ----
            float lx = cx - 740f, ly = oy + 214f, lw = 940f, lh = 640f;
            UiSkin.Panel(new Rect(lx, ly, lw, lh));
            UiSkin.Heading(new Rect(lx + 40f, ly + 26f, 400f, 44f), "CONTESTANTS", 38, UiSkin.Cream);
            UiSkin.Heading(new Rect(lx + lw - 240f, ly + 26f, 200f, 44f), session.Peers.Count + " / " + NetProtocol.MaxPlayers, 38,
                UiSkin.Gold, TextAnchor.MiddleRight);

            const float cw = 418f, ch = 116f, gx = 24f, gy = 16f;
            for (int i = 0; i < NetProtocol.MaxPlayers; i++)
            {
                float x = lx + 40f + (i % 2) * (cw + gx);
                float y = ly + 96f + (i / 2) * (ch + gy);
                var slot = new Rect(x, y, cw, ch);
                if (i < session.Peers.Count) DrawPlayerCard(slot, session.Peers[i], session, voice);
                else DrawEmptySlot(slot, session.FillBots, session.IsHost);
            }

            // ---- The show ----
            float rx = cx + 228f, ry = oy + 214f, rw = 512f, rh = 640f;
            UiSkin.Panel(new Rect(rx, ry, rw, rh));
            UiSkin.Heading(new Rect(rx + 40f, ry + 26f, 400f, 44f), "THE SHOW", 38, UiSkin.Cream);

            int dummies = session.FillBots ? MatchController.RaceSize : session.Peers.Count;
            float share = RaceManager.Instance != null ? RaceManager.Instance.qualifyShare : 0.625f;
            int qualify = Mathf.Clamp(Mathf.RoundToInt(dummies * share), 1, Mathf.Max(1, dummies));
            UiSkin.Text(new Rect(rx + 40f, ry + 82f, 440f, 32f), "Round 1:  Scrap Sprint (race)", UiSkin.Body, 23, UiSkin.Cream, TextAnchor.MiddleLeft, false);
            UiSkin.Text(new Rect(rx + 40f, ry + 116f, 440f, 32f), dummies + " dummies  -  top " + qualify + " qualify", UiSkin.Body, 23,
                UiSkin.Muted, TextAnchor.MiddleLeft, false);

            if (session.IsHost)
            {
                var row = new Rect(rx + 32f, ry + 168f, rw - 64f, 84f);
                UiSkin.Card(row);
                UiSkin.Text(new Rect(row.x + 24f, row.y, 300f, row.height), "Fill empty spots with bots", UiSkin.Body, 22, UiSkin.Cream,
                    TextAnchor.MiddleLeft, false);
                match.FillWithBots = UiSkin.Switch(row, new Rect(row.xMax - 24f - 100f, row.y + 18f, 100f, 50f), match.FillWithBots);

                float y = ry + 280f;
                if (session.UsingSteam)
                {
                    if (UiSkin.Button(new Rect(rx + 32f, y, rw - 64f, 88f), "INVITE FRIENDS", UiSkin.Teal, "Opens your Steam friends list", 32))
                        SteamService.Instance.OpenInviteDialog();
                }
                else
                {
                    UiSkin.Card(new Rect(rx + 32f, y, rw - 64f, 88f));
                    UiSkin.Text(new Rect(rx + 32f, y + 10f, rw - 64f, 36f), "Friends join with JOIN DIRECT", UiSkin.Body, 21, UiSkin.Cream,
                        TextAnchor.MiddleCenter, false);
                    UiSkin.Text(new Rect(rx + 32f, y + 44f, rw - 64f, 32f), "using your IP address and port " + Port(), UiSkin.BodyBold, 19,
                        UiSkin.Muted, TextAnchor.MiddleCenter, false);
                }

                if (UiSkin.Button(new Rect(rx + 32f, ry + 392f, rw - 64f, 120f), "START THE SHOW", UiSkin.Yellow, "Everyone in the lobby races", 46))
                    match.HostStartRace();
            }
            else
            {
                var wait = new Rect(rx + 32f, ry + 168f, rw - 64f, 340f);
                UiSkin.Card(wait);
                UiSkin.Image(new Rect(wait.center.x - 48f, wait.y + 48f + Mathf.Sin(Time.unscaledTime * 3f) * 6f, 96f, 96f), UiSkin.Avatar,
                    UiSkin.Gold);
                UiSkin.Heading(new Rect(wait.x, wait.y + 172f, wait.width, 50f), "WAITING FOR THE HOST", 32, UiSkin.Gold, TextAnchor.MiddleCenter);
                UiSkin.Text(new Rect(wait.x, wait.y + 222f, wait.width, 36f), "to start the show" + Dots(), UiSkin.Body, 24, UiSkin.Cream,
                    TextAnchor.MiddleCenter, false);
            }

            if (UiSkin.Button(new Rect(rx + 32f, ry + 540f, rw - 64f, 72f), "LEAVE LOBBY", UiSkin.Red, null, 28)) session.Leave(null);

            DrawMicBar(new Rect(cx - 400f, oy + 884f, 800f, 56f), voice);
        }

        void DrawPlayerCard(Rect r, NetPeer p, NetSession session, NetVoice voice)
        {
            UiSkin.Card(r);
            var color = MatchController.HumanColors[p.Color % MatchController.HumanColors.Length];
            UiSkin.Image(new Rect(r.x + 16f, r.y + 16f, 84f, 84f), UiSkin.Avatar, color);

            bool you = p.Id == session.LocalId;
            bool talking = voice != null && voice.IsTalking(p.Id);
            bool badges = p.IsHost || you || talking;

            float nameY = badges ? r.y + 16f : r.y + 36f;
            UiSkin.Heading(new Rect(r.x + 116f, nameY, r.width - 130f, 44f), p.Name, 32, UiSkin.Cream);

            if (!badges) return;
            float bx = r.x + 116f, by = r.y + 66f;
            if (p.IsHost) bx += UiSkin.Badge(bx, by, "HOST", UiSkin.Yellow, UiSkin.CrownIcon) + 10f;
            if (you) bx += UiSkin.Badge(bx, by, "YOU", UiSkin.Teal) + 10f;
            if (talking) UiSkin.Badge(bx, by, "TALKING", UiSkin.Green, UiSkin.MicIcon);
        }

        void DrawEmptySlot(Rect r, bool bots, bool isHost)
        {
            UiSkin.CardEmpty(r);
            if (bots)
            {
                UiSkin.Image(new Rect(r.x + 22f, r.y + 22f, 72f, 72f), UiSkin.BotIcon, new Color(1f, 1f, 1f, 0.75f));
                UiSkin.Heading(new Rect(r.x + 116f, r.y + 20f, r.width - 130f, 40f), "BOT", 30, new Color(1f, 0.96f, 0.84f, 0.8f));
                UiSkin.Text(new Rect(r.x + 116f, r.y + 62f, r.width - 130f, 30f), "fills this spot", UiSkin.BodyBold, 20, UiSkin.Muted,
                    TextAnchor.MiddleLeft, false);
            }
            else
            {
                UiSkin.Heading(new Rect(r.x, r.y + 22f, r.width, 40f), "OPEN SPOT", 30, new Color(1f, 0.96f, 0.84f, 0.7f), TextAnchor.MiddleCenter);
                UiSkin.Text(new Rect(r.x, r.y + 62f, r.width, 30f), isHost ? "Invite a friend" : "Waiting for players", UiSkin.BodyBold, 20,
                    UiSkin.Muted, TextAnchor.MiddleCenter, false);
            }
        }

        void DrawMicBar(Rect bar, NetVoice voice)
        {
            UiSkin.Pill(bar, UiSkin.PillDark);
            bool available = voice != null && voice.Available;
            bool on = available && voice.MicOn;
            var tag = new Rect(bar.x + 8f, bar.y + 8f, available ? (on ? 132f : 150f) : 160f, bar.height - 16f);
            UiSkin.Pill(tag, !available ? UiSkin.Dark : on ? UiSkin.Green : UiSkin.Red);
            UiSkin.Image(new Rect(tag.x + 12f, tag.y + 6f, 28f, 28f), UiSkin.MicIcon, available && on ? UiSkin.Ink : UiSkin.Cream);
            UiSkin.Heading(new Rect(tag.x + 46f, tag.y, tag.width - 50f, tag.height), !available ? "VOICE OFF" : on ? "MIC ON" : "MUTED", 22,
                available && on ? UiSkin.Ink : UiSkin.Cream);

            string msg = !available ? "Voice chat needs Steam running on every player's PC."
                       : on ? "Everyone can hear you.  Press M (or click) to mute."
                       : "Nobody can hear you.  Press M (or click) to unmute.";
            UiSkin.Text(new Rect(tag.xMax + 16f, bar.y, bar.xMax - tag.xMax - 30f, bar.height), msg, UiSkin.BodyBold, 21, UiSkin.Cream,
                TextAnchor.MiddleLeft, false);

            if (available && GUI.Button(bar, GUIContent.none, GUIStyle.none)) voice.ToggleMic();
        }

        // =====================================================================
        // In a race
        // =====================================================================

        void DrawPause(MatchController match, float W, float H, float oy)
        {
            UiSkin.Fill(new Rect(0f, 0f, W, H), new Color(0.03f, 0.02f, 0.06f, 0.6f));
            float cx = W / 2f;
            bool online = Net.Online, host = Net.IsHost;

            float tw = 760f, th = UiSkin.HeightFor(UiSkin.TitlePaused, tw);
            float ty = oy + 150f;
            UiSkin.Image(new Rect(cx - tw / 2f, ty, tw, th), UiSkin.TitlePaused);

            float pw = 600f, px = cx - pw / 2f, py = ty + th + 24f;
            float ph = 84f + 108f + (online && host ? 100f : 0f) + 92f + 56f;
            UiSkin.Panel(new Rect(px, py, pw, ph));
            UiSkin.Text(new Rect(px, py + 26f, pw, 34f), online ? "The race keeps going while you're in here!" : "Take a breather.",
                UiSkin.Body, 23, UiSkin.Cream, TextAnchor.MiddleCenter, false);

            float y = py + 84f;
            if (UiSkin.Button(new Rect(px + 40f, y, pw - 80f, 92f), "RESUME", UiSkin.Yellow, null, 40)) SetPaused(false);
            y += 108f;

            if (online && host)
            {
                if (UiSkin.Button(new Rect(px + 40f, y, pw - 80f, 88f), "BACK TO LOBBY", UiSkin.Teal, "Everyone returns to the green room", 30))
                    match.HostBackToLobby();
                y += 100f;
            }

            string leave = !online ? "QUIT TO MENU" : host ? "END GAME FOR EVERYONE" : "LEAVE GAME";
            if (UiSkin.Button(new Rect(px + 40f, y, pw - 80f, 76f), leave, UiSkin.Red, null, 30))
            {
                if (online) Net.Session.Leave(null);
                else match.EndMatch();
            }
            y += 92f;

            var voice = NetVoice.Instance;
            if (online)
            {
                string mic = voice == null || !voice.Available ? "Voice chat is off (needs Steam)"
                           : voice.MicOn ? "Mic ON  -  press M to mute" : "Mic MUTED  -  press M to unmute";
                UiSkin.Text(new Rect(px, y, pw, 30f), mic, UiSkin.BodyBold, 20, UiSkin.Muted, TextAnchor.MiddleCenter, false);
            }
        }

        void DrawOnlineStrip(NetSession session, float W)
        {
            var voice = NetVoice.Instance;
            var bar = new Rect(W - 440f, 14f, 420f, 44f);
            UiSkin.Pill(bar, UiSkin.PillDark);
            UiSkin.Heading(new Rect(bar.x + 20f, bar.y, 200f, bar.height), session.IsHost ? "YOU'RE HOSTING" : "PING " + session.PingMs + " ms",
                20, UiSkin.Cream);

            bool available = voice != null && voice.Available;
            bool on = available && voice.MicOn;
            Color micColor = !available ? UiSkin.Muted : on ? UiSkin.Green : UiSkin.Red;
            UiSkin.Image(new Rect(bar.xMax - 186f, bar.y + 9f, 26f, 26f), UiSkin.MicIcon, micColor);
            UiSkin.Heading(new Rect(bar.xMax - 154f, bar.y, 140f, bar.height), !available ? "VOICE OFF" : on ? "MIC ON (M)" : "MUTED (M)", 20, micColor);

            if (voice == null) return;
            float y = bar.yMax + 8f;
            foreach (var p in session.Peers)
            {
                if (p.Id == session.LocalId || !voice.IsTalking(p.Id)) continue;
                var row = new Rect(W - 440f, y, 420f, 38f);
                UiSkin.Pill(row, UiSkin.PillDark);
                UiSkin.Image(new Rect(row.x + 12f, row.y + 7f, 24f, 24f), UiSkin.MicIcon,
                    MatchController.HumanColors[p.Color % MatchController.HumanColors.Length]);
                UiSkin.Text(new Rect(row.x + 44f, row.y, row.width - 56f, row.height), p.Name + " is talking", UiSkin.BodyBold, 19,
                    UiSkin.Cream, TextAnchor.MiddleLeft, false);
                y += 44f;
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        static string Dots() => new string('.', 1 + (int)(Time.unscaledTime * 2f) % 3);

        string CleanName()
        {
            string n = (nameField ?? "").Trim();
            if (n.Length == 0) n = "Dummy";
            return n.Length > 20 ? n.Substring(0, 20) : n;
        }

        int Port() => int.TryParse(portField, out int p) && p > 0 && p < 65536 ? p : NetProtocol.DirectPort;

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
