using System;
using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    public enum SessionState { Offline, Connecting, Connected }

    public class NetPeer
    {
        public ulong Id;
        public string Name;
        public byte Color;
        public bool IsHost;
        public float LastHeard;
    }

    /// <summary>Shortcuts the gameplay code uses. Offline, this machine is the host of everything.</summary>
    public static class Net
    {
        public static NetSession Session => NetSession.Instance;
        public static bool Online => Session != null && Session.State == SessionState.Connected;
        public static bool IsHost => !Online || Session.IsHost;
        public static ulong LocalId => Online ? Session.LocalId : 1UL;

        /// <summary>Shared game clock: the host's Time.time, estimated on clients. Hazards run on this.</summary>
        public static float Time => UnityEngine.Time.time + (Online && !Session.IsHost ? Session.ClockOffset : 0f);
    }

    /// <summary>
    /// The online session: hosting or joining, the lobby roster, keep-alives, clock sync,
    /// and relaying. Clients only talk to the host; the host forwards "broadcast" messages
    /// to everyone else and routes hits to whoever owns the victim. Game messages are
    /// handed to listeners through GameMessage.
    /// </summary>
    public class NetSession : MonoBehaviour
    {
        public static NetSession Instance { get; private set; }

        const float Timeout = 15f;
        const float ConnectTimeout = 20f;

        public SessionState State { get; private set; } = SessionState.Offline;
        public bool IsHost { get; private set; }
        public bool UsingSteam { get; private set; }
        public ulong LocalId { get; private set; }
        public ulong HostId { get; private set; }
        public float ClockOffset { get; private set; }
        public int PingMs { get; private set; }
        public string LastError { get; set; }
        public string LocalName = "Dummy";

        /// <summary>Host's "fill empty spots with bots" setting (shared so everyone's lobby shows it).</summary>
        public bool FillBots { get; private set; } = true;

        public void SetFillBots(bool on)
        {
            if (FillBots == on) return;
            FillBots = on;
            if (IsHost && State == SessionState.Connected) BroadcastRoster();
        }
        public readonly List<NetPeer> Peers = new List<NetPeer>();

        /// <summary>(sender, type, reader) for every match message.</summary>
        public event Action<ulong, MsgType, NetReader> GameMessage;
        public event Action RosterChanged;
        public event Action<ulong> PeerLeft;
        public event Action SessionEnded;

        /// <summary>Who owns a dummy (by net id). Set by the match so hits can be routed.</summary>
        public Func<byte, ulong> OwnerOf;

        NetTransport transport;
        readonly NetWriter writer = new NetWriter();
        float nextPing, nextHello, hostLastHeard, connectStarted;
        bool gotClock;

        public static NetSession Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("NetSession");
            DontDestroyOnLoad(go);
            return go.AddComponent<NetSession>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            if (State != SessionState.Offline) Leave(null);
            Instance = null;
        }

        void OnApplicationQuit()
        {
            if (State != SessionState.Offline) Leave(null);
        }

        public NetPeer FindPeer(ulong id) => Peers.Find(p => p.Id == id);

        /// <summary>The name we show others: what's typed in the menu, or the Steam name if that's blank / the default.</summary>
        string DisplayName()
        {
            string n = (LocalName ?? "").Trim();
            var steam = SteamService.Instance;
            if ((n.Length == 0 || n == "Dummy") && steam != null && steam.Ready && !string.IsNullOrEmpty(steam.PersonaName))
                n = steam.PersonaName;
            return Clean(n);
        }

        // =====================================================================
        // Starting and stopping
        // =====================================================================

        public void HostSteam()
        {
            var steam = SteamService.Instance;
            if (steam == null || !steam.Ready) { LastError = "Steam isn't running."; return; }
            BeginConnecting(true);
            steam.LobbyCreated += OnSteamLobbyCreated;
            steam.CreateLobby();
        }

        void OnSteamLobbyCreated(ulong lobby)
        {
            var steam = SteamService.Instance;
            steam.LobbyCreated -= OnSteamLobbyCreated;
            if (State != SessionState.Connecting) return; // cancelled
            if (lobby == 0) { Leave("Steam couldn't create a lobby."); return; }
            UsingSteam = true;
            steam.MemberLeft += OnSteamMemberLeft;
            StartHosting(new SteamTransport(true), steam.LocalId);
        }

        public void HostDirect(int port)
        {
            if (State != SessionState.Offline) Leave(null);
            try
            {
                StartHosting(TcpTransport.Host(port), TcpTransport.HostPeerId);
            }
            catch (Exception e)
            {
                Leave("Couldn't host on port " + port + ": " + e.Message);
            }
        }

        void StartHosting(NetTransport t, ulong localId)
        {
            transport = t;
            HookTransport();
            IsHost = true;
            LocalId = HostId = localId;
            ClockOffset = 0f;
            Peers.Clear();
            Peers.Add(new NetPeer { Id = localId, Name = DisplayName(), Color = 0, IsHost = true, LastHeard = Time.unscaledTime });
            State = SessionState.Connected;
            LastError = null;
            RosterChanged?.Invoke();
        }

        public void JoinSteam(ulong lobbyId)
        {
            var steam = SteamService.Instance;
            if (steam == null || !steam.Ready) { LastError = "Steam isn't running."; return; }
            BeginConnecting(false);
            steam.LobbyJoined += OnSteamLobbyJoined;
            steam.JoinLobby(lobbyId);
        }

        void OnSteamLobbyJoined(ulong lobby, ulong owner)
        {
            var steam = SteamService.Instance;
            steam.LobbyJoined -= OnSteamLobbyJoined;
            if (State != SessionState.Connecting) return;
            if (lobby == 0 || owner == 0) { Leave("Couldn't join that lobby (it may be full or closed)."); return; }
            if (owner == steam.LocalId) { Leave("That's your own lobby."); return; }
            UsingSteam = true;
            steam.MemberLeft += OnSteamMemberLeft;
            transport = new SteamTransport(false);
            HookTransport();
            HostId = owner;
            nextHello = 0f; // say hello right away
        }

        public void JoinDirect(string address, int port)
        {
            BeginConnecting(false);
            HostId = TcpTransport.HostPeerId;
            transport = TcpTransport.Connect(address, port);
            HookTransport();
            nextHello = float.MaxValue; // wait for the socket to connect first
        }

        void BeginConnecting(bool host)
        {
            if (State != SessionState.Offline) Leave(null);
            State = SessionState.Connecting;
            IsHost = host;
            LastError = null;
            gotClock = false;
            ClockOffset = 0f;
            connectStarted = hostLastHeard = Time.unscaledTime;
        }

        void HookTransport()
        {
            transport.Connected += OnTransportConnected;
            transport.Disconnected += OnTransportDisconnected;
            transport.Failed += OnTransportFailed;
        }

        void OnTransportConnected(ulong peer)
        {
            // Client (direct): the socket is open, introduce ourselves.
            if (!IsHost && State == SessionState.Connecting) nextHello = 0f;
        }

        void OnTransportDisconnected(ulong peer)
        {
            if (IsHost) RemovePeer(peer);
            else if (peer == HostId) Leave(State == SessionState.Connecting ? "Couldn't reach the host." : "Lost connection to the host.");
        }

        void OnTransportFailed(string reason) => Leave(reason);

        void OnSteamMemberLeft(ulong steamId)
        {
            if (IsHost) RemovePeer(steamId);
            else if (steamId == HostId) Leave("The host left.");
        }

        /// <summary>Leave the session (or cancel connecting). reason is shown on the menu; null for a normal exit.</summary>
        public void Leave(string reason)
        {
            if (transport != null)
            {
                var t = transport;
                transport = null;
                try
                {
                    if (State == SessionState.Connected)
                    {
                        var w = writer.Begin(MsgType.Bye);
                        if (IsHost) { foreach (var p in Peers) if (p.Id != LocalId) t.Send(p.Id, w.Buffer, w.Length, true); }
                        else t.Send(HostId, w.Buffer, w.Length, true);
                    }
                    t.Shutdown();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Knockback Rumble] Error while disconnecting: " + e.Message);
                }
            }

            var steam = SteamService.Instance;
            if (steam != null)
            {
                steam.LobbyCreated -= OnSteamLobbyCreated;
                steam.LobbyJoined -= OnSteamLobbyJoined;
                steam.MemberLeft -= OnSteamMemberLeft;
                if (UsingSteam) steam.LeaveLobby();
            }

            bool wasActive = State != SessionState.Offline;
            State = SessionState.Offline;
            IsHost = false;
            UsingSteam = false;
            LocalId = HostId = 0;
            ClockOffset = 0f;
            gotClock = false;
            PingMs = 0;
            Peers.Clear();
            LastError = reason;
            if (wasActive)
            {
                RosterChanged?.Invoke();
                SessionEnded?.Invoke();
            }
        }

        // =====================================================================
        // Sending
        // =====================================================================

        /// <summary>Host: to every client. Client: to the host (which relays broadcast messages).</summary>
        public void Send(NetWriter w)
        {
            if (transport == null) return;
            bool reliable = NetProtocol.IsReliable(w.Type);
            if (IsHost)
            {
                foreach (var p in Peers)
                    if (p.Id != LocalId) transport.Send(p.Id, w.Buffer, w.Length, reliable);
            }
            else
            {
                transport.Send(HostId, w.Buffer, w.Length, reliable);
            }
        }

        /// <summary>To one machine (the host can reach anyone; a client can only reach the host).</summary>
        public void SendTo(ulong peer, NetWriter w)
        {
            if (transport == null) return;
            transport.Send(peer, w.Buffer, w.Length, NetProtocol.IsReliable(w.Type));
        }

        void Relay(byte[] data, int length, bool reliable, ulong except)
        {
            foreach (var p in Peers)
                if (p.Id != LocalId && p.Id != except) transport.Send(p.Id, data, length, reliable);
        }

        // =====================================================================
        // Receiving
        // =====================================================================

        void Update()
        {
            CheckPendingSteamJoin();
            if (transport == null) return;

            transport.Poll(OnPacket);
            if (transport == null) return; // a packet ended the session

            float now = Time.unscaledTime;
            if (IsHost)
            {
                for (int i = Peers.Count - 1; i >= 0; i--)
                {
                    var p = Peers[i];
                    if (p.Id != LocalId && now - p.LastHeard > Timeout) RemovePeer(p.Id);
                }
                return;
            }

            if (State == SessionState.Connecting)
            {
                if (now >= nextHello)
                {
                    SendTo(HostId, writer.Begin(MsgType.Hello).Int(NetProtocol.Version).Str(DisplayName()));
                    nextHello = now + 1f;
                }
                if (now - connectStarted > ConnectTimeout) { Leave("Couldn't reach the host."); return; }
            }
            else
            {
                if (now >= nextPing)
                {
                    SendTo(HostId, writer.Begin(MsgType.Ping).Float(Time.time));
                    nextPing = now + 1f;
                }
                if (now - hostLastHeard > Timeout) Leave("Lost connection to the host.");
            }
        }

        void CheckPendingSteamJoin()
        {
            var steam = SteamService.Instance;
            if (steam == null || steam.PendingJoinLobby == 0) return;
            if (State == SessionState.Connecting) return;
            ulong lobby = steam.PendingJoinLobby;
            steam.PendingJoinLobby = 0;
            if (lobby == steam.CurrentLobby) return; // already there
            if (State != SessionState.Offline) Leave(null);
            if (MatchController.Instance != null) MatchController.Instance.EndMatch();
            JoinSteam(lobby);
        }

        void OnPacket(ulong from, byte[] data, int length)
        {
            if (length < 1 || transport == null) return;
            var type = (MsgType)data[0];
            try
            {
                if (IsHost) HostReceive(from, type, data, length);
                else ClientReceive(from, type, data, length);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Knockback Rumble] Bad " + type + " packet from " + from + ": " + e.Message);
            }
        }

        void HostReceive(ulong from, MsgType type, byte[] data, int length)
        {
            if (type == MsgType.Hello) { HandleHello(from, new NetReader(data, length)); return; }

            var peer = FindPeer(from);
            if (peer == null || from == LocalId) return; // not introduced yet
            peer.LastHeard = Time.unscaledTime;

            switch (type)
            {
                case MsgType.Ping:
                {
                    var r = new NetReader(data, length);
                    float clientTime = r.Float();
                    SendTo(from, writer.Begin(MsgType.Pong).Float(clientTime).Float(Time.time));
                    return;
                }
                case MsgType.Bye:
                    RemovePeer(from);
                    return;
                case MsgType.Hit:
                {
                    // Route the hit to whoever owns the victim.
                    ulong owner = length > 1 && OwnerOf != null ? OwnerOf(data[1]) : LocalId;
                    if (owner != LocalId && owner != 0) { transport.Send(owner, data, length, true); return; }
                    break;
                }
            }

            if (NetProtocol.IsBroadcast(type)) Relay(data, length, NetProtocol.IsReliable(type), from);
            GameMessage?.Invoke(from, type, new NetReader(data, length));
        }

        void ClientReceive(ulong from, MsgType type, byte[] data, int length)
        {
            if (from != HostId) return;
            hostLastHeard = Time.unscaledTime;
            var r = new NetReader(data, length);

            switch (type)
            {
                case MsgType.Welcome:
                    if (State == SessionState.Connecting)
                    {
                        LocalId = r.ULong();
                        State = SessionState.Connected;
                        nextPing = 0f;
                        RosterChanged?.Invoke();
                    }
                    return;
                case MsgType.Roster:
                    ReadRoster(r);
                    return;
                case MsgType.Pong:
                {
                    float sent = r.Float();
                    float hostTime = r.Float();
                    float rtt = Mathf.Max(0f, Time.time - sent);
                    PingMs = Mathf.RoundToInt(rtt * 1000f);
                    float measured = hostTime + rtt * 0.5f - Time.time;
                    if (!gotClock) { ClockOffset = measured; gotClock = true; }
                    else ClockOffset += (measured - ClockOffset) * 0.15f;
                    return;
                }
                case MsgType.Kick:
                    Leave(r.Str());
                    return;
                case MsgType.Bye:
                    Leave("The host closed the lobby.");
                    return;
            }

            if (State != SessionState.Connected) return;
            GameMessage?.Invoke(from, type, r);
        }

        // =====================================================================
        // Lobby roster (host)
        // =====================================================================

        void HandleHello(ulong from, NetReader r)
        {
            int version = r.Int();
            string name = Clean(r.Str());

            var existing = FindPeer(from);
            if (existing == null)
            {
                if (version != NetProtocol.Version)
                {
                    SendTo(from, writer.Begin(MsgType.Kick).Str("Version mismatch. Make sure you both have the latest build."));
                    return;
                }
                if (Peers.Count >= NetProtocol.MaxPlayers)
                {
                    SendTo(from, writer.Begin(MsgType.Kick).Str("That lobby is full."));
                    return;
                }
                existing = new NetPeer { Id = from, Name = name, Color = FreeColor() };
                Peers.Add(existing);
                Debug.Log("[Knockback Rumble] " + name + " joined.");
            }
            existing.LastHeard = Time.unscaledTime;

            SendTo(from, writer.Begin(MsgType.Welcome).ULong(from));
            BroadcastRoster();
        }

        byte FreeColor()
        {
            for (byte c = 0; c < NetProtocol.MaxPlayers; c++)
                if (!Peers.Exists(p => p.Color == c)) return c;
            return 0;
        }

        static string Clean(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) name = "Dummy";
            return name.Length > 20 ? name.Substring(0, 20) : name;
        }

        void RemovePeer(ulong id)
        {
            var p = FindPeer(id);
            if (p == null || id == LocalId) return;
            Peers.Remove(p);
            transport?.Disconnect(id);
            Debug.Log("[Knockback Rumble] " + p.Name + " left.");
            BroadcastRoster();
            PeerLeft?.Invoke(id);
        }

        void BroadcastRoster()
        {
            var w = writer.Begin(MsgType.Roster).Byte((byte)Peers.Count);
            foreach (var p in Peers) w.ULong(p.Id).Str(p.Name).Byte(p.Color).Bool(p.IsHost);
            w.Bool(FillBots);
            Send(w);
            RosterChanged?.Invoke();
        }

        void ReadRoster(NetReader r)
        {
            int n = r.Byte();
            var fresh = new List<NetPeer>(n);
            for (int i = 0; i < n; i++)
                fresh.Add(new NetPeer { Id = r.ULong(), Name = r.Str(), Color = r.Byte(), IsHost = r.Bool() });
            FillBots = r.Bool();

            var gone = new List<ulong>();
            foreach (var old in Peers)
                if (!fresh.Exists(p => p.Id == old.Id)) gone.Add(old.Id);

            Peers.Clear();
            Peers.AddRange(fresh);
            RosterChanged?.Invoke();
            foreach (var id in gone) PeerLeft?.Invoke(id);
        }
    }
}
