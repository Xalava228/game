using System;
using UnityEngine;

namespace TimeDebt.Core
{
    /// <summary>
    /// First playable-loop controller: time burns every frame, clicks transfer it from the enemy,
    /// and upgrades are paid from the same balance that keeps the player alive.
    /// </summary>
    public sealed class TimePrototypeController : MonoBehaviour
    {
        [Header("Encounter")]
        [SerializeField, Min(1f)] private float startingPlayerSeconds = 60f;
        [SerializeField, Min(1f)] private float startingEnemySeconds = 30f;
        [SerializeField, Min(0.01f)] private float stealPerClick = 0.5f;
        [SerializeField] private bool beginCombatOnAwake = true;

        private TimeBalance player;
        private TimeBalance enemy;

        public event Action<double> PlayerTimeChanged;
        public event Action<double> EnemyTimeChanged;
        public event Action<bool> EncounterEnded;

        public double PlayerSeconds => player?.Seconds ?? 0d;
        public double EnemySeconds => enemy?.Seconds ?? 0d;
        public bool IsCombatActive { get; private set; }

        private void Awake()
        {
            ResetEncounter();
            IsCombatActive = beginCombatOnAwake;
        }

        private void Update()
        {
            if (!IsCombatActive)
            {
                return;
            }

            player.DrainUpTo(Time.deltaTime);
            PlayerTimeChanged?.Invoke(player.Seconds);

            if (player.IsExpired)
            {
                EndEncounter(playerWon: false);
            }
        }

        public void ResetEncounter()
        {
            player = new TimeBalance(startingPlayerSeconds);
            enemy = new TimeBalance(startingEnemySeconds);
            IsCombatActive = false;
            BroadcastBalances();
        }

        public void BeginEncounter()
        {
            if (!player.IsExpired && !enemy.IsExpired)
            {
                IsCombatActive = true;
            }
        }

        public bool TryStealFromEnemy()
        {
            if (!IsCombatActive || player.IsExpired || enemy.IsExpired)
            {
                return false;
            }

            double stolen = enemy.DrainUpTo(stealPerClick);
            player.Add(stolen);
            BroadcastBalances();

            if (enemy.IsExpired)
            {
                EndEncounter(playerWon: true);
            }

            return stolen > 0d;
        }

        public bool TryBuyUpgrade(float costSeconds, float safetyReserveSeconds = 1f)
        {
            if (IsCombatActive)
            {
                return false;
            }

            bool purchased = player.TrySpend(costSeconds, safetyReserveSeconds);
            if (purchased)
            {
                PlayerTimeChanged?.Invoke(player.Seconds);
            }

            return purchased;
        }

        private void EndEncounter(bool playerWon)
        {
            IsCombatActive = false;
            EncounterEnded?.Invoke(playerWon);
        }

        private void BroadcastBalances()
        {
            PlayerTimeChanged?.Invoke(player.Seconds);
            EnemyTimeChanged?.Invoke(enemy.Seconds);
        }
    }
}
