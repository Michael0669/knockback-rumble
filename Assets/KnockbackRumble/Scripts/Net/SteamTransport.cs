#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
#define KR_STEAM
#endif

using System;
using System.Collections.Generic;
#if KR_STEAM
using Steamworks;
#endif

namespace KnockbackRumble
{
    /// <summary>
    /// Sends packets through Steam's peer-to-peer networking (relayed by Valve, so no
    /// port forwarding). Peer ids are SteamIDs. There's no "connect" step: the host
    /// learns about a client when its Hello arrives, and Steam opens the session.
    /// </summary>
    public sealed class SteamTransport : NetTransport
    {
        readonly bool isHost;
        public override bool IsHost => isHost;
        public override string Description => "Steam";

#if KR_STEAM
        const int Channel = 0;
        byte[] buffer = new byte[16 * 1024];
        readonly HashSet<ulong> known = new HashSet<ulong>();
        Callback<P2PSessionRequest_t> sessionRequest;
        Callback<P2PSessionConnectFail_t> connectFail;

        public SteamTransport(bool host)
        {
            isHost = host;
            SteamNetworking.AllowP2PPacketRelay(true);
            sessionRequest = Callback<P2PSessionRequest_t>.Create(OnSessionRequest);
            connectFail = Callback<P2PSessionConnectFail_t>.Create(OnConnectFail);
        }

        void OnSessionRequest(P2PSessionRequest_t req)
        {
            // Only friends (or invitees) can reach the lobby, so accept everyone who asks.
            SteamNetworking.AcceptP2PSessionWithUser(req.m_steamIDRemote);
            known.Add(req.m_steamIDRemote.m_SteamID);
        }

        void OnConnectFail(P2PSessionConnectFail_t fail) => RaiseDisconnected(fail.m_steamIDRemote.m_SteamID);

        public override void Send(ulong peer, byte[] data, int length, bool reliable)
        {
            // Unreliable packets over ~1200 bytes are dropped by Steam, so send big ones reliably.
            var mode = reliable || length > 1100 ? EP2PSend.k_EP2PSendReliable : EP2PSend.k_EP2PSendUnreliable;
            known.Add(peer);
            SteamNetworking.SendP2PPacket(new CSteamID(peer), data, (uint)length, mode, Channel);
        }

        public override void Poll(Action<ulong, byte[], int> onMessage)
        {
            while (SteamNetworking.IsP2PPacketAvailable(out uint size, Channel))
            {
                if (size > buffer.Length) buffer = new byte[size];
                if (!SteamNetworking.ReadP2PPacket(buffer, (uint)buffer.Length, out uint read, out CSteamID remote, Channel)) break;
                known.Add(remote.m_SteamID);
                onMessage(remote.m_SteamID, buffer, (int)read);
            }
        }

        public override void Disconnect(ulong peer)
        {
            SteamNetworking.CloseP2PSessionWithUser(new CSteamID(peer));
            known.Remove(peer);
        }

        public override void Shutdown()
        {
            foreach (var id in known) SteamNetworking.CloseP2PSessionWithUser(new CSteamID(id));
            known.Clear();
            sessionRequest?.Dispose();
            connectFail?.Dispose();
            sessionRequest = null;
            connectFail = null;
        }
#else
        public SteamTransport(bool host)
        {
            isHost = host;
            throw new NotSupportedException("Steam isn't available on this platform.");
        }
        public override void Send(ulong peer, byte[] data, int length, bool reliable) { }
        public override void Poll(Action<ulong, byte[], int> onMessage) { }
        public override void Shutdown() { }
#endif
    }
}
