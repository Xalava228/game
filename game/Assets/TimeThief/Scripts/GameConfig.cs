using UnityEngine;

namespace TimeThief
{
    public enum GameState
    {
        Waiting,
        Intro,
        Fighting,
        Victory,
        RewardSelection,
        Stats,
        Shop,
        GameOver,
        Transition
    }

    public enum AttackType
    {
        Physical,
        Magic
    }

    public enum EncounterType
    {
        Normal,
        MiniBoss,
        Boss
    }

    public enum Stat
    {
        MaxTime,
        Attack,
        CritChance,
        CritMultiplier,
        Armor,
        MagicResistance
    }

    public enum Modifier
    {
        Armored,
        MagicShield,
        Fragile,
        MagicWeak,
        Fast,
        Heavy,
        Vampire,
        Thorny,
        Unstable,
        Shield,
        Fury,
        Hot
    }

    public enum Ability
    {
        None,
        Accelerate,
        PulseShield,
        SwitchDefense,
        Regenerate,
        HeavyStrike,
        MagicSurge,
        Thorns,
        Leech,
        Heat,
        FinalHour
    }

    public enum BuffType
    {
        Armor,
        Resistance,
        SuperClick,
        MagicCandy,
        Critical,
        Freeze,
        DoubleShards
    }

    [CreateAssetMenu(menuName = "Game/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Starting stats")]
        public PlayerStats startingStats = new PlayerStats();
        [Header("Input / combat")]
        public float holdThreshold = .25f, physicalSteal = 1, magicStealPerSecond = 2.6f, armorConstant = 10, resistanceConstant = 10, telegraphDuration = .6f, minimumCooldown = .8f;
        [Header("Enemy scaling")]
        public float baseEnemyTime = 3, timePerLevel = .18f, timePerSqrtLevel = .6f, baseEnemyPower = .25f, powerPerSqrtLevel = .12f, baseCooldown = 4.5f, cooldownDecay = .22f;
        [Header("Growth after every victory")]
        public PlayerStats growth = new PlayerStats{MaxTime = .04f, CurrentTime = 0, Attack = .015f, CritChance = .0001f, CritMultiplier = .001f, Armor = .02f, MagicResistance = .02f};
        [Header("Content / optional sprite and audio slots")]
        public EnemyData[] enemies;
        public BossData[] bosses;
        public AudioClip normalBattleMusic, proceduralBossMusic;
        public AudioClip physicalHitSound, criticalSound, magicStartSound, magicLoopSound, enemyPhysicalAttackSound, enemyMagicAttackSound, victorySound, miniBossIntroSound, bossIntroSound, upgradeSound, shopSound, gameOverSound;
        [Header("Music")]
        public float crossfadeDuration = 1.5f, musicVolume = .7f;
    }
}
