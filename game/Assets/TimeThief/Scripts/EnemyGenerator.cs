using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeThief
{
    [Serializable]
    public sealed class Encounter
    {
        public int level;
        public EncounterType type;
        public string nameRu, nameEn, artKey, hintRu, hintEn;
        public float maxTime, power, cooldown, physicalDefense, magicDefense, budget;
        public AttackType attackType;
        public Ability ability;
        public Vector3 abilityValues;
        public Modifier[] modifiers;
        [NonSerialized]
        public Sprite sprite;
        [NonSerialized]
        public BossData boss;
        public bool Has(Modifier m) => Array.IndexOf(modifiers, m) >= 0;
    }

    public static class EnemyGenerator
    {
        public static EncounterType TypeAt(int level) => level % 25 == 0 ? EncounterType.Boss : level % 10 == 0 ? EncounterType.MiniBoss : EncounterType.Normal;
        public static Encounter Generate(GameConfig c, int level, int seed)
        {
            level = Mathf.Max(1, level);
            var r = new System.Random(unchecked(seed + level * 7919));
            float root = Mathf.Sqrt(level);
            var type = TypeAt(level);
            var template = c.enemies[(level <= 5 ? new[]{6, 0, 7, 1, 0}[level - 1] : r.Next(c.enemies.Length)) % c.enemies.Length];
            var e = new Encounter{level = level, type = type, nameRu = template.nameRu, nameEn = template.nameEn, artKey = template.artKey, sprite = template.enemySprite, attackType = template.attackType, budget = Mathf.Log(level + 1, 2), abilityValues = new Vector3(6, .3f, 1.5f)};
            e.maxTime = c.baseEnemyTime + level * c.timePerLevel + root * c.timePerSqrtLevel;
            e.power = c.baseEnemyPower + root * c.powerPerSqrtLevel;
            e.cooldown = Mathf.Max(c.minimumCooldown, c.baseCooldown - Mathf.Log(level + 1) * c.cooldownDecay);
            // Fixed budget split: extra health/power trades against cooldown, instead of independent worst-case rolls.
            float healthShare = (float)r.NextDouble() - .5f;
            e.maxTime *= 1 + healthShare * .2f;
            e.power *= 1 - healthShare * .2f;
            int count = level <= 5 ? 0 : level < 20 ? r.Next(2) : level < 75 ? 1 + r.Next(2) : 2 + r.Next(2);
            if (type == EncounterType.MiniBoss)
            {
                count = 2 + r.Next(3);
                e.maxTime *= 1.6f;
                e.power *= 1.15f;
                e.cooldown *= 1.1f;
                e.ability = (Ability)(1 + r.Next(10));
                e.nameRu = "Хранитель · " + e.nameRu;
                e.nameEn = "Keeper · " + e.nameEn;
            }

            if (type == EncounterType.Boss)
            {
                int i = level <= 250 ? level / 25 - 1 : r.Next(c.bosses.Length);
                e.boss = c.bosses[i % c.bosses.Length];
                var b = e.boss;
                e.nameRu = b.bossName;
                e.nameEn = b.nameEn;
                e.hintRu = b.hintRu;
                e.hintEn = b.hintEn;
                e.artKey = b.artKey;
                e.sprite = b.bossSprite;
                e.maxTime = e.maxTime * 1.65f + b.baseMaxTime;
                e.power += b.attackPower;
                e.cooldown = Mathf.Max(c.minimumCooldown, (e.cooldown + b.attackCooldown) * .5f);
                e.attackType = b.attackType;
                e.physicalDefense = b.physicalDefense;
                e.magicDefense = b.magicDefense;
                e.ability = b.specialAbilityType;
                e.abilityValues = b.specialAbilityValues;
                count = level <= 250 ? 1 : 4;
                if (level > 250)
                {
                    e.nameRu = "Эхо · " + e.nameRu;
                    e.nameEn = "Echo · " + e.nameEn;
                }
            }

            var mods = new List<Modifier>();
            var pool = new List<Modifier>((Modifier[])Enum.GetValues(typeof(Modifier)));
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int n = r.Next(pool.Count);
                var m = pool[n];
                pool.RemoveAt(n);
                if ((m == Modifier.Armored && mods.Contains(Modifier.MagicShield)) || (m == Modifier.MagicShield && mods.Contains(Modifier.Armored)) || (m == Modifier.Fragile && mods.Contains(Modifier.Armored)) || (m == Modifier.MagicWeak && mods.Contains(Modifier.MagicShield)))
                    continue;
                mods.Add(m);
                switch (m)
                {
                    case Modifier.Armored:
                        e.physicalDefense += .4f;
                        e.magicDefense -= .1f;
                        break;
                    case Modifier.MagicShield:
                        e.magicDefense += .4f;
                        e.physicalDefense -= .1f;
                        break;
                    case Modifier.Fragile:
                        e.physicalDefense -= .3f;
                        break;
                    case Modifier.MagicWeak:
                        e.magicDefense -= .3f;
                        break;
                    case Modifier.Fast:
                        e.cooldown *= .75f;
                        e.power *= .7f;
                        break;
                    case Modifier.Heavy:
                        e.maxTime *= 1.2f;
                        e.power *= 1.1f;
                        e.cooldown *= 1.35f;
                        break;
                }
            }

            e.modifiers = mods.ToArray();
            e.cooldown = Mathf.Max(c.minimumCooldown, e.cooldown);
            e.physicalDefense = Mathf.Clamp(e.physicalDefense, -.4f, .6f);
            e.magicDefense = Mathf.Clamp(e.magicDefense, -.4f, .6f);
            if (e.physicalDefense > .35f)
                e.magicDefense = Mathf.Min(e.magicDefense, .15f);
            if (e.magicDefense > .35f)
                e.physicalDefense = Mathf.Min(e.physicalDefense, .15f);
            return e;
        }
    }
}
