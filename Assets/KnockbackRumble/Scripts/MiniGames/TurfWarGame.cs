using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// TURF WAR: run over floor tiles to paint them your color. Every tile you own at the end pays.
    /// The host works out who painted what from everyone's positions and sends the changes.
    /// </summary>
    public class TurfWarGame : MiniGame
    {
        const int N = 14, PaintCash = 1, TileCash = 8;
        const float Tile = 2.6f;
        const byte OpPaint = 1, NoOwner = 255;

        public override EventKind Kind => EventKind.TurfWar;
        public override string Title => "TURF WAR";
        public override string Goal => "Paint the most floor in your color.";
        public override string Objective => "Run over tiles to paint them your color. Every tile you own at the end pays $" + TileCash + ". Steal other people's tiles!";
        public override string Blurb => "Run around painting the floor your color. Steal everyone else's.";
        public override string[,] Cards => new[,]
        {
            { "$" + TileCash, "per tile you own at the end" },
            { "+$" + PaintCash, "every tile you paint" },
            { "STEAL", "other people's tiles" },
        };
        public override float Duration => 75f;
        public override string PrizeLabel => "TURF";

        readonly byte[] owner = new byte[N * N];
        Renderer[] tiles;
        Material neutral;
        float nextScan, nextSend;
        readonly List<int> pendingTiles = new List<int>();
        readonly Dictionary<Contestant, int> botTarget = new Dictionary<Contestant, int>();
        readonly Dictionary<Contestant, float> botRepick = new Dictionary<Contestant, float>();

        static float Half => N * Tile * 0.5f;

        public override void Build(Transform root, CourseInfo info)
        {
            var baseMat = MiniKit.M("Turf_Base", new Color(0.3f, 0.3f, 0.34f));
            neutral = MiniKit.M("Turf_Neutral", new Color(0.82f, 0.82f, 0.8f));
            MiniKit.Block(root, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(N * Tile, 1f, N * Tile), baseMat);
            var tileRoot = new GameObject("Tiles").transform;
            tileRoot.SetParent(root, false);
            for (int z = 0; z < N; z++)
                for (int x = 0; x < N; x++)
                    MiniKit.Block(tileRoot, "Tile", new Vector3(-Half + (x + 0.5f) * Tile, 0.01f, -Half + (z + 0.5f) * Tile), new Vector3(Tile - 0.12f, 0.02f, Tile - 0.12f),
                        neutral, false);
            MiniKit.Block(root, "Lip", new Vector3(0f, -0.12f, 0f), new Vector3(N * Tile + 0.05f, 0.2f, N * Tile + 0.05f), MiniKit.M("Stripe", Palette.Stripe), false);
            for (int i = 0; i < 4; i++)
                Kit.IBeam(root, Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(-Half + 1f, -1.5f, -Half + 1f),
                    Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(Half - 1f, -1.5f, -Half + 1f), 1f, Kit.Girder);

            // Giant paint cans (cover)
            Color[] canColors = { new Color(0.9f, 0.2f, 0.25f), new Color(0.2f, 0.6f, 0.95f), new Color(0.3f, 0.85f, 0.3f), new Color(1f, 0.8f, 0.2f) };
            for (int i = 0; i < 4; i++)
            {
                var p = Quaternion.Euler(0f, 45f + i * 90f, 0f) * new Vector3(0f, 0f, 8.5f);
                var can = MiniKit.Disc(root, "PaintCan", p + Vector3.up * 1.1f, 1.3f, 2.2f, MiniKit.M("Turf_Can", new Color(0.75f, 0.76f, 0.8f)), 16);
                LowPoly.Make(LowPoly.Cylinder(16), "Label", can.transform, Vector3.zero, new Vector3(1.02f, 0.5f, 1.02f), MiniKit.M("Turf_Can" + i, canColors[i]));
                LowPoly.Make(LowPoly.Cylinder(16), "Paint", can.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 0.04f, 0.9f), MiniKit.M("Turf_Can" + i, canColors[i]));
            }
            MiniKit.Pickup(root, new Vector3(Half - 3f, 0f, Half - 3f));
            MiniKit.Pickup(root, new Vector3(-Half + 3f, 0f, -Half + 3f));
            MiniKit.Pickup(root, new Vector3(Half - 3f, 0f, -Half + 3f));
            MiniKit.Pickup(root, new Vector3(-Half + 3f, 0f, Half - 3f));

            info.spawnPoints = MiniKit.TwoRings(Origin, 4f, 13f);
            info.center = Origin;
            info.radius = Half;
            MiniKit.Dress(root, 20f, 909, "TURF WAR", "wet paint. obviously.", "BUCKETS-R-US", new Color(0.3f, 0.25f, 0.35f));
        }

        public override void Begin()
        {
            var tileRoot = Course.transform.Find("Tiles");
            tiles = new Renderer[N * N];
            if (tileRoot != null)
                for (int i = 0; i < N * N && i < tileRoot.childCount; i++) tiles[i] = tileRoot.GetChild(i).GetComponent<Renderer>();
            if (neutral == null) neutral = MiniKit.M("Turf_Neutral", new Color(0.82f, 0.82f, 0.8f));
            for (int i = 0; i < owner.Length; i++) SetOwner(i, NoOwner, false);
            pendingTiles.Clear();
            botTarget.Clear();
            botRepick.Clear();
        }

        public override void End()
        {
            for (int i = 0; i < owner.Length; i++) SetOwner(i, NoOwner, false);
        }

        int TileAt(Vector3 world)
        {
            Vector3 l = world - Origin;
            int x = Mathf.FloorToInt((l.x + Half) / Tile), z = Mathf.FloorToInt((l.z + Half) / Tile);
            if (x < 0 || z < 0 || x >= N || z >= N) return -1;
            return z * N + x;
        }

        Vector3 TileCenter(int i) => W(-Half + (i % N + 0.5f) * Tile, 0f, -Half + (i / N + 0.5f) * Tile);

        void SetOwner(int i, byte id, bool fx)
        {
            owner[i] = id;
            if (tiles == null || tiles[i] == null) return;
            var c = id == NoOwner ? null : ById(id);
            tiles[i].sharedMaterial = c == null ? neutral : Mats.Get("Turf_" + ColorUtility.ToHtmlStringRGB(c.Color), Color.Lerp(c.Color, Color.white, 0.12f));
            if (fx && c != null)
            {
                Vector3 p = TileCenter(i) + Vector3.up * 0.2f;
                if (NearCamera(p, 30f))
                    for (int k = 0; k < 3; k++) Puffs.Spark(p, Random.onUnitSphere * 2f + Vector3.up * 2.5f, 0.14f, 0.35f, c.Color);
                if (c.IsPlayer && Random.value < 0.35f) Sfx.At("tomato", p, 0.35f, 0.2f, 20f);
            }
        }

        public override void HostFrame(RaceManager race)
        {
            if (Time.time >= nextScan)
            {
                nextScan = Time.time + 0.1f;
                foreach (var c in Alive())
                {
                    Vector3 p = c.transform.position;
                    if (Mathf.Abs(p.y - Origin.y) > 0.8f) continue; // in the air
                    int i = TileAt(p);
                    if (i < 0 || owner[i] == c.NetId) continue;
                    SetOwner(i, c.NetId, true);
                    Award(c, PaintCash);
                    if (!pendingTiles.Contains(i)) pendingTiles.Add(i);
                }
            }
            if (pendingTiles.Count > 0 && Time.time >= nextSend)
            {
                nextSend = Time.time + 0.2f;
                int n = Mathf.Min(255, pendingTiles.Count);
                var w = Msg(OpPaint).Byte((byte)n);
                for (int k = 0; k < n; k++) w.Byte((byte)pendingTiles[k]).Byte(owner[pendingTiles[k]]);
                Send(w);
                pendingTiles.RemoveRange(0, n);
            }
        }

        public override void OnMessage(byte op, NetReader r)
        {
            if (op != OpPaint) return;
            int n = r.Byte();
            for (int k = 0; k < n; k++)
            {
                int i = r.Byte();
                byte id = r.Byte();
                if (i >= 0 && i < owner.Length) SetOwner(i, id, true);
            }
        }

        int CountFor(Contestant c)
        {
            int n = 0;
            for (int i = 0; i < owner.Length; i++) if (owner[i] == c.NetId) n++;
            return n;
        }

        public override void HostRoundEnd(RaceManager race)
        {
            foreach (var c in Everyone())
            {
                int n = CountFor(c);
                if (n > 0) Award(c, n * TileCash);
            }
        }

        public override string Status(RaceManager race)
        {
            Contestant best = null;
            int bestN = 0;
            foreach (var c in Everyone())
            {
                int n = CountFor(c);
                if (n > bestN) { bestN = n; best = c; }
            }
            return best == null ? "PAINT THE FLOOR!" : "MOST TURF: " + best.DisplayName.ToUpperInvariant() + " (" + bestN + ")";
        }

        public override string StatusFor(Contestant c) => CountFor(c) + " TILES";

        public override string Banner(Contestant player, out Color color)
        {
            color = UiSkin.Cream;
            return null;
        }

        // ------------------------------------------------------------------ bots

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            botTarget.TryGetValue(me, out int target);
            botRepick.TryGetValue(me, out float repick);
            bool need = !botTarget.ContainsKey(me) || owner[target] == me.NetId || Time.time > repick;
            if (need)
            {
                int bestI = -1;
                float bestScore = float.MaxValue;
                int here = TileAt(pos);
                for (int i = 0; i < owner.Length; i++)
                {
                    if (owner[i] == me.NetId || i == here) continue;
                    float d = Shapes.Flat(TileCenter(i) - pos).magnitude;
                    // Prefer stealing from the leader a bit, and spread bots out with a little noise.
                    float score = d + Mathf.Repeat(i * 0.618f + me.NetId * 0.37f, 1f) * 3f - (owner[i] != NoOwner ? 1.5f : 0f);
                    if (score < bestScore) { bestScore = score; bestI = i; }
                }
                if (bestI < 0) return null;
                botTarget[me] = bestI;
                botRepick[me] = Time.time + Random.Range(1.5f, 3f);
                target = bestI;
            }
            return TileCenter(target);
        }
    }
}
