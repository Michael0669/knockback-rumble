using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Shared base for "grab the stuff" games (Make It Rain, Balloon Pop). Every item's spawn time,
    /// place and type come from the round's seed, so all machines show the same items at the same
    /// time. Only "who grabbed it" goes over the network: the host decides and sends it.
    /// </summary>
    public abstract class CollectGame : MiniGame
    {
        const byte OpTaken = 1;

        protected struct Item
        {
            public float spawn;     // seconds after GO
            public float life;      // seconds it stays around
            public Vector3 pos;     // world position it spawns at (games move it from there)
            public byte type;
        }

        protected Item[] items;
        protected bool[] taken;
        GameObject[] views;
        int first;

        protected abstract int ItemCount { get; }
        protected abstract Item MakeItem(int i);
        /// <summary>Where item i is at time t (seconds after GO). collectable = can be grabbed right now.</summary>
        protected abstract Vector3 PositionAt(int i, float t, out bool collectable);
        protected abstract GameObject MakeView(int i, Transform parent);
        protected abstract int Value(int i);
        /// <summary>How close (from the dummy's middle) you must get.</summary>
        protected virtual float GrabRadius => 1.4f;
        /// <summary>Visual/sound when grabbed (every machine).</summary>
        protected abstract void OnGrabbed(int i, Vector3 at, Contestant by);
        /// <summary>Per-frame extra for a live item (e.g. bombs exploding). Every machine.</summary>
        protected virtual void ItemTick(int i, float t, GameObject view) { }
        /// <summary>Items that are never grabbed (bombs).</summary>
        protected virtual bool Grabbable(int i) => true;

        public override void Begin()
        {
            int n = ItemCount;
            items = new Item[n];
            taken = new bool[n];
            views = new GameObject[n];
            for (int i = 0; i < n; i++) items[i] = MakeItem(i);
            first = 0;
        }

        public override void End()
        {
            if (views != null)
                foreach (var v in views) if (v != null) Object.Destroy(v);
            views = null;
        }

        bool Live(int i, float t) => t >= items[i].spawn && t < items[i].spawn + items[i].life && !taken[i];

        public override void Tick()
        {
            if (items == null) return;
            float t = Clock;
            bool playing = Race != null && Race.State != RaceManager.Phase.Countdown;
            while (first < items.Length && t > items[first].spawn + items[first].life + 0.5f)
            {
                if (views[first] != null) { Object.Destroy(views[first]); views[first] = null; }
                first++;
            }
            for (int i = first; i < items.Length; i++)
            {
                if (items[i].spawn > t) break; // sorted by spawn time
                bool live = playing && Live(i, t);
                if (!live)
                {
                    if (views[i] != null) { Object.Destroy(views[i]); views[i] = null; }
                    continue;
                }
                if (views[i] == null) views[i] = MakeView(i, Course.transform);
                views[i].transform.position = PositionAt(i, t, out _);
                ItemTick(i, t, views[i]);
            }
        }

        public override void HostFrame(RaceManager race)
        {
            if (items == null) return;
            float t = Clock;
            for (int i = first; i < items.Length; i++)
            {
                if (items[i].spawn > t) break;
                if (!Live(i, t) || !Grabbable(i)) continue;
                Vector3 p = PositionAt(i, t, out bool collectable);
                if (!collectable) continue;
                foreach (var c in Alive())
                {
                    if ((c.transform.position + Vector3.up * 0.9f - p).magnitude > GrabRadius) continue;
                    Grab(i, c, p);
                    Send(Msg(OpTaken).UShort((ushort)i).Byte(c.NetId));
                    Award(c, Value(i));
                    break;
                }
            }
        }

        void Grab(int i, Contestant c, Vector3 at)
        {
            if (taken[i]) return;
            taken[i] = true;
            if (views[i] != null) { Object.Destroy(views[i]); views[i] = null; }
            OnGrabbed(i, at, c);
        }

        public override void OnMessage(byte op, NetReader r)
        {
            if (op != OpTaken || items == null) return;
            int i = r.UShort();
            var c = ById(r.Byte());
            if (i < 0 || i >= items.Length) return;
            Grab(i, c, PositionAt(i, Clock, out _));
        }

        /// <summary>Bots: the nearest grabbable item (optionally only ones you can reach from the ground).</summary>
        protected Vector3? NearestItem(Vector3 from, float maxHeightAboveFeet)
        {
            if (items == null) return null;
            float t = Clock;
            Vector3? best = null;
            float bestD = float.MaxValue;
            for (int i = first; i < items.Length; i++)
            {
                if (items[i].spawn > t) break;
                if (!Live(i, t) || !Grabbable(i)) continue;
                Vector3 p = PositionAt(i, t, out _);
                if (p.y - from.y > maxHeightAboveFeet) continue;
                float d = Shapes.Flat(p - from).magnitude - Value(i) * 0.02f;
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }
    }
}
