#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
#define KR_STEAM
#endif

using System;
using UnityEngine;
#if KR_STEAM
using Steamworks;
#endif

namespace KnockbackRumble
{
    /// <summary>
    /// Starts Steam, runs its callbacks, and wraps the lobby features we use:
    /// create a friends-only lobby, join one, invite friends, and notice people leaving.
    /// Uses Valve's free test app id (480, "Spacewar") until the game has its own.
    /// If Steam isn't running, Ready stays false and the game still works offline / direct.
    /// </summary>
    public class SteamService : MonoBehaviour
    {
        public const uint AppId = 480;

        public static SteamService Instance { get; private set; }

        public bool Ready { get; private set; }
        public string InitError { get; private set; }
        public ulong LocalId { get; private set; }
        public string PersonaName { get; private set; } = "";
        public ulong CurrentLobby { get; private set; }

        /// <summary>A lobby we were invited to (Steam overlay "Join Game" or launch arguments).</summary>
        public ulong PendingJoinLobby { get; set; }

        public event Action<ulong> LobbyCreated;          // lobby id, 0 on failure
        public event Action<ulong, ulong> LobbyJoined;    // lobby id + owner id, zeros on failure
        public event Action<ulong> MemberLeft;            // steam id

        public static SteamService Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SteamService");
            return go.AddComponent<SteamService>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();
            ReadLaunchArguments();
        }

        void ReadLaunchArguments()
        {
            // Steam launches the game with "+connect_lobby <id>" when you accept an invite while it's closed.
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "+connect_lobby" && ulong.TryParse(args[i + 1], out ulong id))
                    PendingJoinLobby = id;
        }

#if KR_STEAM
        CallResult<LobbyCreated_t> lobbyCreatedResult;
        CallResult<LobbyEnter_t> lobbyEnterResult;
        Callback<GameLobbyJoinRequested_t> joinRequested;
        Callback<LobbyChatUpdate_t> chatUpdate;

        void Init()
        {
            try
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SteamAppId")))
                {
                    Environment.SetEnvironmentVariable("SteamAppId", AppId.ToString());
                    Environment.SetEnvironmentVariable("SteamGameId", AppId.ToString());
                }

                var result = SteamAPI.InitEx(out string error);
                if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    InitError = "Steam isn't running (" + (string.IsNullOrEmpty(error) ? result.ToString() : error) + ")";
                    Debug.LogWarning("[Knockback Rumble] " + InitError);
                    return;
                }

                Ready = true;
                LocalId = SteamUser.GetSteamID().m_SteamID;
                PersonaName = SteamFriends.GetPersonaName();

                lobbyCreatedResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
                lobbyEnterResult = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
                joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
                chatUpdate = Callback<LobbyChatUpdate_t>.Create(OnChatUpdate);
                Debug.Log("[Knockback Rumble] Steam ready as " + PersonaName);
            }
            catch (Exception e)
            {
                InitError = "Couldn't start Steam: " + e.Message;
                Debug.LogWarning("[Knockback Rumble] " + InitError);
            }
        }

        void Update()
        {
            if (Ready) SteamAPI.RunCallbacks();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (!Ready) return;
            LeaveLobby();
            SteamAPI.Shutdown();
            Ready = false;
        }

        public void CreateLobby()
        {
            if (!Ready) { LobbyCreated?.Invoke(0); return; }
            LeaveLobby();
            lobbyCreatedResult.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, NetProtocol.MaxPlayers));
        }

        void OnLobbyCreated(LobbyCreated_t r, bool ioFailure)
        {
            if (ioFailure || r.m_eResult != EResult.k_EResultOK)
            {
                LobbyCreated?.Invoke(0);
                return;
            }
            CurrentLobby = r.m_ulSteamIDLobby;
            var lobby = new CSteamID(CurrentLobby);
            SteamMatchmaking.SetLobbyData(lobby, "game", "knockbackrumble");
            SteamMatchmaking.SetLobbyData(lobby, "version", NetProtocol.Version.ToString());
            SteamMatchmaking.SetLobbyData(lobby, "host", PersonaName);
            LobbyCreated?.Invoke(CurrentLobby);
        }

        public void JoinLobby(ulong lobbyId)
        {
            if (!Ready) { LobbyJoined?.Invoke(0, 0); return; }
            LeaveLobby();
            lobbyEnterResult.Set(SteamMatchmaking.JoinLobby(new CSteamID(lobbyId)));
        }

        void OnLobbyEntered(LobbyEnter_t r, bool ioFailure)
        {
            if (ioFailure || r.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                LobbyJoined?.Invoke(0, 0);
                return;
            }
            CurrentLobby = r.m_ulSteamIDLobby;
            ulong owner = SteamMatchmaking.GetLobbyOwner(new CSteamID(CurrentLobby)).m_SteamID;
            LobbyJoined?.Invoke(CurrentLobby, owner);
        }

        void OnJoinRequested(GameLobbyJoinRequested_t r) => PendingJoinLobby = r.m_steamIDLobby.m_SteamID;

        void OnChatUpdate(LobbyChatUpdate_t u)
        {
            if (u.m_ulSteamIDLobby != CurrentLobby) return;
            const uint gone = (uint)(EChatMemberStateChange.k_EChatMemberStateChangeLeft |
                                     EChatMemberStateChange.k_EChatMemberStateChangeDisconnected |
                                     EChatMemberStateChange.k_EChatMemberStateChangeKicked |
                                     EChatMemberStateChange.k_EChatMemberStateChangeBanned);
            if ((u.m_rgfChatMemberStateChange & gone) != 0) MemberLeft?.Invoke(u.m_ulSteamIDUserChanged);
        }

        public void LeaveLobby()
        {
            if (!Ready || CurrentLobby == 0) return;
            SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobby));
            CurrentLobby = 0;
        }

        public void OpenInviteDialog()
        {
            if (Ready && CurrentLobby != 0) SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(CurrentLobby));
        }
#else
        void Init() { InitError = "Steam isn't supported on this platform."; }
        public void CreateLobby() => LobbyCreated?.Invoke(0);
        public void JoinLobby(ulong lobbyId) => LobbyJoined?.Invoke(0, 0);
        public void LeaveLobby() { }
        public void OpenInviteDialog() { }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Unused() { MemberLeft?.Invoke(0); }
#endif
    }
}
