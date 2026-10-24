using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// COLOR CRAZY: a floor of colored tiles. A color is called - get on a tile of that color
    /// before the timer runs out, because every other tile drops away. Then they all come back,
    /// reshuffled, and it gets faster. Fall and you're out.
    /// Every round's colors and timing come from the seed and the shared clock, so all machines
    /// drop the same tiles at the same moment.
    /// </summary>
    public class ColorCrazyGame : MiniGame
    {
        const int Grid = 7;
        const float Tile = 3.3f, Gap = 0.15f, FirstRound = 3f, DropTime = 2.4f, Rest = 1.2f;

        static readonly Color[] Colors =
        {
            new Color(0.95f, 0.25f, 0.3f), new Color(0.25f, 0.6f, 0.95f), new Color(0.3f, 0.85f, 0.35f),
            new Color(1f, 0.82f, 0.15f), new Color(0.7f, 0.35f, 0.95f),
        };
        static readonly string[] Names = { "RED", "BLUE", "GREEN", "YELLOW", "PURPLE" };

        public override EventKind Kind => EventKind.ColorCrazy;
        public override string Title => "COLOR CRAZY";
        public override string Goal => "Stand on the called color before the floor drops.";
        public override string Objective => "A color gets called. Get onto a tile of that color - every other tile falls away! It gets faster every round. Fall and you're out.";
        public override string Blurb => "Get on the called color before the other tiles drop. Faster every round.";
        public override string[,] Cards => new[,]
        {
            { "FIND THE COLOR", "before the timer ends" },
            { "FALL = OUT", "one life only" },
            { "$5 / SEC", "while you're still in" },
        };
        public override float Duration => 120f;
        public override bool FallsEliminate => true;
        public override bool WeaponsAllowed => false;
        public override int StandingPerSecond => 5;

        GameObject[] tiles;
        Renderer[] tileRenderers;
        Collider[] tileColliders;
        Material[] mats;
        Renderer beacon;
        int shownRound = -1;
        bool shownDropped;

        public override void Build(Transform root, CourseInfo info)
        {
            float span = Grid * Tile;
            var frame = MiniKit.M("Color_Frame", new Color(0.2f, 0.2f, 0.25f));
            for (int x = 0; x < Grid; x++)
                for (int z = 0; z < Grid; z++)
                {
                    var p = new Vector3((x - (Grid - 1) * 0.5f) * Tile, -0.4f, (z - (Grid - 1) * 0.5f) * Tile);
                    MiniKit.Block(root, "Tile_" + x + "_" + z, p, new Vector3(Tile - Gap, 0.8f, Tile - Gap), MiniKit.M("Color_0", Colors[0]));
                }
            // Rim and a beacon pillar that glows the called color
            for (int i = 0; i < 4; i++)
                MiniKit.Block(root, "Rim", Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(0f, -0.4f, span * 0.5f + 0.4f), new Vector3(span + 1.6f, 0.8f, 0.8f), frame, true,
                    new Vector3(0f, i * 90f, 0f));
            MiniKit.Block(root, "Beacon", new Vector3(0f, 6f, span * 0.5f + 6f), new Vector3(3f, 12f, 3f), MiniKit.Glow("Color_Beacon", Color.white), false);
            MiniKit.Block(root, "Grid", new Vector3(0f, -1f, 0f), new Vector3(span, 0.2f, span), frame, false);

            info.spawnPoints = MiniKit.TwoRings(Origin, 4f, 8.5f);
            info.center = Origin;
            info.radius = span * 0.5f;
            MiniKit.Lounge(root, info, new Vector3(0f, 9f, -26f), 180f);
            MiniKit.Dress(root, span * 0.5f + 2f, 2424, "COLOR CRAZY", null, null, new Color(0.15f, 0.15f, 0.2f));
        }

        public override void Begin()
        {
            tiles = new GameObject[Grid * Grid];
            tileRenderers = new Renderer[tiles.Length];
            tileColliders = new Collider[tiles.Length];
            for (int x = 0; x < Grid; x++)
                for (int z = 0; z < Grid; z++)
                {
                    var t = Course.transform.Find("Tile_" + x + "_" + z);
                    int i = x * Grid + z;
                    tiles[i] = t != null ? t.gameObject : null;
                    tileRenderers[i] = t != null ? t.GetComponent<Renderer>() : null;
                    tileColliders[i] = t != null ? t.GetComponent<Collider>() : null;
                }
            mats = new Material[Colors.Length];
            for (int c = 0; c < Colors.Length; c++) mats[c] = MiniKit.M("Color_" + c, Colors[c]);
            var b = Course.transform.Find("Beacon");
            beacon = b != null ? b.GetComponent<Renderer>() : null;
            shownRound = -1;
            shownDropped = false;
            Show(0, false, true);
            shownRound = -1; // so round 1 still gets its "RED!" call
        }

        public override void End()
        {
            if (tiles == null) return;
            for (int i = 0; i < tiles.Length; i++) SetTile(i, true);
        }

        // ---- The round clock ----

        float CallTime(int r) => Mathf.Lerp(5f, 2.2f, Mathf.Clamp01(r / 10f));
        float RoundLength(int r) => CallTime(r) + DropTime + Rest;

        /// <summary>Which round it is at time t, how far into it, and whether the floor is down.</summary>
        int RoundAt(float t, out float into, out bool dropped)
        {
            dropped = false;
            into = 0f;
            if (t < FirstRound) return -1;
            float s = FirstRound;
            for (int r = 0; r < 200; r++)
            {
                float len = RoundLength(r);
                if (t < s + len)
                {
                    into = t - s;
                    dropped = into >= CallTime(r) && into < CallTime(r) + DropTime;
                    return r;
                }
                s += len;
            }
            return 199;
        }

        int Called(int r) => Mathf.FloorToInt(Rand(r, 1) * Colors.Length) % Colors.Length;

        int TileColor(int r, int i)
        {
            int called = Called(r);
            // Make sure there are always a few safe tiles (fewer as rounds go on).
            int safeEvery = r < 4 ? 4 : r < 8 ? 5 : 7;
            if (Mathf.FloorToInt(Rand(r * 97 + i, 2) * safeEvery) == 0) return called;
            int c = Mathf.FloorToInt(Rand(r * 97 + i, 3) * Colors.Length) % Colors.Length;
            return c;
        }

        void SetTile(int i, bool solid)
        {
            if (tileRenderers[i] != null) tileRenderers[i].enabled = solid;
            if (tileColliders[i] != null) tileColliders[i].enabled = solid;
        }

        void Show(int r, bool dropped, bool force = false)
        {
            if (!force && r == shownRound && dropped == shownDropped) return;
            bool newRound = r != shownRound;
            shownRound = r;
            shownDropped = dropped;
            int called = Called(Mathf.Max(0, r));
            for (int i = 0; i < tiles.Length; i++)
            {
                int c = TileColor(Mathf.Max(0, r), i);
                if (tileRenderers[i] != null) tileRenderers[i].sharedMaterial = mats[c];
                SetTile(i, !dropped || c == called);
            }
            if (beacon != null) beacon.sharedMaterial = MiniKit.Glow("Color_Glow_" + called, Colors[called]);
            if (r >= 0 && newRound && !force)
            {
                Announcer.ShowToast(Names[called] + "!");
                Sfx.Ui("count_beep");
            }
            if (dropped) Sfx.Ui("tile_fall", 0.8f);
        }

        public override void Tick()
        {
            if (tiles == null) return;
            int r = RoundAt(Mathf.Max(0f, Clock), out _, out bool dropped);
            if (r < 0) return;
            Show(r, dropped);
        }

        public override string Status(RaceManager race)
        {
            int r = RoundAt(Mathf.Max(0f, Clock), out float into, out bool dropped);
            if (r < 0) return "STILL IN: " + race.AliveCount;
            string what = dropped ? "HOLD ON!" : "GET ON " + Names[Called(r)] + "  " + Mathf.CeilToInt(Mathf.Max(0f, CallTime(r) - into));
            return "STILL IN: " + race.AliveCount + "     " + what;
        }

        public override string Banner(Contestant player, out Color color)
        {
            color = UiSkin.Gold;
            int r = RoundAt(Mathf.Max(0f, Clock), out float into, out bool dropped);
            if (r < 0 || dropped || player == null || player.Eliminated || into > CallTime(r)) return null;
            int called = Called(r);
            color = Colors[called];
            return Names[called];
        }

        // ---- Bots: walk to the closest tile of the called color ----

        public override Vector3? BotGoal(Contestant me, Vector3 pos, float skill)
        {
            int r = RoundAt(Mathf.Max(0f, Clock), out float into, out _);
            if (r < 0 || tiles == null) return Origin;
            // Slow bots notice late.
            if (into < (1f - skill) * 1.2f) return null;
            int called = Called(r);
            Vector3 best = Origin;
            float bd = float.MaxValue;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null || TileColor(r, i) != called) continue;
                Vector3 tp = tiles[i].transform.position;
                float d = Shapes.Flat(tp - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = tp; }
            }
            return new Vector3(best.x, Origin.y, best.z);
        }
    }
}
