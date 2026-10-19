using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace KnockbackRumble
{
    /// <summary>
    /// Direct-IP transport over TCP, for testing without Steam (e.g. a build and the
    /// editor on one PC, or two PCs on the same network). Everything is sent reliably.
    /// Packets are framed as [int length][payload]. Socket work happens on background
    /// threads; results are queued and handed over in Poll() on the main thread.
    /// Peer ids: the host is 1, clients are 2, 3, 4...
    /// </summary>
    public sealed class TcpTransport : NetTransport
    {
        public const ulong HostPeerId = 1;
        const int MaxPacket = 1 << 20;

        enum EvtKind { Data, Connected, Disconnected, Failed }

        struct Evt
        {
            public EvtKind kind;
            public ulong peer;
            public byte[] data;
            public string error;
        }

        sealed class Conn
        {
            public ulong id;
            public TcpClient client;
            public NetworkStream stream;
            public readonly object writeLock = new object();
            public volatile bool alive = true;
        }

        readonly ConcurrentQueue<Evt> events = new ConcurrentQueue<Evt>();
        readonly Dictionary<ulong, Conn> conns = new Dictionary<ulong, Conn>();
        readonly object connLock = new object();
        readonly bool isHost;
        TcpListener listener;
        long nextId = 2;
        volatile bool running = true;

        public override bool IsHost => isHost;
        public override string Description => "Direct";

        TcpTransport(bool host) { isHost = host; }

        /// <summary>Starts listening. Throws SocketException if the port is in use.</summary>
        public static TcpTransport Host(int port)
        {
            var t = new TcpTransport(true);
            t.listener = new TcpListener(IPAddress.Any, port);
            t.listener.Start();
            var thread = new Thread(t.AcceptLoop) { IsBackground = true, Name = "KR Accept" };
            thread.Start();
            return t;
        }

        /// <summary>Starts connecting in the background; raises Connected or Failed from Poll().</summary>
        public static TcpTransport Connect(string address, int port)
        {
            var t = new TcpTransport(false);
            var thread = new Thread(() => t.ConnectLoop(address, port)) { IsBackground = true, Name = "KR Connect" };
            thread.Start();
            return t;
        }

        // ---------- Background threads ----------

        void AcceptLoop()
        {
            while (running)
            {
                TcpClient client;
                try { client = listener.AcceptTcpClient(); }
                catch { break; } // listener stopped
                ulong id = (ulong)Interlocked.Increment(ref nextId) - 1;
                StartConn(id, client);
                events.Enqueue(new Evt { kind = EvtKind.Connected, peer = id });
            }
        }

        void ConnectLoop(string address, int port)
        {
            try
            {
                var client = new TcpClient();
                var ar = client.BeginConnect(address, port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(6)))
                {
                    client.Close();
                    events.Enqueue(new Evt { kind = EvtKind.Failed, error = "No answer from " + address + ":" + port });
                    return;
                }
                client.EndConnect(ar);
                StartConn(HostPeerId, client);
                events.Enqueue(new Evt { kind = EvtKind.Connected, peer = HostPeerId });
            }
            catch (Exception e)
            {
                events.Enqueue(new Evt { kind = EvtKind.Failed, error = "Couldn't connect to " + address + ": " + e.Message });
            }
        }

        void StartConn(ulong id, TcpClient client)
        {
            client.NoDelay = true;
            var conn = new Conn { id = id, client = client, stream = client.GetStream() };
            lock (connLock) conns[id] = conn;
            var thread = new Thread(() => ReadLoop(conn)) { IsBackground = true, Name = "KR Read " + id };
            thread.Start();
        }

        void ReadLoop(Conn conn)
        {
            var header = new byte[4];
            try
            {
                while (running && conn.alive)
                {
                    if (!ReadFully(conn.stream, header, 4)) break;
                    int len = BitConverter.ToInt32(header, 0);
                    if (len <= 0 || len > MaxPacket) break;
                    var data = new byte[len];
                    if (!ReadFully(conn.stream, data, len)) break;
                    events.Enqueue(new Evt { kind = EvtKind.Data, peer = conn.id, data = data });
                }
            }
            catch { /* connection closed */ }
            Drop(conn);
        }

        static bool ReadFully(NetworkStream s, byte[] buf, int count)
        {
            int got = 0;
            while (got < count)
            {
                int n = s.Read(buf, got, count - got);
                if (n <= 0) return false;
                got += n;
            }
            return true;
        }

        void Drop(Conn conn)
        {
            bool wasAlive;
            lock (connLock)
            {
                wasAlive = conn.alive;
                conn.alive = false;
                if (conns.TryGetValue(conn.id, out var c) && c == conn) conns.Remove(conn.id);
            }
            try { conn.client.Close(); } catch { }
            if (wasAlive && running) events.Enqueue(new Evt { kind = EvtKind.Disconnected, peer = conn.id });
        }

        // ---------- Main thread ----------

        public override void Send(ulong peer, byte[] data, int length, bool reliable)
        {
            Conn conn;
            lock (connLock) conns.TryGetValue(peer, out conn);
            if (conn == null || !conn.alive) return;
            try
            {
                var header = BitConverter.GetBytes(length);
                lock (conn.writeLock)
                {
                    conn.stream.Write(header, 0, 4);
                    conn.stream.Write(data, 0, length);
                }
            }
            catch
            {
                Drop(conn);
            }
        }

        public override void Poll(Action<ulong, byte[], int> onMessage)
        {
            while (events.TryDequeue(out var e))
            {
                switch (e.kind)
                {
                    case EvtKind.Data: onMessage(e.peer, e.data, e.data.Length); break;
                    case EvtKind.Connected: RaiseConnected(e.peer); break;
                    case EvtKind.Disconnected: RaiseDisconnected(e.peer); break;
                    case EvtKind.Failed: RaiseFailed(e.error); break;
                }
                if (!running) return; // a handler shut us down
            }
        }

        public override void Disconnect(ulong peer)
        {
            Conn conn;
            lock (connLock) conns.TryGetValue(peer, out conn);
            if (conn != null) Drop(conn);
        }

        public override void Shutdown()
        {
            running = false;
            try { listener?.Stop(); } catch { }
            List<Conn> all;
            lock (connLock) { all = new List<Conn>(conns.Values); conns.Clear(); }
            foreach (var c in all)
            {
                c.alive = false;
                try { c.client.Close(); } catch { }
            }
        }
    }
}
