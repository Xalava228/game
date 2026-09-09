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
        public int shards, maxTimePurchases, attackPurchases;
        public List<ActiveBuff> buffs = new List<ActiveBuff>();
        public int Cost(int item)
        {
            if (item == 0)
                return 8 + maxTimePurchases * 5 + maxTimePurchases * maxTimePurchases / 2;
            if (item == 1)
                return 8 + attackPurchases * 5 + attackPurchases * attackPurchases / 2;
            return new[]{10, 10, 12, 12, 12, 10, 14}[item - 2];
        }

        public bool Has(BuffType t) => buffs.Exists(b => b.type == t && b.remainingBattles > 0);
        public bool CanBuy(int item) => item >= 0 && item < 9 && shards >= Cost(item) && (item < 2 || !Has((BuffType)(item - 2)));
        public bool Buy(int item, PlayerStats p)
        {
            if (!CanBuy(item))
                return false;
            shards -= Cost(item);
            if (item == 0)
            {
                p.Upgrade(Stat.MaxTime, 1);
                maxTimePurchases++;
            }
            else if (item == 1)
            {
                p.Upgrade(Stat.Attack, .15f);
                attackPurchases++;
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
