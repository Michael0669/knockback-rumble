#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
#define KR_STEAM
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if KR_STEAM
using Steamworks;
#endif

namespace KnockbackRumble
{
    /// <summary>
    /// Open-mic proximity voice chat using Steam's voice capture and codec.
    /// Your compressed voice goes to the host, which relays it to everyone. Each
    /// player's voice plays from their dummy (louder up close); in the lobby, or if
    /// they have no dummy, it plays flat so everyone hears it. M mutes your mic.
    /// Needs Steam running on every PC (it works over direct connect too).
    /// </summary>
    public class NetVoice : MonoBehaviour
    {
        public static NetVoice Instance { get; private set; }

        public bool MicOn { get; private set; } = true;
        public bool Available => steamReady && Net.Online;

        readonly Dictionary<ulong, VoicePlayer> players = new Dictionary<ulong, VoicePlayer>();
        readonly Dictionary<ulong, float> lastTalk = new Dictionary<ulong, float>();
        readonly NetWriter writer = new NetWriter();
        bool steamReady, recording, hooked;

        public static NetVoice Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("NetVoice");
            DontDestroyOnLoad(go);
            return go.AddComponent<NetVoice>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SetRecording(false);
            var s = NetSession.Instance;
            if (s != null && hooked)
            {
                s.GameMessage -= OnGameMessage;
                s.PeerLeft -= RemovePlayer;
                s.SessionEnded -= ClearPlayers;
            }
        }

        public bool IsTalking(ulong peer) => lastTalk.TryGetValue(peer, out float t) && Time.unscaledTime - t < 0.35f;

        void Update()
        {
            var session = NetSession.Instance;
            if (session != null && !hooked)
            {
                session.GameMessage += OnGameMessage;
                session.PeerLeft += RemovePlayer;
                session.SessionEnded += ClearPlayers;
                hooked = true;
            }
            steamReady = SteamService.Instance != null && SteamService.Instance.Ready;

            var kb = Keyboard.current;
            if (kb != null && kb.mKey.wasPressedThisFrame && Net.Online) MicOn = !MicOn;

            SetRecording(Available && MicOn);
            if (recording) Capture();

            // Voices follow their dummies around the course.
            var match = MatchController.Instance;
            foreach (var kv in players)
            {
                var dummy = match != null && match.InMatch ? match.HumanFor(kv.Key) : null;
                kv.Value.Follow(dummy != null ? dummy.transform : null);
            }
        }

        void SetRecording(bool on)
        {
            if (on == recording) return;
            recording = on;
#if KR_STEAM
            if (SteamService.Instance == null || !SteamService.Instance.Ready) return; // Steam already shut down
            if (on) SteamUser.StartVoiceRecording();
            else SteamUser.StopVoiceRecording();
#endif
        }

#if KR_STEAM
        readonly byte[] compressed = new byte[16 * 1024];
        readonly byte[] pcm = new byte[256 * 1024];

        void Capture()
        {
            var result = SteamUser.GetAvailableVoice(out uint size);
            if (result != EVoiceResult.k_EVoiceResultOK || size == 0) return;
            result = SteamUser.GetVoice(true, compressed, (uint)compressed.Length, out uint written);
            if (result != EVoiceResult.k_EVoiceResultOK || written == 0) return;

            Net.Session.Send(writer.Begin(MsgType.Voice).ULong(Net.LocalId).UShort((ushort)written).Bytes(compressed, (int)written));
            lastTalk[Net.LocalId] = Time.unscaledTime;
        }

        void OnGameMessage(ulong from, MsgType type, NetReader r)
        {
            if (type != MsgType.Voice || !steamReady) return;
            ulong speaker = r.ULong();
            int length = r.UShort();
            byte[] data = r.Bytes(length);
            if (speaker == Net.LocalId || data.Length == 0) return;

            uint rate = SteamUser.GetVoiceOptimalSampleRate();
            var result = SteamUser.DecompressVoice(data, (uint)data.Length, pcm, (uint)pcm.Length, out uint pcmBytes, rate);
            if (result != EVoiceResult.k_EVoiceResultOK || pcmBytes == 0) return;

            if (!players.TryGetValue(speaker, out var player) || player == null)
            {
                var go = new GameObject("Voice_" + speaker);
                DontDestroyOnLoad(go);
                player = go.AddComponent<VoicePlayer>();
                player.Init((int)rate);
                players[speaker] = player;
            }
            player.Push(pcm, (int)pcmBytes);
            lastTalk[speaker] = Time.unscaledTime;
        }
#else
        void Capture() { }
        void OnGameMessage(ulong from, MsgType type, NetReader r) { }
#endif

        void RemovePlayer(ulong peer)
        {
            if (players.TryGetValue(peer, out var p) && p != null) Destroy(p.gameObject);
            players.Remove(peer);
            lastTalk.Remove(peer);
        }

        void ClearPlayers()
        {
            foreach (var p in players.Values) if (p != null) Destroy(p.gameObject);
            players.Clear();
            lastTalk.Clear();
        }
    }
}
