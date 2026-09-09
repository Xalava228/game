using System;
using UnityEngine;

namespace TimeThief
{
    [Serializable]
    public sealed class Reward
    {
        public Stat stat;
        public float amount;
        public bool percent;
        public void Apply(PlayerStats p) => p.Upgrade(stat, amount, percent);
    }

    public static class RewardManager
    {
        public static int Shards(Encounter e) => Mathf.RoundToInt((3 + Mathf.Sqrt(e.level)) * (e.type == EncounterType.Boss ? 8 * (e.boss ? e.boss.rewardMultiplier : 1) : e.type == EncounterType.MiniBoss ? 4 : 1));
        public static Reward[] Choices(int seed, int level, bool boss)
        {
            var r = new System.Random(unchecked(seed + level * 3571));
            int[] stats = {0, 1, 2, 3, 4, 5};
            for (int i = 5; i > 0; i--)
            {
                int j = r.Next(i + 1);
                (stats[i], stats[j]) = (stats[j], stats[i]);
            }

            var result = new Reward[boss ? 3 : 1];
            for (int i = 0; i < result.Length; i++)
            {
                var s = (Stat)stats[i];
                result[i] = new Reward{stat = s, amount = (boss ? new[]{5f, .2f, .08f, .4f, 5, 5} : new[]{2f, .3f, .03f, .2f, 2, 2})[(int)s], percent = boss && s == Stat.Attack};
            }

            return result;
        }
    }
}
