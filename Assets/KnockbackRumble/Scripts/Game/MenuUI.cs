using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Main menu, lobby ("The Green Room"), connecting screen, pause menu ("Commercial
    /// Break"), the LOADOUT screen, and the small online status strip shown during shows.
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
        bool showVolumes;   // the VOICE VOLUMES pop-up
        bool showLoadout;   // the LOADOUT screen
        int loadoutSlot;    // which slot the LOADOUT screen is editing
        Loadout editing;
        Screen lastScreen = Screen.Main;
        float enteredAt;   // when the current screen appeared (drives the slide-in)

        static float Now => Time.unscaledTime;

        /// <summary>Gentle pulse for the button we want people to press, e.g. 1.0 to 1.025.</summary>
        static float Pulse(float amount = 0.025f, float speed = 3.2f) => 1f + amount * (0.5f + 0.5f * Mathf.Sin(Now * speed));

        void Start()
        {
            enteredAt = Now;
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
                // Keep the session's name in sync with the box, so joining through a Steam invite uses it too.
                var session = NetSession.Instance;
                if (session != null && session.State == SessionState.Offline) session.LocalName = CleanName();
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
                enteredAt = Now;
            }

            if (screen == Screen.Connecting) showVolumes = false;
            if (screen == Screen.Race && !Paused) { showVolumes = false; showLoadout = false; }
            if (screen == Screen.Connecting) showLoadout = false;
            bool popup = showVolumes || showLoadout;

            if (screen == Screen.Race)
            {
                if (Net.Online) DrawOnlineStrip(session, W);
                if (Paused)
                {
                    GUI.enabled = !popup;
                    DrawPause(match, W, H, oy);
                    GUI.enabled = true;
                    if (showVolumes) DrawVolumes(session, W, H, oy);
                    else if (showLoadout) DrawLoadout(W, H, oy);
                }
                return;
            }

            UiSkin.StageBackground(W, H);

            // Menus slide up and fade in when they appear.
            float intro = Mathf.Clamp01((Now - enteredAt) / 0.45f);
            var baseMatrix = GUI.matrix;
            var baseColor = GUI.color;
            GUI.matrix = baseMatrix * Matrix4x4.Translate(new Vector3(0f, (1f - UiSkin.EaseOutBack(intro)) * 60f, 0f));
            GUI.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Mathf.Clamp01(intro * 1.6f));

            GUI.enabled = !popup;
            switch (screen)
            {
                case Screen.Connecting: DrawConnecting(session, W, oy); break;
                case Screen.Lobby: DrawLobby(session, match, W, oy); break;
                default: DrawMain(session, match, W, H, oy); break;
            }
            GUI.enabled = true;

            GUI.matrix = baseMatrix;
            GUI.color = baseColor;
            if (screen == Screen.Main && !popup && string.IsNullOrEmpty(session.LastError))
                DrawProfileBar(new Rect(W - 220f - 470f, H - 102f, 470f, 92f));
            if (showVolumes) DrawVolumes(session, W, H, oy);
            else if (showLoadout) DrawLoadout(W, H, oy);
        }

        // =====================================================================
        // Main menu
        // =====================================================================

        void DrawMain(NetSession session, MatchController match, float W, float H, float oy)
        {
            var steam = SteamService.Instance;
            bool steamReady = steam != null && steam.Ready;
            float cx = W / 2f;

            DrawLogo(new Rect(cx - 430f, oy + 14f, 860f, UiSkin.HeightFor(UiSkin.Logo, 860f)));

            float px = cx - 340f, py = oy + 470f, pw = 680f, ph = 500f;
            UiSkin.Panel(new Rect(px, py, pw, ph));
            UiSkin.Heading(new Rect(px + 40f, py + 26f, 300f, 30f), "YOUR NAME", 24, UiSkin.LabelBlue);
            nameField = UiSkin.TextField(new Rect(px + 40f, py + 60f, 600f, 66f), nameField, 20, "name");

            var hostRect = new Rect(px + 40f, py + 148f, 600f, 104f);
            var before = GUI.matrix;
            if (steamReady) GUI.matrix = before * UiSkin.Around(hostRect.center, 0f, Pulse());
            bool host = UiSkin.Button(hostRect, "HOST A GAME", UiSkin.Yellow,
                steamReady ? "Invite your Steam friends" : "Needs Steam running", 44, steamReady);
            GUI.matrix = before;
            if (host)
            {
                session.LocalName = CleanName();
                session.HostSteam();
            }

            if (UiSkin.Button(new Rect(px + 40f, py + 266f, 292f, 88f), "PLAY OFFLINE", UiSkin.Dark,
                    "You vs " + match.OfflineBots + " bots", 32))
            {
                match.OfflineName = CleanName();
                match.StartOffline();
            }
            if (UiSkin.Button(new Rect(px + 348f, py + 266f, 292f, 88f), "LOADOUT", UiSkin.Teal, "Level " + Profile.Level, 32))
                OpenLoadout();

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
            if (UiSkin.Button(new Rect(W - 200f, H - 160f, 160f, 60f), "VOLUME", UiSkin.Dark, null, 26)) showVolumes = true;
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
            DrawWobbly(new Rect(cx - tw / 2f, oy + 30f, tw, UiSkin.HeightFor(UiSkin.TitleLobby, tw)), UiSkin.TitleLobby, 0.6f);
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
            string lineup = "";
            for (int i = 0; i < ShowManager.Lineup.Length; i++)
                lineup += (i > 0 ? "  >  " : "") + Events.Title(ShowManager.Lineup[i]);
            UiSkin.Text(new Rect(rx + 40f, ry + 82f, 440f, 32f), lineup, UiSkin.Body, 20, UiSkin.Cream, TextAnchor.MiddleLeft, false);
            UiSkin.Text(new Rect(rx + 40f, ry + 116f, 440f, 32f), dummies + " dummies  -  most cash wins", UiSkin.Body, 23,
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

                var startRect = new Rect(rx + 32f, ry + 392f, rw - 64f, 120f);
                var m = GUI.matrix;
                GUI.matrix = m * UiSkin.Around(startRect.center, 0f, Pulse());
                bool start = UiSkin.Button(startRect, "START THE SHOW", UiSkin.Yellow, "3 events, everyone in the lobby plays", 46);
                GUI.matrix = m;
                if (start) match.HostStartRace();
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

            DrawMicBar(new Rect(cx - 740f, oy + 884f, 860f, 56f), voice);
            if (UiSkin.Button(new Rect(cx + 140f, oy + 880f, 280f, 62f), "VOLUMES", UiSkin.Dark, null, 26)) showVolumes = true;
            if (UiSkin.Button(new Rect(cx + 440f, oy + 880f, 300f, 62f), "LOADOUT", UiSkin.Teal, null, 26)) OpenLoadout();
        }

        void DrawPlayerCard(Rect r, NetPeer p, NetSession session, NetVoice voice)
        {
            UiSkin.Card(r);
            var color = MatchController.HumanColors[p.Color % MatchController.HumanColors.Length];
            bool you = p.Id == session.LocalId;
            bool talking = voice != null && voice.IsTalking(p.Id);

            // Avatars bob when that player is talking.
            float hop = talking ? -Mathf.Abs(Mathf.Sin(Now * 12f)) * 6f : 0f;
            UiSkin.Image(new Rect(r.x + 16f, r.y + 16f + hop, 84f, 84f), UiSkin.Avatar, color);
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
                       : on ? "Everyone can hear you. M or click to mute."
                       : "Nobody can hear you. M or click to unmute.";
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
            float ph = 84f + 108f + 88f + (online && host ? 100f : 0f) + 88f + 92f + 56f;
            UiSkin.Panel(new Rect(px, py, pw, ph));
            UiSkin.Text(new Rect(px, py + 26f, pw, 34f), online ? "The show keeps going while you're in here!" : "Take a breather.",
                UiSkin.Body, 23, UiSkin.Cream, TextAnchor.MiddleCenter, false);

            float y = py + 84f;
            if (UiSkin.Button(new Rect(px + 40f, y, pw - 80f, 92f), "RESUME", UiSkin.Yellow, null, 40)) SetPaused(false);
            y += 108f;

            if (UiSkin.Button(new Rect(px + 40f, y, pw - 80f, 76f), "LOADOUT", UiSkin.Teal, null, 30)) OpenLoadout();
            y += 88f;

            if (online && host)
            {
                if (UiSkin.Button(new Rect(px + 40f, y, pw - 80f, 88f), "BACK TO LOBBY", UiSkin.Teal, "Everyone returns to the green room", 30))
                    match.HostBackToLobby();
                y += 100f;
            }

            if (UiSkin.Button(new Rect(px + 40f, y, pw - 80f, 76f), "VOLUME", UiSkin.Dark, null, 30)) showVolumes = true;
            y += 88f;

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

        /// <summary>Pop-up with music / effects sliders, an "all voices" slider and one per other player.</summary>
        void DrawVolumes(NetSession session, float W, float H, float oy)
        {
            var voice = NetVoice.Instance;
            UiSkin.Fill(new Rect(0f, 0f, W, H), new Color(0.03f, 0.02f, 0.06f, 0.55f));

            int others = 0;
            if (session != null)
                foreach (var p in session.Peers) if (p.Id != session.LocalId) others++;
            bool online = Net.Online;

            float cx = W / 2f, pw = 720f;
            float ph = 140f + 2f * 78f + (online ? 60f + 92f + Mathf.Max(1, others) * 78f : 0f) + 100f;
            var panel = new Rect(cx - pw / 2f, oy + Mathf.Max(60f, (1080f - ph) / 2f), pw, ph);
            UiSkin.Panel(panel);
            UiSkin.Heading(new Rect(panel.x, panel.y + 26f, pw, 50f), "VOLUME", 42, UiSkin.Gold, TextAnchor.MiddleCenter);
            UiSkin.Text(new Rect(panel.x, panel.y + 80f, pw, 30f), "Saved for next time.", UiSkin.BodyBold, 20, UiSkin.Muted,
                TextAnchor.MiddleCenter, false);

            float y = panel.y + 130f;
            VolumeRow(new Rect(panel.x + 40f, y, pw - 80f, 66f), null, "MUSIC", UiSkin.Teal, Sfx.MusicVolume, v => Sfx.MusicVolume = v, false, 1f);
            y += 78f;
            VolumeRow(new Rect(panel.x + 40f, y, pw - 80f, 66f), null, "EFFECTS", UiSkin.Teal, Sfx.EffectsVolume, v => Sfx.EffectsVolume = v, false, 1f);
            y += 78f;

            if (online)
            {
                UiSkin.Heading(new Rect(panel.x + 40f, y + 10f, pw - 80f, 40f), "VOICE CHAT", 28, UiSkin.LabelBlue);
                y += 60f;
                if (voice == null || !voice.Available)
                {
                    UiSkin.Text(new Rect(panel.x, y, pw, 60f), "Voice chat needs Steam running on every player's PC.", UiSkin.Body, 22,
                        UiSkin.Cream, TextAnchor.MiddleCenter, false);
                }
                else
                {
                    VolumeRow(new Rect(panel.x + 40f, y, pw - 80f, 70f), null, "ALL VOICES", UiSkin.Gold, voice.MasterVolume,
                        v => voice.SetMasterVolume(v), false);
                    y += 92f;
                    foreach (var p in session.Peers)
                    {
                        if (p.Id == session.LocalId) continue;
                        var color = MatchController.HumanColors[p.Color % MatchController.HumanColors.Length];
                        ulong id = p.Id;
                        VolumeRow(new Rect(panel.x + 40f, y, pw - 80f, 66f), UiSkin.Avatar, p.Name, color, voice.GetVolume(id),
                            v => voice.SetVolume(id, v), voice.IsTalking(id));
                        y += 78f;
                    }
                    if (others == 0)
                        UiSkin.Text(new Rect(panel.x, y, pw, 60f), "Nobody else is here yet.", UiSkin.Body, 22, UiSkin.Muted,
                            TextAnchor.MiddleCenter, false);
                }
            }

            if (UiSkin.Button(new Rect(cx - 150f, panel.yMax - 92f, 300f, 66f), "DONE", UiSkin.Yellow, null, 30)) showVolumes = false;
        }

        static void VolumeRow(Rect r, Texture2D icon, string label, Color color, float value, System.Action<float> set, bool talking,
            float max = -1f)
        {
            if (max <= 0f) max = NetVoice.MaxVolume;
            UiSkin.Card(r);
            float x = r.x + 18f;
            if (icon != null)
            {
                float hop = talking ? -Mathf.Abs(Mathf.Sin(Now * 12f)) * 4f : 0f;
                UiSkin.Image(new Rect(x, r.y + (r.height - 46f) / 2f + hop, 46f, 46f), icon, color);
                x += 58f;
            }
            UiSkin.Heading(new Rect(x, r.y, 220f, r.height), label, 26, icon == null ? color : UiSkin.Cream);

            var slider = new Rect(r.x + 290f, r.y + (r.height - 30f) / 2f, r.width - 290f - 110f, 30f);
            float nv = UiSkin.Slider(slider, value, 0f, max, value <= 0.001f ? UiSkin.Red : UiSkin.Green);
            if (!Mathf.Approximately(nv, value)) set(nv);

            string pct = nv <= 0.001f ? "OFF" : Mathf.RoundToInt(nv * 100f) + "%";
            UiSkin.Heading(new Rect(r.xMax - 100f, r.y, 84f, r.height), pct, 24, nv <= 0.001f ? UiSkin.Red : UiSkin.Cream, TextAnchor.MiddleRight);
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
        // Loadout and profile
        // =====================================================================

        static readonly string[] SlotNames = { "PRIMARY", "GADGET", "PERK 1", "PERK 2", "PAYOUT STREAK 1", "PAYOUT STREAK 2" };

        struct Option
        {
            public byte id;
            public string name, desc;
            public int level;
            public Color color;
        }

        void OpenLoadout()
        {
            editing = Profile.Loadout.Copy();
            showLoadout = true;
            showVolumes = false;
            GUI.FocusControl(null);
        }

        /// <summary>Level, XP bar and wallet.</summary>
        static void DrawProfileBar(Rect r)
        {
            UiSkin.Panel(r);
            UiSkin.Heading(new Rect(r.x + 24f, r.y + 10f, 200f, 40f), "LEVEL " + Profile.Level, 32, UiSkin.Gold);
            UiSkin.Heading(new Rect(r.x + r.width - 230f, r.y + 10f, 206f, 40f), "WALLET " + HUD.Money(Profile.Wallet), 26, UiSkin.Green,
                TextAnchor.MiddleRight);
            float fill = Profile.XpToNext > 0 ? Mathf.Clamp01((float)Profile.Xp / Profile.XpToNext) : 1f;
            var bar = new Rect(r.x + 24f, r.y + 58f, r.width - 190f, 16f);
            UiSkin.Pill(bar, UiSkin.PillDark);
            if (fill > 0.02f) UiSkin.Pill(new Rect(bar.x, bar.y, Mathf.Max(16f, bar.width * fill), bar.height), UiSkin.Green);
            UiSkin.Text(new Rect(bar.xMax + 10f, r.y + 50f, 150f, 32f), Profile.Xp + " / " + Profile.XpToNext + " XP", UiSkin.BodyBold, 18,
                UiSkin.Muted, TextAnchor.MiddleRight, false);
        }

        Option[] OptionsFor(int slot)
        {
            var list = new System.Collections.Generic.List<Option>();
            if (slot == 0)
            {
                foreach (var w in Loadouts.Primaries)
                {
                    string extra = "Ammo " + w.ammo + (w.zoomFov <= 30f ? "   -   Scope" : "");
                    list.Add(new Option { id = w.netId, name = w.name, desc = w.description + "\n" + extra, level = w.unlockLevel, color = w.color });
                }
            }
            else
            {
                var items = slot == 1 ? Loadouts.Gadgets : slot <= 3 ? Loadouts.Perks : Loadouts.Streaks;
                if (slot == 5) list.Add(new Option { id = 0, name = "Empty", desc = "No second streak.", level = 1, color = UiSkin.Muted });
                foreach (var i in items)
                {
                    string desc = i.description + (slot == 1 ? "\n" + i.cost + " per event (refill at crates with Scavenger)" : "");
                    list.Add(new Option { id = i.id, name = i.name, desc = desc, level = i.unlockLevel, color = i.color });
                }
            }
            return list.ToArray();
        }

        byte Equipped(Loadout l, int slot)
        {
            switch (slot)
            {
                case 0: return l.primary;
                case 1: return (byte)l.gadget;
                case 2: return (byte)l.perk1;
                case 3: return (byte)l.perk2;
                case 4: return (byte)l.streak1;
                default: return (byte)l.streak2;
            }
        }

        string EquippedName(Loadout l, int slot)
        {
            byte id = Equipped(l, slot);
            foreach (var o in OptionsFor(slot)) if (o.id == id) return o.name;
            return "-";
        }

        void Equip(Loadout l, int slot, byte id)
        {
            switch (slot)
            {
                case 0: l.primary = id; break;
                case 1: l.gadget = (Gadget)id; break;
                case 2: if ((Perk)id == l.perk2) l.perk2 = l.perk1; l.perk1 = (Perk)id; break;
                case 3: if ((Perk)id == l.perk1) l.perk1 = l.perk2; l.perk2 = (Perk)id; break;
                case 4:
                    if ((Streak)id == l.streak2) l.streak2 = l.streak1;
                    l.streak1 = (Streak)id;
                    break;
                default:
                    if ((Streak)id == l.streak1) { if (l.streak2 == Streak.None) return; l.streak1 = l.streak2; }
                    l.streak2 = (Streak)id;
                    break;
            }
        }

        void DrawLoadout(float W, float H, float oy)
        {
            if (editing == null) editing = Profile.Loadout.Copy();
            UiSkin.Fill(new Rect(0f, 0f, W, H), new Color(0.03f, 0.02f, 0.06f, 0.7f));

            float cx = W / 2f, pw = 1460f, ph = 880f;
            var panel = new Rect(cx - pw / 2f, oy + (1080f - ph) / 2f, pw, ph);
            UiSkin.Panel(panel);
            UiSkin.Heading(new Rect(panel.x + 44f, panel.y + 26f, 400f, 60f), "LOADOUT", 52, UiSkin.Gold);
            UiSkin.Text(new Rect(panel.x + 46f, panel.y + 86f, 700f, 30f),
                MatchController.Instance != null && MatchController.Instance.InMatch ? "Changes kick in at the start of the next event."
                    : "Level up by playing shows to unlock more.", UiSkin.BodyBold, 20, UiSkin.Muted, TextAnchor.MiddleLeft, false);
            DrawProfileBar(new Rect(panel.xMax - 520f, panel.y + 26f, 480f, 92f));

            // Slots
            float sx = panel.x + 40f, sy = panel.y + 140f, sw = 400f, sh = 100f;
            for (int i = 0; i < SlotNames.Length; i++)
            {
                var r = new Rect(sx, sy + i * (sh + 12f), sw, sh);
                bool sel = i == loadoutSlot;
                if (UiSkin.Button(r, "", sel ? UiSkin.Yellow : UiSkin.Dark, null, 10)) loadoutSlot = i;
                Color ink = sel ? UiSkin.Ink : UiSkin.Cream;
                UiSkin.Text(new Rect(r.x + 26f, r.y + 12f, r.width - 40f, 30f), SlotNames[i], UiSkin.BodyBold, 19,
                    new Color(ink.r, ink.g, ink.b, 0.75f), TextAnchor.MiddleLeft, false);
                UiSkin.Text(new Rect(r.x + 26f, r.y + 40f, r.width - 40f, 44f), EquippedName(editing, i).ToUpperInvariant(), UiSkin.Display, 32,
                    ink, TextAnchor.MiddleLeft, !sel);
            }

            // Options for the selected slot
            var options = OptionsFor(loadoutSlot);
            byte current = Equipped(editing, loadoutSlot);
            byte other = loadoutSlot == 2 ? (byte)editing.perk2 : loadoutSlot == 3 ? (byte)editing.perk1
                       : loadoutSlot == 4 ? (byte)editing.streak2 : loadoutSlot == 5 ? (byte)editing.streak1 : (byte)255;
            float ox = panel.x + 480f, oyy = panel.y + 140f, cw = 460f, chh = 150f, gap = 20f;
            int level = Profile.Level;
            for (int i = 0; i < options.Length; i++)
            {
                var o = options[i];
                var r = new Rect(ox + (i % 2) * (cw + gap), oyy + (i / 2) * (chh + gap), cw, chh);
                bool locked = o.level > level;
                bool on = o.id == current;
                UiSkin.Card(r);
                if (on) UiSkin.Pill(new Rect(r.x + 6f, r.y + 6f, r.width - 12f, r.height - 12f), new Color(1f, 0.79f, 0.16f, 0.22f));
                UiSkin.Fill(new Rect(r.x + 18f, r.y + 22f, 8f, r.height - 44f), locked ? UiSkin.Muted : o.color);
                UiSkin.Heading(new Rect(r.x + 42f, r.y + 14f, r.width - 200f, 44f), o.name.ToUpperInvariant(), 30,
                    locked ? UiSkin.Muted : UiSkin.Cream);
                var desc = new GUIStyle { font = UiSkin.BodyBold, fontSize = 19, wordWrap = true, alignment = TextAnchor.UpperLeft };
                desc.normal.textColor = locked ? new Color(0.75f, 0.72f, 0.82f, 0.7f) : UiSkin.Muted;
                GUI.Label(new Rect(r.x + 42f, r.y + 60f, r.width - 64f, r.height - 70f), o.desc, desc);

                if (locked) UiSkin.Badge(r.xMax - 170f, r.y + 18f, "LEVEL " + o.level, UiSkin.Red);
                else if (on) UiSkin.Badge(r.xMax - 150f, r.y + 18f, "EQUIPPED", UiSkin.Green);
                else if (o.id == other && o.id != 0) UiSkin.Badge(r.xMax - 150f, r.y + 18f, "OTHER SLOT", UiSkin.Teal);

                if (!locked && !on && GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    Equip(editing, loadoutSlot, o.id);
                    Sfx.Ui("ui_equip");
                    Profile.SetLoadout(editing);
                    editing = Profile.Loadout.Copy();
                }
            }

            if (UiSkin.Button(new Rect(panel.xMax - 340f, panel.yMax - 112f, 300f, 80f), "DONE", UiSkin.Yellow, null, 36))
            {
                Profile.SetLoadout(editing);
                showLoadout = false;
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        /// <summary>The logo pops in, then gently bobs, sways and breathes.</summary>
        void DrawLogo(Rect r)
        {
            float pop = Mathf.Lerp(0.82f, 1f, UiSkin.EaseOutBack((Now - enteredAt) / 0.6f));
            float breathe = 1f + 0.015f * Mathf.Sin(Now * 2.1f);
            float angle = Mathf.Sin(Now * 0.9f) * 1.5f;
            r.y += Mathf.Sin(Now * 1.6f) * 6f;
            var old = GUI.matrix;
            GUI.matrix = old * UiSkin.Around(r.center, angle, pop * breathe);
            UiSkin.Image(r, UiSkin.Logo);
            GUI.matrix = old;
        }

        /// <summary>A title image with a slow bob and sway.</summary>
        static void DrawWobbly(Rect r, Texture2D tex, float strength)
        {
            r.y += Mathf.Sin(Now * 1.4f) * 5f * strength;
            var old = GUI.matrix;
            GUI.matrix = old * UiSkin.Around(r.center, Mathf.Sin(Now * 0.8f) * 1.5f * strength, 1f + 0.012f * Mathf.Sin(Now * 2f));
            UiSkin.Image(r, tex);
            GUI.matrix = old;
        }

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
