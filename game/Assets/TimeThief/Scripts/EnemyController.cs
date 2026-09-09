using UnityEngine;

namespace TimeThief
{
    public sealed class EnemyController
    {
        public Encounter data;
        public float time, timer, elapsed, holdSeconds;
        public int attackCount, physicalHits;
        readonly GameManager game;
        public bool Telegraph => timer <= game.config.telegraphDuration;
        public EnemyController(GameManager g, Encounter d)
        {
            game = g;
            data = d;
            time = d.maxTime;
            timer = d.cooldown;
        }

        float Cycle => Mathf.Max(2, data.abilityValues.x);
        public bool UsesMagic
        {
            get
            {
                bool magic = data.attackType == AttackType.Magic;
                if (data.ability == Ability.SwitchDefense && Mathf.FloorToInt(elapsed / 4) % 2 == 1) magic = !magic;
                if (data.ability == Ability.MagicSurge && (attackCount + 1) % 3 == 0) magic = !magic;
                return magic;
            }
        }
        public float FlowRate => game.config.continuousDrain * (data.condition == BattleCondition.QuickSand ? 1.2f : data.condition == BattleCondition.SlowGlass ? .85f : 1);
        public float RecoveryFraction => data.condition == BattleCondition.FrayedTime ? .45f : data.condition == BattleCondition.Abundance ? .75f : game.config.playerRecoveryFraction;
        public float DrainPlayer(float amount)
        {
            float taken = game.player.LoseTime(amount);
            // All time lost to the keeper is transferred, even above its starting reserve.
            time += taken;
            return taken;
        }
        public bool Shielded => (data.Has(Modifier.Shield) || data.ability == Ability.PulseShield) && elapsed % Cycle > Cycle - .8f;
        public void Tick(float dt)
        {
            elapsed += dt;
            timer -= dt;
            if (data.ability == Ability.Regenerate && !Shielded && time < data.maxTime)
                time = Mathf.Min(data.maxTime, time + Mathf.Min(game.player.Attack * .25f, data.maxTime * .006f) * dt);
            if (timer <= 0)
            {
                Attack();
                float speed = data.Has(Modifier.Fast) ? .85f : 1;
                if (data.Has(Modifier.Fury) && time < data.maxTime * .3f)
                    speed *= .75f;
                if (data.ability == Ability.Accelerate)
                    speed *= Mathf.Max(.55f, 1 - attackCount * .055f);
                if (data.ability == Ability.FinalHour && time < data.maxTime * .35f)
                    speed *= .65f;
                timer = Mathf.Max(game.config.minimumCooldown, data.cooldown * speed);
            }
        }

        public float Defense(bool magic)
        {
            if (magic == UsesMagic) return game.config.sameTypeResistance;
            float v = Mathf.Min(data.physicalDefense, data.magicDefense);
            if (data.Has(Modifier.Unstable)) v += Mathf.Sin(elapsed * 1.7f) * .1f;

            if (Shielded)
                v += .35f;
            if (magic && (data.Has(Modifier.Hot) || data.ability == Ability.Heat) && holdSeconds > 2)
                v += .2f;
            return Mathf.Clamp(v, -.3f, .65f);
        }

        public void Hit(bool magic, float dt = 1)
        {
            if (!game.IsFighting)
                return;
            bool crit = !magic && Random.value < Mathf.Min(.9f, game.player.CritChance + (game.shop.Has(BuffType.Critical) ? .15f : 0));
            float power = game.player.Attack * (magic ? game.config.magicStealPerSecond * dt : game.config.physicalSteal);
            if ((magic && data.condition == BattleCondition.ArcaneMist) || (!magic && data.condition == BattleCondition.SteelEcho)) power *= 1.25f;
            if (game.shop.Has(magic ? BuffType.MagicCandy : BuffType.SuperClick))
                power *= 1.5f;
            if (crit)
                power *= game.player.CritMultiplier;
            power *= 1 - Defense(magic);
            float stolen = Mathf.Min(time, Mathf.Max(0, power));
            time -= stolen;
            float recovered = game.player.AddTime(stolen * RecoveryFraction);
            if (!magic)
            {
                physicalHits++;
                game.ui.Hit(stolen, recovered, crit);
                game.music.Sfx(crit ? game.config.criticalSound : game.config.physicalHitSound);
            }

            if (time <= .0001f)
            {
                game.Win();
                return;
            }

            if (!magic && (data.Has(Modifier.Thorny) || data.ability == Ability.Thorns) && physicalHits % 4 == 0)
            {
                float reflected = DrainPlayer(Mathf.Min(.35f, game.player.MaxTime * .06f));
                game.ui.EnemyHit(reflected, false, false, true);
                if (game.player.CurrentTime <= 0)
                    game.Lose();
            }
        }

        void Attack()
        {
            if (!game.IsFighting)
                return;
            bool magic = UsesMagic;
            attackCount++;
            float p = data.power;
            bool heavy = data.ability == Ability.HeavyStrike && attackCount % 3 == 0;
            if (heavy)
                p *= 1.5f;
            float defense = magic ? game.player.MagicResistance : game.player.Armor;
            if (game.shop.Has(magic ? BuffType.Resistance : BuffType.Armor))
                defense = defense * 1.3f + 3;
            if (data.condition == BattleCondition.BrittleGuard) defense *= .65f;
            float damage = PlayerStats.Reduced(p, defense, magic ? game.config.resistanceConstant : game.config.armorConstant);
            if (data.Has(Modifier.Vampire) || data.ability == Ability.Leech) damage *= 1.2f;
            if (data.Has(Modifier.Heavy)) damage *= 1.1f;
            float taken = DrainPlayer(damage);
            game.ui.EnemyHit(taken, magic, heavy);
            game.music.Sfx(magic ? game.config.enemyMagicAttackSound : game.config.enemyPhysicalAttackSound);
            if (game.player.CurrentTime <= 0)
                game.Lose();
        }
    }
}
