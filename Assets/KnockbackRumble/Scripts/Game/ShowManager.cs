using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// The show: a lineup of events played back to back, and the cash everybody earns.
    /// Most cash at the end of the last event wins the show.
    ///
    /// The HOST is the bank. Every payout is decided on the host and sent to everyone
    /// (CashDelta), so all screens show the same numbers. The host also owns the money
    /// bags in Cash Grab. Offline, this machine is the host.
    ///
    /// Where cash comes from (balance numbers are the constants below):
    ///   knock someone off           +$100
    ///   fall off                     -$50  (-$25 with Deep Pockets)
    ///   get hit by a player          "pain pay": a few bucks per hit, more for bigger hits
    ///   Gauntlet finish              $500 / $350 / $250 / $150, then $100
    ///   Cash Grab                    +$10 a second while holding a bag
    ///   Crumble Plaza                +$5 a second while standing, then survivor bonuses
    /// </summary>
    public class ShowManager : MonoBehaviour
    {
        public static ShowManager Instance { get; private set; }

        /// <summary>The original three-event show.</summary>
        public static readonly EventKind[] ClassicLineup = { EventKind.Gauntlet, EventKind.CashGrab, EventKind.Crumble };

        /// <summary>The games in this tournament, in order (the host picks them; see Tournament).</summary>
        public readonly List<EventKind> Playlist = new List<EventKind>(ClassicLineup);

        public enum Reason : byte { Knockoff, Fall, PainPay, Finish, Bag, Standing, Survivor, Prize }

        // ---- Balance ----
        public const int KnockoffCash = 100;
        public const int BountyBonus = 50;
        public const int FallPenalty = 50;
        public const int BagPerSecond = 10;
        public const int StandingPerSecond = 5;
        public const int PainPayMax = 20;
        public static readonly int[] FinishPayout = { 500, 350, 250, 150 };
        public const int FinishOther = 100;
        public static readonly int[] SurvivorPayout = { 300, 150, 75 };
        public const int BagCount = 3;
        public const float NextRoundDelay = 8f;

        /// <summary>How long the loading screen (rules and controls) stays up before each event's countdown.</summary>
        public static float LoadingTimeFor(int round) => round == 0 ? 10f : 8f;
        const float BagDropKnock = 7f;
        const float RegrabDelay = 1.5f;

        public int RoundIndex { get; private set; }
        public int RoundCount => Playlist.Count;
        public EventKind Kind { get; private set; }
        public bool IsLastRound => RoundIndex >= RoundCount - 1;
        /// <summary>The last event's results are up: the show is over.</summary>
        public bool ShowOver { get; private set; }
        /// <summary>When the next event starts automatically (Time.time; 0 = not scheduled).</summary>
        public float NextRoundAt { get; private set; }

        // The local player's rewards when the show ends.
        public int RewardCash { get; private set; }
        public bool RewardWon { get; private set; }

        public class Popup
        {
            public int amount;
            public Reason reason;
            public float time;
        }

        /// <summary>Recent cash changes for the local player (the HUD shows these under the cash counter).</summary>
        public readonly List<Popup> Popups = new List<Popup>();
        public readonly List<MoneyBag> Bags = new List<MoneyBag>();

        struct Delta
        {
            public byte id;
            public int amount;
            public Reason reason;
        }

        readonly Dictionary<byte, int> cash = new Dictionary<byte, int>();
        readonly Dictionary<byte, int> roundCash = new Dictionary<byte, int>();
        readonly List<Delta> pending = new List<Delta>();
        readonly NetWriter writer = new NetWriter();
        float nextTick, nextBagRequest;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public EventKind KindAt(int round) => Playlist.Count == 0 ? EventKind.Gauntlet : Playlist[Mathf.Clamp(round, 0, Playlist.Count - 1)];

        /// <summary>Set the games for the next tournament (every machine, before round 0).</summary>
        public void SetPlaylist(IList<EventKind> kinds)
        {
            Playlist.Clear();
            if (kinds != null) Playlist.AddRange(kinds);
            if (Playlist.Count == 0) Playlist.AddRange(ClassicLineup);
        }

        public static string ReasonLabel(Reason r)
        {
            switch (r)
            {
                case Reason.Knockoff: return "KNOCK-OFF";
                case Reason.Fall: return "FELL OFF";
                case Reason.PainPay: return "PAIN PAY";
                case Reason.Finish: return "FINISH PAYOUT";
                case Reason.Bag: return "BAG MONEY";
                case Reason.Standing: return "STILL STANDING";
                case Reason.Prize: return MiniGames.Current != null ? MiniGames.Current.PrizeLabel : "PRIZE";
                default: return "SURVIVOR BONUS";
            }
        }

        // =====================================================================
        // Rounds
        // =====================================================================

        /// <summary>A new event is starting (every machine). Round 0 starts a fresh show.</summary>
        public void BeginRound(EventKind kind, int round, CourseInfo course, int seed)
        {
            Kind = kind;
            RoundIndex = round;
            ShowOver = false;
            NextRoundAt = 0f;
            if (round == 0) cash.Clear();
            roundCash.Clear();
            Popups.Clear();
            pending.Clear();
            nextTick = 0f;
            ClearBags();
            if (kind == EventKind.CashGrab && course != null) SpawnBags(course, seed);
        }

        /// <summary>Back to the menu or lobby.</summary>
        public void Clear()
        {
            ClearBags();
            cash.Clear();
            roundCash.Clear();
            Popups.Clear();
            pending.Clear();
            ShowOver = false;
            NextRoundAt = 0f;
            RoundIndex = 0;
        }

        /// <summary>An event's results are up (every machine).</summary>
        public void OnRoundResults(RaceManager race)
        {
            if (IsLastRound) FinishShow(race);
            else NextRoundAt = Time.time + NextRoundDelay;
        }

        void FinishShow(RaceManager race)
        {
            ShowOver = true;
            var p = race.Player;
            if (p == null) return;
            RewardCash = Cash(p);
            RewardWon = race.Results.Count > 0 && race.Results[0] == p;
            Profile.AwardShow(RewardCash, RewardWon);
        }

        // =====================================================================
        // Cash
        // =====================================================================

        public int Cash(Contestant c) => c != null && cash.TryGetValue(c.NetId, out int v) ? v : 0;
        public int RoundCash(Contestant c) => c != null && roundCash.TryGetValue(c.NetId, out int v) ? v : 0;

        /// <summary>Contestants sorted by show cash (then knock-offs), richest first.</summary>
        public List<Contestant> Ranking(IEnumerable<Contestant> list)
        {
            var order = new List<Contestant>();
            foreach (var c in list) if (c != null) order.Add(c);
            order.Sort((a, b) =>
            {
                int byCash = Cash(b).CompareTo(Cash(a));
                if (byCash != 0) return byCash;
                int byKo = b.Knockoffs.CompareTo(a.Knockoffs);
                return byKo != 0 ? byKo : a.NetId.CompareTo(b.NetId);
            });
            return order;
        }

        /// <summary>The richest dummy still in play, not counting "excluding" (Wrecking Ball target).</summary>
        public Contestant Leader(Contestant excluding)
        {
            var race = RaceManager.Instance;
            if (race == null) return null;
            Contestant best = null;
            int bestCash = -1;
            foreach (var c in race.Contestants)
            {
                if (c == null || c == excluding || c.Eliminated) continue;
                int v = Cash(c);
                if (v > bestCash) { best = c; bestCash = v; }
            }
            return best;
        }

        /// <summary>Host only: pay (or charge) a contestant. Sent to everyone at the end of the frame.</summary>
        public void HostAward(Contestant c, int amount, Reason reason)
        {
            if (c == null || amount == 0 || !Net.IsHost) return;
            int applied = Apply(c.NetId, amount, reason);
            if (Net.Online && applied != 0) pending.Add(new Delta { id = c.NetId, amount = applied, reason = reason });
        }

        int Apply(byte id, int amount, Reason reason)
        {
            cash.TryGetValue(id, out int have);
            int now = Mathf.Max(0, have + amount);
            int applied = now - have;
            cash[id] = now;
            roundCash.TryGetValue(id, out int r);
            roundCash[id] = r + applied;

            var race = RaceManager.Instance;
            var player = race != null ? race.Player : null;
            if (player != null && player.NetId == id && applied != 0)
            {
                AddPopup(applied, reason);
                if (reason == Reason.Fall) Sfx.Ui("cash_lose");
                else if (reason == Reason.Bag || reason == Reason.Standing || reason == Reason.Prize) Sfx.Ui("coin", 1f, 0.05f);
                else if (reason != Reason.PainPay) Sfx.Ui("cash");
            }
            return applied;
        }

        void AddPopup(int amount, Reason reason)
        {
            // Steady income (bags, standing) piles into one popup instead of a new one every second.
            if (Popups.Count > 0)
            {
                var last = Popups[Popups.Count - 1];
                bool steady = reason == Reason.Bag || reason == Reason.Standing || reason == Reason.PainPay || reason == Reason.Prize;
                if (steady && last.reason == reason && Time.time - last.time < 1.6f)
                {
                    last.amount += amount;
                    last.time = Time.time;
                    return;
                }
            }
            Popups.Add(new Popup { amount = amount, reason = reason, time = Time.time });
            if (Popups.Count > 6) Popups.RemoveAt(0);
        }

        /// <summary>Send any queued payouts now (host).</summary>
        public void Flush()
        {
            if (!Net.Online || pending.Count == 0) { pending.Clear(); return; }
            int i = 0;
            while (i < pending.Count)
            {
                int n = Mathf.Min(200, pending.Count - i);
                var w = writer.Begin(MsgType.CashDelta).Byte((byte)n);
                for (int k = 0; k < n; k++)
                {
                    var d = pending[i + k];
                    w.Byte(d.id).Int(d.amount).Byte((byte)d.reason);
                }
                Net.Session.Send(w);
                i += n;
            }
            pending.Clear();
        }

        /// <summary>Client: the host paid someone.</summary>
        public void OnCashDelta(NetReader r)
        {
            int n = r.Byte();
            for (int i = 0; i < n; i++)
            {
                byte id = r.Byte();
                int amount = r.Int();
                var reason = (Reason)r.Byte();
                if (!Net.IsHost) Apply(id, amount, reason);
            }
        }

        // ---------- Host hooks (called by RaceManager on the host) ----------

        public void HostOnFell(Contestant victim, Contestant attacker)
        {
            if (attacker != null && attacker != victim)
            {
                bool bounty = attacker.Loadout != null && attacker.Loadout.Has(Perk.BountyHunter);
                HostAward(attacker, KnockoffCash + (bounty ? BountyBonus : 0), Reason.Knockoff);
            }
            bool deep = victim.Loadout != null && victim.Loadout.Has(Perk.DeepPockets);
            var mini = MiniGames.Current;
            if (mini == null || mini.FallPenalty) HostAward(victim, -(deep ? FallPenalty / 2 : FallPenalty), Reason.Fall);
            var bag = BagOf(victim);
            if (bag != null) HostDropBag(bag, null, true);
            if (mini != null) mini.HostOnFell(victim, attacker != victim ? attacker : null);
        }

        public void HostOnLaunched(Contestant victim, Contestant attacker, float knock, bool hazard)
        {
            if (!hazard && attacker != null && attacker != victim)
            {
                int pay = Mathf.Clamp(Mathf.RoundToInt(knock * 0.5f), 1, PainPayMax);
                if (victim.Loadout != null && victim.Loadout.Has(Perk.HazardPay)) pay *= 2;
                HostAward(victim, pay, Reason.PainPay);
            }
            var bag = BagOf(victim);
            if (bag != null && knock >= BagDropKnock) HostDropBag(bag, victim, false);
            if (MiniGames.Current != null) MiniGames.Current.HostOnLaunched(victim, hazard ? null : attacker, knock);
        }

        public void HostOnFinish(Contestant c, int place) =>
            HostAward(c, place <= FinishPayout.Length ? FinishPayout[place - 1] : FinishOther, Reason.Finish);

        public void HostOnRoundEnd(RaceManager race)
        {
            if (MiniGames.Current != null) MiniGames.Current.HostRoundEnd(race);
            if (race.HasEliminations)
            {
                // Everyone still standing gets the top bonus; the last ones knocked out get the rest.
                int survivors = 0;
                foreach (var c in race.Contestants)
                    if (c != null && !c.Eliminated) { HostAward(c, SurvivorPayout[0], Reason.Survivor); survivors++; }
                int tier = survivors > 0 ? 1 : 0;
                for (int i = race.Eliminations.Count - 1; i >= 0 && tier < SurvivorPayout.Length; i--, tier++)
                    HostAward(race.Eliminations[i], SurvivorPayout[tier], Reason.Survivor);
            }
            Flush();
        }

        // =====================================================================
        // Per frame
        // =====================================================================

        void Update()
        {
            var race = RaceManager.Instance;
            if (race == null) return;

            if (race.State == RaceManager.Phase.Racing)
            {
                if (Net.IsHost) HostTick(race);
                if (Kind == EventKind.CashGrab) CheckLocalPickups(race);
            }

            // Host: roll into the next event after the results screen.
            if (Net.IsHost && race.State == RaceManager.Phase.Results && !ShowOver && NextRoundAt > 0f && Time.time >= NextRoundAt)
            {
                NextRoundAt = 0f;
                if (MatchController.Instance != null) MatchController.Instance.HostNextRound();
            }
        }

        void LateUpdate()
        {
            if (Net.IsHost) Flush();
        }

        void HostTick(RaceManager race)
        {
            if (nextTick <= 0f) nextTick = Time.time + 1f;
            if (Time.time < nextTick) return;
            nextTick += 1f;

            if (Kind == EventKind.CashGrab)
            {
                foreach (var b in Bags)
                    if (b != null && b.Carried && b.Carrier != null) HostAward(b.Carrier, BagPerSecond, Reason.Bag);
            }
            else if (Kind == EventKind.Crumble)
            {
                foreach (var c in race.Contestants)
                    if (c != null && !c.Eliminated) HostAward(c, StandingPerSecond, Reason.Standing);
            }
            else if (MiniGames.Current != null)
            {
                var mini = MiniGames.Current;
                if (mini.StandingPerSecond > 0)
                    foreach (var c in race.Contestants)
                        if (c != null && !c.Eliminated) HostAward(c, mini.StandingPerSecond, Reason.Standing);
                mini.HostSecond(race);
            }
        }

        // =====================================================================
        // Money bags (Cash Grab)
        // =====================================================================

        void SpawnBags(CourseInfo course, int seed)
        {
            var spots = new List<Vector3>();
            if (course.bagSpawns != null) spots.AddRange(course.bagSpawns);
            if (spots.Count == 0) spots.Add(course.center);
            var rng = new System.Random(seed);
            for (int i = 0; i < BagCount; i++)
            {
                int pick = rng.Next(spots.Count);
                Bags.Add(MoneyBag.Spawn(i, spots[pick]));
                if (spots.Count > 1) spots.RemoveAt(pick);
            }
        }

        void ClearBags()
        {
            foreach (var b in Bags) if (b != null) Destroy(b.gameObject);
            Bags.Clear();
        }

        public MoneyBag BagOf(Contestant c)
        {
            if (c == null) return null;
            foreach (var b in Bags) if (b != null && b.Carried && b.Carrier == c) return b;
            return null;
        }

        MoneyBag Bag(int index) => index >= 0 && index < Bags.Count ? Bags[index] : null;

        Vector3 RandomBagSpot()
        {
            var course = RaceManager.Instance != null ? RaceManager.Instance.Course : null;
            if (course == null) return Vector3.zero;
            if (course.bagSpawns == null || course.bagSpawns.Length == 0) return course.center;
            return course.bagSpawns[Random.Range(0, course.bagSpawns.Length)];
        }

        /// <summary>Every machine checks its own dummies against bags on the ground and asks the host for them.</summary>
        void CheckLocalPickups(RaceManager race)
        {
            if (Time.time < nextBagRequest) return;
            foreach (var bag in Bags)
            {
                if (bag == null || bag.Carried || !bag.Landed) continue;
                foreach (var c in race.Contestants)
                {
                    if (c == null || !c.IsLocal || c.Eliminated || BagOf(c) != null) continue;
                    Vector3 d = c.transform.position - bag.transform.position;
                    if (Shapes.Flat(d).magnitude > 1.7f || Mathf.Abs(d.y) > 2.2f) continue;
                    nextBagRequest = Time.time + 0.25f;
                    if (Net.IsHost) HostGiveBag(bag, c);
                    else Net.Session.Send(writer.Begin(MsgType.BagReq).Byte((byte)bag.Index).Byte(c.NetId));
                    return;
                }
            }
        }

        /// <summary>Host: someone asked for a bag (BagReq).</summary>
        public void OnBagRequest(int index, Contestant c)
        {
            if (Net.IsHost) HostGiveBag(Bag(index), c);
        }

        void HostGiveBag(MoneyBag bag, Contestant c)
        {
            if (bag == null || c == null || bag.Carried || !bag.Landed || c.Eliminated || BagOf(c) != null) return;
            if (bag.LastDropper == c && Time.time - bag.DropTime < RegrabDelay) return;
            bag.SetCarrier(c);
            if (Net.Online)
                Net.Session.Send(writer.Begin(MsgType.Bag).Byte((byte)bag.Index).Byte(c.NetId).Vec3(bag.transform.position));
        }

        /// <summary>Host: knock a bag loose (it pops out next to them) or send it back to a spawn spot.</summary>
        public void HostDropBag(MoneyBag bag, Contestant from, bool respawn)
        {
            if (bag == null) return;
            Vector3 target;
            if (respawn || from == null)
            {
                target = RandomBagSpot();
            }
            else
            {
                Vector2 r = Random.insideUnitCircle.normalized * Random.Range(2.5f, 4f);
                Vector3 probe = from.transform.position + new Vector3(r.x, 2f, r.y);
                if (!MoneyBag.TryGround(probe, out target)) target = RandomBagSpot();
            }
            bag.Drop(target, bag.Carrier != null ? bag.Carrier : from);
            if (Net.Online)
                Net.Session.Send(writer.Begin(MsgType.Bag).Byte((byte)bag.Index).Byte(NetProtocol.NoId).Vec3(target));
        }

        /// <summary>Client: the host moved a bag.</summary>
        public void OnBagMessage(int index, Contestant carrier, bool carried, Vector3 pos)
        {
            if (Net.IsHost) return;
            var bag = Bag(index);
            if (bag == null) return;
            if (carried && carrier != null) bag.SetCarrier(carrier);
            else bag.Drop(pos, bag.Carrier);
        }
    }
}
