using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeThief
{
    [Serializable]
    public sealed class ActiveBuff
    {
        public BuffType type;
        public int remainingBattles;
    }

    [Serializable]
    public sealed class ShopManager
    {
        public int shards, maxTimePurchases, attackPurchases, armorPurchases, resistancePurchases;
        public List<ActiveBuff> buffs = new List<ActiveBuff>();
        // Kept only to read old saves. All new purchases use the player's life timer.
        public const float MinimumReserve = 1f;
        public float Cost(int item)
        {
            if (item < 0 || item >= 9) return float.PositiveInfinity;
            if (item < 4)
            {
                long n = item == 0 ? maxTimePurchases : item == 1 ? attackPurchases : item == 2 ? armorPurchases : resistancePurchases;
                return (float)Math.Round(Math.Min(10000000, (item == 1 ? .65 : item == 0 ? .85 : .90) + n * .20 + n * n * .025), 2, MidpointRounding.AwayFromZero);
            }
            return new[]{.65f, .65f, .70f, .90f, .40f}[item - 4];
        }

        public bool Has(BuffType t) => buffs.Exists(b => b.type == t && b.remainingBattles > 0);
        public bool CanBuy(int item, PlayerStats p) => p != null && item >= 0 && item < 9 && p.CurrentTime - Cost(item) >= MinimumReserve && (item < 4 || !Has((BuffType)(item - 2)));
        public bool Buy(int item, PlayerStats p)
        {
            if (!CanBuy(item, p))
                return false;
            p.LoseTime(Cost(item));
            if (item == 0)
            {
                p.MaxTime += 1; // Capacity does not refund the life spent buying it.
                maxTimePurchases++;
            }
            else if (item == 1)
            {
                p.Upgrade(Stat.Attack, .15f);
                attackPurchases++;
            }
            else if (item == 2 || item == 3)
            {
                p.Upgrade(item == 2 ? Stat.Armor : Stat.MagicResistance, 3);
                if (item == 2) armorPurchases++; else resistancePurchases++;
            }
            else
                buffs.Add(new ActiveBuff{type = (BuffType)(item - 2), remainingBattles = item < 4 ? 3 : 1});
            return true;
        }

        public void EndBattle()
        {
            for (int i = buffs.Count - 1; i >= 0; i--)
                if (--buffs[i].remainingBattles <= 0)
                    buffs.RemoveAt(i);
        }
    }
}
