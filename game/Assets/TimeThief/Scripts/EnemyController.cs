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
        public bool Shielded => (data.Has(Modifier.Shield) || data.ability == Ability.PulseShield) && elapsed % Cycle > Cycle - .8f;
        public void Tick(float dt)
        {
            elapsed += dt;
            timer -= dt;
            if (data.ability == Ability.Regenerate && !Shielded)
                time = Mathf.Min(data.maxTime, time + Mathf.Min(game.player.Attack * .25f, data.maxTime * .006f) * dt);
            if (timer <= 0)
            {
                Attack();
                float speed = 1;
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
            float v = magic ? data.magicDefense : data.physicalDefense;
            if (data.Has(Modifier.Unstable) || data.ability == Ability.SwitchDefense)
            {
                bool phase = Mathf.FloorToInt(elapsed / Mathf.Max(3, Cycle * .65f)) % 2 == 0;
                v += phase == magic ? .35f : -.2f;
            }

            if (Shielded)
                v += .6f;
            if (magic && (data.Has(Modifier.Hot) || data.ability == Ability.Heat) && holdSeconds > 2)
                v += .4f;
            return Mathf.Clamp(v, -.4f, .8f);
        }

        public void Hit(bool magic, float dt = 1)
        {
            if (!game.IsFighting)
                return;
            bool crit = !magic && Random.value < Mathf.Min(.9f, game.player.CritChance + (game.shop.Has(BuffType.Critical) ? .15f : 0));
            float power = game.player.Attack * (magic ? game.config.magicStealPerSecond * dt : game.config.physicalSteal);
            if (game.shop.Has(magic ? BuffType.MagicCandy : BuffType.SuperClick))
                power *= 1.5f;
            if (crit)
                power *= game.player.CritMultiplier;
            power *= 1 - Defense(magic);
            float stolen = Mathf.Min(time, Mathf.Max(0, power));
            time -= stolen;
            game.player.AddTime(stolen);
            if (!magic)
            {
                physicalHits++;
                game.ui.Hit(stolen, crit);
                game.music.Sfx(crit ? game.config.criticalSound : game.config.physicalHitSound);
            }

            if (time <= .0001f)
            {
                game.Win();
                return;
            }

            if (!magic && (data.Has(Modifier.Thorny) || data.ability == Ability.Thorns) && physicalHits % 4 == 0)
            {
                float reflected = game.player.LoseTime(Mathf.Min(.35f, game.player.MaxTime * .06f));
                game.ui.EnemyHit(reflected, false, false, true);
                if (game.player.CurrentTime <= 0)
                    game.Lose();
            }
        }

        void Attack()
        {
            if (!game.IsFighting)
                return;
            attackCount++;
            bool magic = data.attackType == AttackType.Magic;
            if (data.ability == Ability.MagicSurge && attackCount % 3 == 0)
                magic = !magic;
            float p = data.power;
            bool heavy = data.ability == Ability.HeavyStrike && attackCount % 3 == 0;
            if (heavy)
                p *= 1.5f;
            float defense = magic ? game.player.MagicResistance : game.player.Armor;
            if (game.shop.Has(magic ? BuffType.Resistance : BuffType.Armor))
                defense = defense * 1.3f + 3;
            float damage = PlayerStats.Reduced(p, defense, magic ? game.config.resistanceConstant : game.config.armorConstant);
            float taken = game.player.LoseTime(damage);
            time = Mathf.Min(data.maxTime, time + taken * (data.Has(Modifier.Vampire) || data.ability == Ability.Leech ? 1.4f : 1));
            game.ui.EnemyHit(taken, magic, heavy);
            game.music.Sfx(magic ? game.config.enemyMagicAttackSound : game.config.enemyPhysicalAttackSound);
            if (game.player.CurrentTime <= 0)
                game.Lose();
        }
    }
}
