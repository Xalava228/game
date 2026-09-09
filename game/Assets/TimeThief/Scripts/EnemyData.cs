using UnityEngine;

namespace TimeThief
{
    [CreateAssetMenu(menuName = "Game/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        public string nameRu, nameEn, artKey = "moth";
        public Sprite enemySprite;
        public Color themeColor = new Color(.3f, .7f, .65f);
        public AttackType attackType;
    }
}
