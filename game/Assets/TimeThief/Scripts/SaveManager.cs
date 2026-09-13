using System;
using System.IO;
using UnityEngine;

namespace TimeThief
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1, bestLevel, totalEnemiesDefeated, totalBossesDefeated, seed, level = 1, defeated, lastReward;
        public bool activeRun, reviveUsed, rewardDoubled, bossPhase2;
        public string phase = "Intro";
        public PlayerStats player;
        public Reward lastGift;
        public float[] lastGrowth;
        public ShopManager shop;
        public float enemyTime, enemyTimer, elapsed;
        public int enemyAttacks, physicalHits;
        public int economyVersion, bossStage;
        public float lastTimeReward, lastTimeAwarded, bossProgress, bossWindow;
        public long updatedAt;
    }

    public static class SaveManager
    {
        const string Key = "TimeThiefSaveV1";
        static string EditorPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../LocalData/save.json"));
        public static SaveData Load()
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
   string json=PlatformBridge.Load();if(string.IsNullOrEmpty(json))json=PlayerPrefs.GetString(Key,"");
#else
                string json = File.Exists(EditorPath) ? File.ReadAllText(EditorPath) : "";
#endif
                if (string.IsNullOrEmpty(json))
                    return new SaveData();
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d == null || d.version != 1)
                    return new SaveData();
                d.bestLevel = Mathf.Max(0, d.bestLevel);
                d.totalEnemiesDefeated = Mathf.Max(0, d.totalEnemiesDefeated);
                if (d.lastGift != null && ((int)d.lastGift.stat < 0 || (int)d.lastGift.stat > 5 || !float.IsFinite(d.lastGift.amount) || d.lastGift.amount <= 0))
                    d.lastGift = null;
                if (d.lastGrowth != null)
                {
                    bool validGrowth = d.lastGrowth.Length == 6;
                    foreach (float value in d.lastGrowth) validGrowth &= float.IsFinite(value) && value >= 0;
                    if (!validGrowth) d.lastGrowth = null;
                }
                if (d.level < 1 || d.level > 10000000 || d.player == null || !d.player.Valid() || d.shop == null || d.shop.shards < 0 || d.shop.maxTimePurchases < 0 || d.shop.maxTimePurchases > 100000 || d.shop.attackPurchases < 0 || d.shop.attackPurchases > 100000 || d.shop.armorPurchases < 0 || d.shop.armorPurchases > 100000 || d.shop.resistancePurchases < 0 || d.shop.resistancePurchases > 100000 || d.shop.buffs == null || d.shop.buffs.Count > 7 || !float.IsFinite(d.enemyTime) || !float.IsFinite(d.enemyTimer) || !float.IsFinite(d.elapsed))
                    d.activeRun = false;
                if (d.shop?.buffs != null)
                    foreach (var buff in d.shop.buffs)
                        if (buff == null || (int)buff.type < 0 || (int)buff.type > 6 || buff.remainingBattles < 1 || buff.remainingBattles > 3)
                            d.activeRun = false;
                MigrateEconomy(d);
                return d;
            }
            catch
            {
                return new SaveData();
            }
        }

        public static void MigrateEconomy(SaveData d)
        {
            if (d.economyVersion < 2)
            {
                // Preserve upgrades and convert legacy balance once, capped by capacity.
                if (d.player != null && d.player.Valid() && d.player.CurrentTime > 0 && d.shop != null)
                    d.player.AddTime(Mathf.Max(0, d.shop.shards) * .1f);
                if (d.shop != null) d.shop.shards = 0;
                d.lastTimeReward = d.lastReward > 0 ? .65f : 0;
                d.lastReward = 0;
                d.economyVersion = 2;
            }
            if (!float.IsFinite(d.lastTimeReward) || d.lastTimeReward < 0 || d.lastTimeReward > 4) d.lastTimeReward = 0;
            if (!float.IsFinite(d.lastTimeAwarded) || d.lastTimeAwarded < 0 || d.lastTimeAwarded > 8) d.lastTimeAwarded = 0;
            d.bossStage = Mathf.Clamp(d.bossStage, 0, 3);
            if (!float.IsFinite(d.bossProgress)) d.bossProgress = 0;
            if (!float.IsFinite(d.bossWindow)) d.bossWindow = 0;
            d.bossProgress = Mathf.Clamp(d.bossProgress, 0, 100000000);
            d.bossWindow = Mathf.Clamp(d.bossWindow, 0, 4);
        }

        public static void Write(SaveData d)
        {
            d.economyVersion = 2;
            d.updatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = JsonUtility.ToJson(d);
#if UNITY_WEBGL && !UNITY_EDITOR
   PlayerPrefs.SetString(Key,json);PlayerPrefs.Save();PlatformBridge.Save(json);
#else
            Directory.CreateDirectory(Path.GetDirectoryName(EditorPath));
            File.WriteAllText(EditorPath + ".tmp", json);
            File.Copy(EditorPath + ".tmp", EditorPath, true);
            File.Delete(EditorPath + ".tmp");
#endif
        }
    }
}
