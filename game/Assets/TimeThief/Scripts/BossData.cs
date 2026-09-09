using UnityEngine;

namespace TimeThief
{
    [CreateAssetMenu(menuName = "Game/Boss Data")]
    public sealed class BossData : ScriptableObject
    {
        public string bossName, nameEn, hintRu, hintEn, artKey = "witch";
        public Sprite bossSprite;
        public Color bossThemeColor = new Color(.5f, .4f, .7f);
        public AudioClip bossMusic;
        public float baseMaxTime = 12, physicalDefense, magicDefense, attackPower = .7f, attackCooldown = 3.5f, rewardMultiplier = 1;
        public AttackType attackType;
        public Ability specialAbilityType;
        public Vector3 specialAbilityValues = new Vector3(6, .3f, 1.5f);
    }
}
