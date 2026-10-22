using System.IO;
using System.Text;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Every message type. The first byte of every packet is one of these.</summary>
    public enum MsgType : byte
    {
        // Session (connection bookkeeping)
        Hello = 1,      // client -> host: version, name
        Welcome = 2,    // host -> client: your peer id
        Roster = 3,     // host -> all: who's in the lobby
        Ping = 4,       // client -> host: client time
        Pong = 5,       // host -> client: client time, host time
        Bye = 6,        // either way: I'm leaving
        Kick = 7,       // host -> client: reason

        // Match, sent by the host
        StartRace = 20,
        Finished = 21,
        Results = 22,
        CrateTaken = 23,
        CrateBack = 24,
        BackToLobby = 25,
        CashDelta = 26,     // cash changes: count, then (netId, amount, reason)
        Bag = 27,           // money bag moved: index, carrier netId (NoId = on the ground), position

        // Sent to the host
        FinishReq = 40,
        CrateReq = 41,
        BagReq = 42,        // "my dummy touched this bag": index, netId

        // Routed to the machine that owns the victim dummy
        Hit = 50,

        // Sent by anyone to everyone (the host relays clients' copies)
        State = 60,
        Fire = 61,
        Launched = 62,
        Fell = 63,
        Voice = 64,
        Gadget = 65,        // thrower netId, gadget, id, position, velocity
        Boom = 66,          // gadget id, position, radius
        StreakCall = 67,    // caller netId, streak, bomb count, then (position, delay) per bomb
        Loadout = 68,       // netId + 6 loadout bytes (sent by the owner at round start)
        Heckle = 69,        // from netId, target netId, kind
        Outfit = 70,        // netId + 6 cosmetic ids (sent by the owner at round start)
        Mini = 71,          // mini-game event from the host: game kind, op, payload (see MiniGame)
    }

    public static class NetProtocol
    {
        public const int Version = 4;
        public const int MaxPlayers = 8;
        public const int DirectPort = 7777;
        public const byte NoId = 255;

        public static bool IsBroadcast(MsgType t) => t >= MsgType.State;

        /// <summary>Fast-changing data (positions, voice, pings) is allowed to drop; everything else must arrive.</summary>
        public static bool IsReliable(MsgType t) =>
            !(t == MsgType.State || t == MsgType.Voice || t == MsgType.Ping || t == MsgType.Pong);
    }

    /// <summary>Builds one packet. Reuse an instance: Begin() clears it.</summary>
    public sealed class NetWriter
    {
        readonly MemoryStream ms = new MemoryStream(1024);
        readonly BinaryWriter w;

        public NetWriter() { w = new BinaryWriter(ms, Encoding.UTF8); }

        public MsgType Type { get; private set; }
        public byte[] Buffer => ms.GetBuffer();
        public int Length => (int)ms.Length;

        public NetWriter Begin(MsgType type)
        {
            ms.Position = 0;
            ms.SetLength(0);
            Type = type;
            w.Write((byte)type);
            return this;
        }

        public NetWriter Byte(byte v) { w.Write(v); return this; }
        public NetWriter Bool(bool v) { w.Write(v); return this; }
        public NetWriter UShort(ushort v) { w.Write(v); return this; }
        public NetWriter Int(int v) { w.Write(v); return this; }
        public NetWriter ULong(ulong v) { w.Write(v); return this; }
        public NetWriter Float(float v) { w.Write(v); return this; }
        public NetWriter Vec3(Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); return this; }
        public NetWriter Str(string v) { w.Write(v ?? ""); return this; }
        public NetWriter Bytes(byte[] data, int count) { w.Write(data, 0, count); return this; }
    }

    /// <summary>Reads one packet. The type byte has already been consumed.</summary>
    public sealed class NetReader
    {
        readonly BinaryReader r;
        public MsgType Type { get; }

        public NetReader(byte[] data, int length)
        {
            r = new BinaryReader(new MemoryStream(data, 0, length, false), Encoding.UTF8);
            Type = (MsgType)r.ReadByte();
        }

        public byte Byte() => r.ReadByte();
        public bool Bool() => r.ReadBoolean();
        public ushort UShort() => r.ReadUInt16();
        public int Int() => r.ReadInt32();
        public ulong ULong() => r.ReadUInt64();
        public float Float() => r.ReadSingle();
        public Vector3 Vec3() => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        public string Str() => r.ReadString();
        public byte[] Bytes(int count) => r.ReadBytes(count);
    }
}
