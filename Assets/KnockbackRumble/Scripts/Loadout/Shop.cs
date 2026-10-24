namespace KnockbackRumble
{
    /// <summary>Questions about everything you can buy (weapons, gadgets, perks, kill streaks, characters).</summary>
    public static class Shop
    {
        /// <summary>The cheapest thing you don't own yet (null if you own everything).</summary>
        public static string NextUnlock(out int price)
        {
            string best = null;
            int cheapest = int.MaxValue;
            void Check(string name, int p, bool owned)
            {
                if (owned || p >= cheapest) return;
                best = name;
                cheapest = p;
            }
            foreach (var w in WeaponDef.Guns) Check(w.name, w.price, Profile.Owns(w));
            foreach (var i in Loadouts.Gadgets) Check(i.name, i.price, Profile.Owns(ShopCategory.Gadget, i));
            foreach (var i in Loadouts.Perks) Check(i.name, i.price, Profile.Owns(ShopCategory.Perk, i));
            foreach (var i in Loadouts.Streaks) Check(i.name, i.price, Profile.Owns(ShopCategory.Streak, i));
            foreach (var c in Characters.All) Check(c.name, c.price, Profile.OwnsCharacter(c));
            price = best == null ? 0 : cheapest;
            return best;
        }
    }
}
