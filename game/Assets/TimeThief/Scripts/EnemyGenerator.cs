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
        public BattleCondition condition;
        public Vector3 abilityValues;
        public Modifier[] modifiers;
        [NonSerialized] public Sprite sprite;
        [NonSerialized] public BossData boss;
        public bool Has(Modifier m) => Array.IndexOf(modifiers, m) >= 0;
    }

    public static class EnemyGenerator
    {
        public static EncounterType TypeAt(int level) => level % 25 == 0 ? EncounterType.Boss : level % 10 == 0 ? EncounterType.MiniBoss : EncounterType.Normal;

        // A seeded deck covers every silhouette before reusing it. Rotating successive
        // decks by one also prevents an identical card at their boundary.
        public static int TemplateIndex(GameConfig c, int level, int seed)
        {
            int count = c.enemies.Length;
            if (count <= 2) return (level - 1) % count;
            var order = new List<int>();
            foreach (int i in new[] { 6, 0, 7, 1, 2 })
                if (i < count && !order.Contains(i)) order.Add(i);
            var rest = new List<int>();
            for (int i = 0; i < count; i++) if (!order.Contains(i)) rest.Add(i);
            var random = new System.Random(seed);
            while (rest.Count > 0)
            {
                int i = random.Next(rest.Count);
                order.Add(rest[i]); rest.RemoveAt(i);
            }
            int n = level - 1;
            return order[(n % count + n / count % count) % count];
        }

        public static Encounter Generate(GameConfig c, int level, int seed)
        {
            level = Mathf.Max(1, level);
            var random = new System.Random(unchecked(seed + level * 7919));
            float step = level - 1, root = Mathf.Sqrt(step);
            float pressure = Mathf.Max(0, level - 5);
            var template = c.enemies[TemplateIndex(c, level, seed)];
            var type = TypeAt(level);
            var e = new Encounter
            {
                level = level, type = type, nameRu = template.nameRu, nameEn = template.nameEn,
                artKey = template.artKey, sprite = template.enemySprite, attackType = template.attackType,
                ability = level <= 5 ? Ability.None : template.ability, abilityValues = new Vector3(6, .3f, 1.5f),
                maxTime = c.baseEnemyTime + step * c.timePerLevel + root * c.timePerSqrtLevel + pressure * c.timePressure + Mathf.Pow(pressure, 1.65f) * c.timeCurve,
                power = c.baseEnemyPower + step * c.powerPerLevel + root * c.powerPerSqrtLevel + pressure * c.powerPressure + pressure * pressure * c.powerCurve,
                cooldown = Mathf.Max(c.minimumCooldown, c.baseCooldown - Mathf.Log(level) * c.cooldownDecay - Mathf.Log(1 + pressure / 20) * c.cooldownPressure),
                budget = Mathf.Log(level + 1, 2),
                condition = level <= 5 ? BattleCondition.QuietHour : (BattleCondition)random.Next(8)
            };
            // No random HP/power multipliers: both base values increase at every level,
            // including the level immediately following a boss.
            int count = level <= 5 ? 0 : level < 20 ? 1 : level < 75 ? 2 : 3;
            if (type == EncounterType.MiniBoss)
            {
                e.nameRu = "Страж · " + e.nameRu;
                e.nameEn = "Guardian · " + e.nameEn;
                e.ability = (Ability)(1 + random.Next(10));
                count = Mathf.Min(4, count + 1);
            }
            if (type == EncounterType.Boss)
            {
                int index = level <= 250 ? level / 25 - 1 : random.Next(c.bosses.Length);
                e.boss = c.bosses[index];
                var b = e.boss;
                e.nameRu = (level > 250 ? "Эхо · " : "") + b.bossName;
                e.nameEn = (level > 250 ? "Echo · " : "") + b.nameEn;
                e.hintRu = b.hintRu; e.hintEn = b.hintEn;
                e.artKey = b.artKey; e.sprite = b.bossSprite;
                e.attackType = b.attackType; e.ability = b.specialAbilityType;
                e.abilityValues = b.specialAbilityValues;
                count = level <= 250 ? 1 : 4;
            }
            var mods = new List<Modifier>();
            var pool = new List<Modifier>((Modifier[])Enum.GetValues(typeof(Modifier)));
            // Armor/weakness modifiers affect the counter-element, so their labels
            // describe a real change rather than the already resistant element.
            pool.Remove(e.attackType == AttackType.Physical ? Modifier.Armored : Modifier.MagicShield);
            pool.Remove(e.attackType == AttackType.Physical ? Modifier.Fragile : Modifier.MagicWeak);
            for (int i = 0; i < count; i++)
            {
                int index = random.Next(pool.Count);
                mods.Add(pool[index]); pool.RemoveAt(index);
            }
            e.modifiers = mods.ToArray();
            // The opposite element stays viable; abilities may briefly shield it.
            float counterDefense = mods.Contains(Modifier.Armored) || mods.Contains(Modifier.MagicShield) ? .15f : 0;
            if (mods.Contains(Modifier.Fragile) || mods.Contains(Modifier.MagicWeak)) counterDefense = -.15f;
            e.physicalDefense = e.attackType == AttackType.Physical ? c.sameTypeResistance : counterDefense;
            e.magicDefense = e.attackType == AttackType.Magic ? c.sameTypeResistance : counterDefense;
            return e;
        }

        public static string ConditionName(BattleCondition condition, bool english)
        {
            string[] ru = { "Тихий час", "Быстрый песок", "Чародейский туман", "Стальное эхо", "Рваное время", "Щедрый час", "Хрупкая защита", "Медленное стекло" };
            string[] en = { "Quiet hour", "Quick sand", "Arcane mist", "Steel echo", "Frayed time", "Abundant hour", "Brittle guard", "Slow glass" };
            return (english ? en : ru)[(int)condition];
        }

        public static string ConditionHint(BattleCondition condition, bool english)
        {
            string[] ru = { "Обычный поток времени", "Время утекает на 20% быстрее", "Урон магии +25%", "Урон клика +25%", "Возврат времени: 45% урона", "Возврат времени: 75% урона", "Твоя защита ослаблена на 35%", "Время утекает на 15% медленнее" };
            string[] en = { "Normal time flow", "Time drains 20% faster", "Magic damage +25%", "Tap damage +25%", "Recover 45% of damage", "Recover 75% of damage", "Your defenses are 35% weaker", "Time drains 15% slower" };
            return (english ? en : ru)[(int)condition];
        }
    }
}
