using System;

namespace KnockbackRumble
{
    /// <summary>
    /// Moves raw packets between this machine and others. Two versions:
    /// SteamTransport (real games, through Steam) and TcpTransport (direct IP, for testing).
    /// Everything is a star: clients only talk to the host; the host relays.
    /// All events are raised from Poll(), on the main thread.
    /// </summary>
    public abstract class NetTransport
    {
        public abstract bool IsHost { get; }
        public abstract string Description { get; }

        /// <summary>Host: a client connected (direct only). Client: we reached the host.</summary>
        public event Action<ulong> Connected;
        /// <summary>A connection dropped.</summary>
        public event Action<ulong> Disconnected;
        /// <summary>Client: couldn't connect at all.</summary>
        public event Action<string> Failed;

        protected void RaiseConnected(ulong peer) => Connected?.Invoke(peer);
        protected void RaiseDisconnected(ulong peer) => Disconnected?.Invoke(peer);
        protected void RaiseFailed(string reason) => Failed?.Invoke(reason);

        public abstract void Send(ulong peer, byte[] data, int length, bool reliable);

        /// <summary>Delivers every waiting packet as (sender, buffer, length). The buffer may be reused afterwards.</summary>
        public abstract void Poll(Action<ulong, byte[], int> onMessage);

        public virtual void Disconnect(ulong peer) { }
        public abstract void Shutdown();
    }
}
