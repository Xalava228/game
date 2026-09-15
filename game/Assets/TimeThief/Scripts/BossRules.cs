using UnityEngine;

namespace TimeThief
{
    // Pointer and target overlay share a square based on the stage's shorter side.
    public static class BossRules
    {
        public const int Version = 2;
        public const float Radius = .16f;
        public static int Kind(EnemyController e)
        {
            if (e.data.type != EncounterType.Boss || e.data.artKey == null) return 0;
            return int.TryParse(e.data.artKey.Replace("boss-", ""), out int n) && n >= 1 && n <= 10 ? n : 0;
        }
        public static bool HasTarget(EnemyController e)
        {
            switch (Kind(e))
            {
                case 1: return e.bossStage < 3;
                case 2: case 3: case 6: return true;
                case 4: return e.bossWindow <= 0;
                case 7: return e.Telegraph;
                case 8: return e.bossProgress > .05f;
                default: return false;
            }
        }
        public static float X(EnemyController e)
        {
            switch (Kind(e))
            {
                case 1: return .30f;
                case 2: return e.bossStage == 0 ? .28f : e.bossStage == 1 ? .72f : .5f;
                case 3: return e.bossProgress <= .5f ? .28f : .72f;
                case 6: return .5f + Mathf.Sin(e.elapsed * .75f) * .23f;
                case 7: return .68f;
                default: return .5f;
            }
        }
        public static float Y(EnemyController e)
        {
            switch (Kind(e))
            {
                case 2: return e.bossStage == 2 ? .72f : e.bossStage == 3 ? .5f : .43f;
                case 4: return .28f;
                case 6: return .5f + Mathf.Cos(e.elapsed * .75f) * .23f;
                case 7: return .69f;
                case 8: return .40f;
                default: return .5f;
            }
        }
        static bool Circle(float x, float y, float cx, float cy)
        {
            float dx = x - cx, dy = y - cy;
            return dx * dx + dy * dy <= Radius * Radius;
        }
        public static bool InTarget(EnemyController e, float x, float y) =>
            Circle(x, y, X(e), Y(e)) || Kind(e) == 3 && Circle(x, y, 1 - X(e), .5f);

        public static void Tick(EnemyController e, float dt)
        {
            int n = Kind(e);
            if (n == 0) return;
            e.bossIdle = Mathf.Min(10, e.bossIdle + dt);
            e.bossWindow = Mathf.Max(0, e.bossWindow - dt);
            if ((n == 2 || n == 10) && e.bossStage == 3 && e.bossWindow <= 0) e.bossStage = 0;
            if (n == 5 && e.bossIdle >= .45f && e.bossProgress >= 3)
            {
                float burst = e.bossStoredDamage * 2;
                e.bossProgress = e.bossStoredDamage = 0;
                e.bossWindow = .8f;
                e.Burst(burst);
            }
            if (n == 6) e.bossProgress = Mathf.Max(0, e.bossProgress - dt * .35f);
            if (n == 9 && e.bossIdle > .12f)
            {
                float cooling = Mathf.Min(dt, e.bossIdle - .12f);
                e.bossProgress = Mathf.Max(0, e.bossProgress - cooling * .5f);
                if (e.bossProgress <= .35f) e.bossStage = 0;
            }
        }
        public static float Multiplier(EnemyController e, bool magic, float dt, float x, float y)
        {
            int n = Kind(e);
            if (n == 0) return 1;
            bool freshGesture = e.bossIdle > .10f;
            e.bossIdle = 0;
            float effort = magic ? dt * 2.5f : 1;
            bool target = InTarget(e, x, y);
            switch (n)
            {
                case 1: // Destroyed armor remains broken for the rest of this fight.
                    if (e.bossStage == 3) return 1.9f;
                    if (!target) return .55f + e.bossStage * .2f;
                    e.bossProgress += effort;
                    if (e.bossProgress >= 2) { e.bossProgress -= 2; e.bossStage++; }
                    return 1.2f + e.bossStage * .25f;
                case 2: // Ordered seals with a grace period for moving between them.
                    if (e.bossStage == 3) return target ? 2.3f : 1;
                    if (e.bossWindow > 0) return 1;
                    if (!target) return .55f;
                    e.bossProgress += effort;
                    if (e.bossProgress >= 2)
                    {
                        e.bossProgress = 0;
                        e.bossStage++;
                        e.bossWindow = e.bossStage == 3 ? 4 : .6f;
                    }
                    return 1;
                case 3: // Both faces accept damage; balance cumulative load.
                    if (!target) return .55f;
                    e.bossProgress = Mathf.Clamp(e.bossProgress + (x < .5f ? 1 : -1) * effort * .12f, .05f, .95f);
                    return e.bossProgress >= .25f && e.bossProgress <= .75f ? 1.9f : .6f;
                case 4: // Roots disable healing, without making the crown invulnerable.
                    if (e.bossWindow > 0) return 1.4f;
                    if (target)
                    {
                        e.bossProgress += effort;
                        if (e.bossProgress >= 2) { e.bossProgress = 0; e.bossWindow = 8; }
                    }
                    return 1;
                case 5: // Charge from real damage, then release to detonate.
                    e.bossProgress = Mathf.Min(3, e.bossProgress + effort);
                    return .85f;
                case 6: // Track continuously to build a damage bonus.
                    if (!target) return .55f;
                    e.bossProgress = Mathf.Min(1, e.bossProgress + (magic ? dt * 1.35f : .35f));
                    return 1.2f + e.bossProgress;
                case 7: // A reaction parry, not a permanently required target.
                    if (e.Telegraph && target)
                    {
                        e.bossProgress += magic ? dt / .18f : 1;
                        if (e.bossProgress >= 1)
                        {
                            e.timer = Mathf.Max(1.5f, e.data.cooldown);
                            e.bossProgress = 0; e.bossWindow = 1.2f;
                        }
                    }
                    else e.bossProgress = 0;
                    return e.bossWindow > 0 ? 1.8f : 1;
                case 8: return 1; // Swallowed time is reclaimed separately.
                case 9:
                    if (e.bossStage == 1) return .45f;
                    e.bossProgress = Mathf.Min(1, e.bossProgress + (magic ? dt * .38f : .13f));
                    if (e.bossProgress >= 1) e.bossStage = 1;
                    return 1.7f;
                case 10:
                    if (e.bossStage == 3) return 2.5f;
                    bool beatOpen = e.elapsed % 2 < .85f;
                    if (freshGesture)
                    {
                        int beat = (int)(e.elapsed / 2) + 1;
                        if (!beatOpen) e.bossStage = 0;
                        else if (e.bossProgress != beat)
                        {
                            e.bossProgress = beat;
                            if (++e.bossStage >= 3) e.bossWindow = 3;
                        }
                    }
                    return beatOpen ? 1.8f : .55f;
            }
            return 1;
        }
        public static float Reclaim(EnemyController e, bool magic, float dt, float x, float y)
        {
            if (Kind(e) != 8 || !InTarget(e, x, y)) return 0;
            float returned = Mathf.Min(e.bossProgress, magic ? dt * 6 : 2);
            e.bossProgress -= returned;
            return returned;
        }
        public static float Meter(EnemyController e)
        {
            switch (Kind(e))
            {
                case 1: return (e.bossStage + e.bossProgress / 2) / 3;
                case 2: return e.bossStage == 3 ? e.bossWindow / 4 : (e.bossStage + e.bossProgress / 2) / 3;
                case 3: return e.bossProgress;
                case 4: return e.bossWindow > 0 ? e.bossWindow / 8 : e.bossProgress / 2;
                case 5: return e.bossProgress / 3;
                case 6: case 9: return e.bossProgress;
                case 7: return e.bossWindow > 0 ? 1 : e.bossProgress;
                case 8: return Mathf.Min(1, e.bossProgress / 5);
                case 10: return e.bossStage == 3 ? e.bossWindow / 3 : e.elapsed % 2 / 2;
                default: return 0;
            }
        }
        static readonly string[] Ru = { "",
            "Разбей 3 пластины на котле. Каждая навсегда ослабляет броню и замедляет поезд.",
            "Две атаки или 0,8 с удержания на каждой из трёх печатей. Между печатями есть 0,6 с для перехода.",
            "Оба циферблата принимают урон. Держи баланс в средней половине шкалы: атакуй менее нагруженную сторону.",
            "Две атаки или 0,8 с удержания на корнях отключают лечение на 8 с. Затем можно бить в любое место.",
            "Заряди кузницу тремя атаками или 1,2 с удержания. Отпусти на 0,45 с: накопленный урон взорвётся.",
            "Веди зажатый палец за луной или кликай по ней. Точное наведение постепенно усиливает урон.",
            "Во время замаха попади в жало: один клик или 0,18 с магии отменяет ближайший удар.",
            "Пасть хранит украденное у тебя время. Атакуй жемчужину, чтобы вернуть его полностью, пока есть место в запасе.",
            "Чередуй атаки с паузами. Жар уходит только без атак; при перегреве остынь до 35%.",
            "Начинай отдельный клик или удержание на каждом световом такте. Три такта открывают колокол; атака мимо ритма сбивает серию."
        };
        static readonly string[] En = { "",
            "Break 3 plates on the boiler. Each permanently weakens armor and slows the train.",
            "Two taps or 0.8 s of holding per seal. Break all three; a 0.6 s transition gives you time to move.",
            "Both faces take damage. Keep the balance in the middle half of the bar: attack the less loaded face.",
            "Two taps or 0.8 s of holding on the roots disable healing for 8 s. Then hit anywhere.",
            "Charge with three taps or 1.2 s of holding. Release for 0.45 s to detonate the stored damage.",
            "Drag your held finger with the moon, or tap it. Accurate tracking gradually increases damage.",
            "During the wind-up, hit the stinger: one tap or 0.18 s of magic cancels the next strike.",
            "The maw stores time stolen from you. Attack the pearl to reclaim it in full, up to your time capacity.",
            "Alternate attacks and rests. Heat drops only without attacks; after overheating, cool down to 35%.",
            "Start a separate tap or hold on each light beat. Three beats open the bell; an offbeat attack resets the combo."
        };
        public static string Hint(EnemyController e, bool en) => (en ? En : Ru)[Kind(e)];
        public static string Status(EnemyController e, bool en)
        {
            string s = "", t = "";
            switch (Kind(e))
            {
                case 1: s = e.bossStage == 3 ? "БРОНЯ РАЗРУШЕНА · бей куда угодно" : "Котёл · пластин осталось: " + (3 - e.bossStage); t = e.bossStage == 3 ? "ARMOR BROKEN · hit anywhere" : "Boiler · plates left: " + (3 - e.bossStage); break;
                case 2: s = e.bossStage == 3 ? "ВРАТА ОТКРЫТЫ · бей в центр" : "Печать " + (e.bossStage + 1) + "/3 · " + (e.bossWindow > 0 ? "переводи палец" : "бей по кругу"); t = e.bossStage == 3 ? "GATE OPEN · hit the center" : "Seal " + (e.bossStage + 1) + "/3 · " + (e.bossWindow > 0 ? "move to the next ring" : "attack the ring"); break;
                case 3: s = "Баланс " + (e.bossProgress * 100).ToString("0") + "% · " + (e.bossProgress > .5f ? "атакуй справа" : "атакуй слева"); t = "Balance " + (e.bossProgress * 100).ToString("0") + "% · " + (e.bossProgress > .5f ? "attack right" : "attack left"); break;
                case 4: s = e.bossWindow > 0 ? "ЛЕЧЕНИЕ ОТКЛЮЧЕНО · " + e.bossWindow.ToString("0.0") + " с" : "Сад лечится · атакуй корни"; t = e.bossWindow > 0 ? "HEALING STOPPED · " + e.bossWindow.ToString("0.0") + " s" : "Healing · attack the roots"; break;
                case 5: s = e.bossProgress >= 3 ? "ЗАРЯЖЕНО · отпусти для взрыва" : e.bossWindow > 0 ? "РАЗРЯД!" : "Заряд " + (e.bossProgress / 3 * 100).ToString("0") + "% · атакуй"; t = e.bossProgress >= 3 ? "CHARGED · release to detonate" : e.bossWindow > 0 ? "DETONATED!" : "Charge " + (e.bossProgress / 3 * 100).ToString("0") + "% · attack"; break;
                case 6: s = "Следуй за луной · наведение " + (e.bossProgress * 100).ToString("0") + "%"; t = "Track the moon · lock " + (e.bossProgress * 100).ToString("0") + "%"; break;
                case 7: s = e.bossWindow > 0 ? "УДАР СБИТ!" : e.Telegraph ? "ЖАЛО · сбей удар!" : "Атакуй тело · следи за замахом"; t = e.bossWindow > 0 ? "STRIKE CANCELED!" : e.Telegraph ? "STINGER · interrupt!" : "Attack the body · watch the wind-up"; break;
                case 8: s = "Жемчужина · верни " + e.bossProgress.ToString("0.0") + " с жизни"; t = "Pearl · reclaim " + e.bossProgress.ToString("0.0") + " s of life"; break;
                case 9: s = e.bossStage == 1 ? "ПЕРЕГРЕВ · отпусти до 35%" : "Жар " + (e.bossProgress * 100).ToString("0") + "% · пауза охлаждает"; t = e.bossStage == 1 ? "OVERHEATED · release until 35%" : "Heat " + (e.bossProgress * 100).ToString("0") + "% · rest to cool"; break;
                case 10: s = e.bossStage == 3 ? "КОЛОКОЛ ОТКРЫТ · атакуй!" : e.elapsed % 2 < .85f ? "БЕЙ В ТАКТ · " + e.bossStage + "/3" : "ОТПУСТИ · жди следующего такта"; t = e.bossStage == 3 ? "BELL OPEN · attack!" : e.elapsed % 2 < .85f ? "ATTACK ON BEAT · " + e.bossStage + "/3" : "RELEASE · wait for the next beat"; break;
            }
            return en ? t : s;
        }
    }
}
