using System;
using UnityEngine;

namespace TimeThief
{
    [Serializable]
    public sealed class PlayerStats
    {
        public float CurrentTime = 5, MaxTime = 5, Attack = 1, CritChance = .05f, CritMultiplier = 2, Armor = 0, MagicResistance = 0;
        public PlayerStats Copy() => (PlayerStats)MemberwiseClone();
        public float AddTime(float amount)
        {
            float actual = Mathf.Min(Mathf.Max(0, amount), Mathf.Max(0, MaxTime - CurrentTime));
            CurrentTime += actual;
            return actual;
        }

        public float LoseTime(float amount)
        {
            float actual = Mathf.Min(CurrentTime, Mathf.Max(0, amount));
            CurrentTime -= actual;
            return actual;
        }

        public static float Reduced(float power, float defense, float constant) => Mathf.Max(0, power) * Mathf.Max(.001f, constant) / (Mathf.Max(0, defense) + Mathf.Max(.001f, constant));
        public void Grow(PlayerStats g)
        {
            MaxTime += g.MaxTime;
            Attack += g.Attack;
            CritChance = Mathf.Min(.75f, CritChance + g.CritChance);
            CritMultiplier += g.CritMultiplier;
            Armor += g.Armor;
            MagicResistance += g.MagicResistance;
        }

        public void Upgrade(Stat s, float amount, bool percent = false)
        {
            switch (s)
            {
                case Stat.MaxTime:
                    MaxTime += amount;
                    AddTime(amount);
                    break;
                case Stat.Attack:
                    Attack = percent ? Attack * (1 + amount) : Attack + amount;
                    break;
                case Stat.CritChance:
                    CritChance = Mathf.Min(.75f, CritChance + amount);
                    break;
                case Stat.CritMultiplier:
                    CritMultiplier += amount;
                    break;
                case Stat.Armor:
                    Armor += amount;
                    break;
                case Stat.MagicResistance:
                    MagicResistance += amount;
                    break;
            }
        }

        public float Get(Stat s)
        {
            switch (s)
            {
                case Stat.MaxTime:
                    return MaxTime;
                case Stat.Attack:
                    return Attack;
                case Stat.CritChance:
                    return CritChance;
                case Stat.CritMultiplier:
                    return CritMultiplier;
                case Stat.Armor:
                    return Armor;
                default:
                    return MagicResistance;
            }
        }

        public bool Valid()
        {
            foreach (Stat s in Enum.GetValues(typeof(Stat)))
            {
                float v = Get(s);
                if (float.IsNaN(v) || float.IsInfinity(v) || v < 0 || v > 1e20f)
                    return false;
            }

            return MaxTime >= 1 && Attack > 0 && !float.IsNaN(CurrentTime) && !float.IsInfinity(CurrentTime) && CurrentTime >= 0 && CurrentTime <= MaxTime + .01f;
        }
    }
}
