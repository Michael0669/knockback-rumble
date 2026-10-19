using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Main menu, lobby, pause menu and the small online status strip, drawn with IMGUI
    /// like the HUD (placeholder look until the UI art pass).
    /// </summary>
    public class MenuUI : MonoBehaviour
    {
        const float RefHeight = 1080f;

        public static bool Paused { get; private set; }

        public static void SetPaused(bool paused)
        {
            Paused = paused;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        string nameField = "";
        string ipField = "127.0.0.1";
        string portField = NetProtocol.DirectPort.ToString();
        GUIStyle title, heading, body, small, button, field, box;
        Texture2D white;

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

        void EnsureStyles()
        {
            if (title != null) return;
            white = Texture2D.whiteTexture;
            title = new GUIStyle(GUI.skin.label) { fontSize = 84, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            heading = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            body = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 26, fontStyle = FontStyle.Bold };
            field = new GUIStyle(GUI.skin.textField) { fontSize = 24, alignment = TextAnchor.MiddleLeft };
            box = new GUIStyle(GUI.skin.box);
        }

        void OnGUI()
        {
            var match = MatchController.Instance;
            var session = NetSession.Instance;
            if (match == null || session == null) return;
            EnsureStyles();

            float scale = Screen.height / RefHeight;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float W = Screen.width / scale;
            float H = RefHeight;

            if (match.InMatch)
            {
                if (Net.Online) DrawOnlineStrip(session, W);
                if (Paused) DrawPause(match, W, H);
                return;
            }

            Fill(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.45f));
            switch (session.State)
            {
                case SessionState.Connecting: DrawConnecting(session, W, H); break;
                case SessionState.Connected: DrawLobby(session, match, W, H); break;
                default: DrawMain(session, match, W, H); break;
            }
        }

        // ---------- Main menu ----------

        void DrawMain(NetSession session, MatchController match, float W, float H)
        {
            var steam = SteamService.Instance;
            bool steamReady = steam != null && steam.Ready;
            float cx = W / 2f;

            Shadowed(new Rect(0f, 70f, W, 110f), "KNOCKBACK RUMBLE", title, Palette.Gold);
            Shadowed(new Rect(0f, 175f, W, 40f), "Rusty Nutz presents: the dumbest show on television", body, Color.white);

            float y = 250f;
            Shadowed(new Rect(cx - 300f, y, 140f, 44f), "Name", body, Color.white);
            nameField = GUI.TextField(new Rect(cx - 160f, y, 460f, 44f), nameField, 20, field);
            y += 70f;

            if (GUI.Button(new Rect(cx - 300f, y, 600f, 64f), "PLAY OFFLINE  (vs bots)", button))
            {
                match.OfflineName = CleanName();
                match.StartOffline();
            }
            y += 80f;

            GUI.enabled = steamReady;
            if (GUI.Button(new Rect(cx - 300f, y, 600f, 64f), "HOST ONLINE  (Steam friends)", button))
            {
                session.LocalName = CleanName();
                session.HostSteam();
            }
            GUI.enabled = true;
            y += 72f;

            string steamNote = steamReady
                ? "To join a friend: accept their Steam invite, or right-click them in your Steam friends list and pick Join Game."
                : "Steam isn't running, so online play through Steam is off. Start Steam and restart the game. (" + (steam != null ? steam.InitError : "") + ")";
            Shadowed(new Rect(cx - 360f, y, 720f, 70f), steamNote, body, steamReady ? Color.white : new Color(1f, 0.7f, 0.5f));
            y += 100f;

            Fill(new Rect(cx - 360f, y, 720f, 2f), new Color(1f, 1f, 1f, 0.3f));
            y += 14f;
            Shadowed(new Rect(0f, y, W, 36f), "Direct connect (for testing without Steam)", heading, Color.white);
            y += 50f;

            Shadowed(new Rect(cx - 360f, y, 60f, 44f), "IP", body, Color.white);
            ipField = GUI.TextField(new Rect(cx - 300f, y, 300f, 44f), ipField, 64, field);
            Shadowed(new Rect(cx + 10f, y, 70f, 44f), "Port", body, Color.white);
            portField = GUI.TextField(new Rect(cx + 85f, y, 110f, 44f), portField, 5, field);
            y += 60f;

            if (GUI.Button(new Rect(cx - 360f, y, 350f, 56f), "HOST DIRECT", button))
            {
                session.LocalName = CleanName();
                session.HostDirect(Port());
            }
            if (GUI.Button(new Rect(cx + 10f, y, 350f, 56f), "JOIN DIRECT", button))
            {
                session.LocalName = CleanName();
                session.JoinDirect(ipField.Trim(), Port());
            }
            y += 70f;

            if (!string.IsNullOrEmpty(session.LastError))
                Shadowed(new Rect(cx - 400f, y, 800f, 60f), session.LastError, body, new Color(1f, 0.45f, 0.4f));

            if (GUI.Button(new Rect(W - 180f, H - 80f, 150f, 50f), "QUIT", button)) Quit();
        }

        // ---------- Connecting ----------

        void DrawConnecting(NetSession session, float W, float H)
        {
            Shadowed(new Rect(0f, H * 0.38f, W, 60f), session.IsHost ? "Creating lobby..." : "Connecting...", heading, Color.white);
            if (GUI.Button(new Rect(W / 2f - 150f, H * 0.38f + 90f, 300f, 60f), "CANCEL", button)) session.Leave(null);
        }

        // ---------- Lobby ----------

        void DrawLobby(NetSession session, MatchController match, float W, float H)
        {
            float cx = W / 2f;
            Shadowed(new Rect(0f, 60f, W, 70f), "LOBBY", title, Palette.Gold);
            string how = session.UsingSteam ? "Steam lobby (friends only)" : "Direct connect  -  port " + Port();
            Shadowed(new Rect(0f, 140f, W, 36f), how, body, Color.white);

            float y = 200f;
            Fill(new Rect(cx - 360f, y, 720f, 50f + session.Peers.Count * 52f), new Color(0f, 0f, 0f, 0.5f));
            y += 10f;
            Shadowed(new Rect(cx - 340f, y, 680f, 34f), "PLAYERS  " + session.Peers.Count + " / " + NetProtocol.MaxPlayers, heading, Color.white);
            y += 44f;
            foreach (var p in session.Peers)
            {
                var col = MatchController.HumanColors[p.Color % MatchController.HumanColors.Length];
                Fill(new Rect(cx - 330f, y + 10f, 24f, 24f), col);
                string label = p.Name + (p.IsHost ? "   (host)" : "") + (p.Id == session.LocalId ? "   (you)" : "");
                if (NetVoice.Instance != null && NetVoice.Instance.IsTalking(p.Id)) label += "   [talking]";
                Shadowed(new Rect(cx - 290f, y, 600f, 44f), label, small, Color.white);
                y += 52f;
            }
            y += 30f;

            if (session.IsHost)
            {
                int bots = Mathf.Max(0, MatchController.RaceSize - session.Peers.Count);
                match.FillWithBots = GUI.Toggle(new Rect(cx - 300f, y, 600f, 40f), match.FillWithBots,
                    "  Fill empty spots with bots (" + bots + " bots)", new GUIStyle(GUI.skin.toggle) { fontSize = 22 });
                y += 56f;

                if (session.UsingSteam)
                {
                    if (GUI.Button(new Rect(cx - 300f, y, 600f, 60f), "INVITE FRIENDS", button)) SteamService.Instance.OpenInviteDialog();
                    y += 74f;
                }
                if (GUI.Button(new Rect(cx - 300f, y, 600f, 70f), "START RACE", button)) match.HostStartRace();
                y += 84f;
            }
            else
            {
                Shadowed(new Rect(0f, y, W, 40f), "Waiting for the host to start the race...", body, Color.white);
                y += 56f;
            }

            if (GUI.Button(new Rect(cx - 300f, y, 600f, 56f), "LEAVE", button)) session.Leave(null);
            y += 70f;

            Shadowed(new Rect(cx - 400f, y, 800f, 70f), VoiceHint(), body, new Color(1f, 1f, 1f, 0.8f));
        }

        // ---------- In a race ----------

        void DrawPause(MatchController match, float W, float H)
        {
            Fill(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.55f));
            float cx = W / 2f;
            Shadowed(new Rect(0f, H * 0.25f, W, 80f), "PAUSED", title, Palette.Gold);
            if (Net.Online) Shadowed(new Rect(0f, H * 0.25f + 80f, W, 36f), "(the race keeps going - this is online)", body, Color.white);

            float y = H * 0.25f + 140f;
            if (GUI.Button(new Rect(cx - 220f, y, 440f, 64f), "RESUME", button)) SetPaused(false);
            y += 80f;

            if (Net.Online && Net.IsHost)
            {
                if (GUI.Button(new Rect(cx - 220f, y, 440f, 64f), "BACK TO LOBBY (everyone)", button)) match.HostBackToLobby();
                y += 80f;
            }

            string leave = !Net.Online ? "QUIT TO MENU" : (Net.IsHost ? "END GAME FOR EVERYONE" : "LEAVE GAME");
            if (GUI.Button(new Rect(cx - 220f, y, 440f, 64f), leave, button))
            {
                if (Net.Online) Net.Session.Leave(null);
                else match.EndMatch();
            }
            y += 90f;

            Shadowed(new Rect(cx - 400f, y, 800f, 60f), VoiceHint(), body, new Color(1f, 1f, 1f, 0.8f));
        }

        void DrawOnlineStrip(NetSession session, float W)
        {
            var voice = NetVoice.Instance;
            string mic = voice == null || !voice.Available ? "VOICE OFF" : (voice.MicOn ? "MIC ON (M)" : "MIC MUTED (M)");
            string text = (session.IsHost ? "HOST" : "PING " + session.PingMs + "ms") + "   " + mic;
            Shadowed(new Rect(W - 520f, 12f, 500f, 30f), text, new GUIStyle(small) { alignment = TextAnchor.MiddleRight }, Color.white);

            if (voice == null) return;
            float y = 46f;
            foreach (var p in session.Peers)
            {
                if (!voice.IsTalking(p.Id)) continue;
                Shadowed(new Rect(W - 520f, y, 500f, 28f), p.Name + " is talking", new GUIStyle(small) { alignment = TextAnchor.MiddleRight },
                    MatchController.HumanColors[p.Color % MatchController.HumanColors.Length]);
                y += 28f;
            }
        }

        // ---------- Helpers ----------

        static string VoiceHint()
        {
            var voice = NetVoice.Instance;
            if (voice == null || !voice.Available) return "Voice chat needs Steam running on every player's PC.";
            return voice.MicOn ? "Your mic is ON - everyone nearby can hear you. Press M to mute." : "Your mic is MUTED. Press M to unmute.";
        }

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

        void Shadowed(Rect r, string text, GUIStyle style, Color color)
        {
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.75f * color.a);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        void Fill(Rect r, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }
    }
}
