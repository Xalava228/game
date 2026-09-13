using UnityEngine;

namespace TimeThief
{
    // Coordinates refer to the same enemy rectangle used by pointer input and the target overlay.
    public static class BossRules
    {
        public static int Kind(EnemyController e)
        {
            if (e.data.type != EncounterType.Boss || e.data.artKey == null) return 0;
            return int.TryParse(e.data.artKey.Replace("boss-", ""), out int n) && n >= 1 && n <= 10 ? n : 0;
        }
        public static bool HasTarget(EnemyController e)
        {
            int n = Kind(e);
            return n == 1 || n == 2 || n == 3 || n == 4 || n == 6 || n == 7;
        }
        public static float X(EnemyController e)
        {
            switch (Kind(e))
            {
                case 1: return (int)(e.elapsed / 3) % 2 == 0 ? .30f : .70f;
                case 2: return e.bossWindow > 0 ? .5f : new[] { .28f, .72f, .5f }[e.bossStage % 3];
                case 3: return e.bossStage % 2 == 0 ? .28f : .72f;
                case 6: return .5f + Mathf.Sin(e.elapsed * .9f) * .24f;
                case 7: return e.Telegraph ? .73f : .48f;
                default: return .5f;
            }
        }
        public static float Y(EnemyController e)
        {
            switch (Kind(e))
            {
                case 2: return e.bossWindow > 0 ? .5f : e.bossStage % 3 == 2 ? .72f : .43f;
                case 4: return e.bossWindow > 0 ? .72f : .28f;
                case 6: return .5f + Mathf.Cos(e.elapsed * .9f) * .24f;
                case 7: return e.Telegraph ? .72f : .40f;
                default: return .5f;
            }
        }
        public static bool InTarget(EnemyController e, float x, float y)
        {
            float dx = x - X(e), dy = y - Y(e);
            return dx * dx + dy * dy <= .17f * .17f;
        }
        public static void Tick(EnemyController e, float dt)
        {
            e.bossWindow = Mathf.Max(0, e.bossWindow - dt);
            if (Kind(e) == 9) e.bossProgress = Mathf.Max(0, e.bossProgress - dt * .22f);
        }
        public static void AfterStrike(EnemyController e)
        {
            if (Kind(e) == 5) e.bossWindow = 1.25f;
        }
        public static float Multiplier(EnemyController e, bool magic, float dt, float x, float y)
        {
            int n = Kind(e);
            if (n == 0) return 1;
            float effort = magic ? dt * 2.5f : 1;
            if (HasTarget(e) && !InTarget(e, x, y)) return .18f;
            switch (n)
            {
                case 1: return 1.6f; // Follow the lit carriage, which changes every three seconds.
                case 2:
                    if (e.bossWindow > 0) return 2.2f;
                    e.bossProgress += effort;
                    if (e.bossProgress >= 1)
                    {
                        e.bossProgress = 0;
                        if (++e.bossStage >= 3) { e.bossStage = 0; e.bossWindow = 3; }
                    }
                    return .65f;
                case 3:
                    e.bossProgress += effort;
                    if (e.bossProgress >= 1) { e.bossStage = 1 - e.bossStage % 2; e.bossProgress = 0; }
                    return 1.8f;
                case 4:
                    if (e.bossWindow > 0) return 2.3f;
                    e.bossProgress += effort;
                    if (e.bossProgress >= 3) { e.bossProgress = 0; e.bossWindow = 4; }
                    return .6f;
                case 5: return e.bossWindow > 0 ? 2.4f : .35f;
                case 6: return 1.8f;
                case 7:
                    if (e.Telegraph)
                    {
                        e.bossProgress += effort;
                        if (e.bossProgress >= 1)
                        {
                            e.timer = Mathf.Max(1, e.data.cooldown);
                            e.bossProgress = 0; e.bossWindow = 1;
                        }
                    }
                    else e.bossProgress = 0;
                    return 1.5f;
                case 8: return e.elapsed % 6 < 4.5f ? 1.8f : .12f;
                case 9:
                    if (e.bossWindow > 0) return .12f;
                    e.bossProgress += magic ? dt * .52f : .16f;
                    if (e.bossProgress >= 1) { e.bossProgress = 1; e.bossWindow = 2.5f; }
                    return 1.8f;
                case 10:
                    if (e.bossWindow > 0) return 2.5f;
                    if (e.elapsed % 2 >= .7f) return .20f;
                    // One seal per beat, even when several taps arrive in the same window.
                    int beat = (int)(e.elapsed / 2) + 1;
                    if (e.bossProgress != beat)
                    {
                        e.bossProgress = beat;
                        if (++e.bossStage >= 3) { e.bossStage = 0; e.bossWindow = 3; }
                    }
                    return 1.8f;
            }
            return 1;
        }
        public static string Hint(EnemyController e, bool en)
        {
            string[] ru = { "", "Следуй за светящимся вагоном. Цель меняется раз в 3 секунды.", "Раскрой три печати по порядку. Затем атакуй открытый центр.", "Чередуй левый и правый циферблаты. Удержание: переводи палец вслед за целью.", "Разбей корни тремя попаданиями, затем атакуй крону. Удержание тоже разбивает корни.", "После удара кузница остывает на 1,25 секунды. В этот момент твой урон выше.", "Веди палец за светящейся луной по орбите или кликай по ней.", "Во время замаха атакуй подсвеченное жало: это отменяет ближайший удар.", "Атакуй на вдохе. На выдохе пасть закрыта, и урон почти не проходит.", "Атаки накапливают жар. Отпусти, чтобы остыть: при перегреве урон резко падает.", "Попадай в световые доли. Три разные доли открывают колокол на 3 секунды." };
            string[] eng = { "", "Follow the lit carriage. The target changes every 3 seconds.", "Break three seals in order, then attack the open center.", "Alternate clock faces. While holding, drag your finger to the next target.", "Break the roots with three hits, then attack the crown. Holding also breaks roots.", "The forge cools for 1.25 seconds after striking. Deal extra damage in that window.", "Follow the glowing moon around its orbit with taps or a dragging hold.", "Hit the lit stinger during the wind-up to cancel the next strike.", "Attack while it inhales. During exhalation, the closed maw resists almost all damage.", "Attacks build heat. Release to cool down: overheating sharply reduces damage.", "Hit on the light pulses. Three separate beats open the bell for 3 seconds." };
            return (en ? eng : ru)[Kind(e)];
        }
        public static string Status(EnemyController e, bool en)
        {
            string s = "", t = "";
            switch (Kind(e))
            {
                case 1: s = "Цель: светящийся вагон"; t = "Follow the lit carriage"; break;
                case 2: s = e.bossWindow > 0 ? "Врата открыты — бей в центр!" : "Печать " + (e.bossStage + 1) + " из 3 · бей по кругу"; t = e.bossWindow > 0 ? "Gate open — hit the center!" : "Seal " + (e.bossStage + 1) + " of 3 · hit the ring"; break;
                case 3: s = e.bossStage % 2 == 0 ? "Левый циферблат" : "Правый циферблат"; t = e.bossStage % 2 == 0 ? "Left clock face" : "Right clock face"; break;
                case 4: s = e.bossWindow > 0 ? "Корни сломаны — бей по кроне!" : "Корни: " + (int)e.bossProgress + "/3 · бей по кругу"; t = e.bossWindow > 0 ? "Roots broken — hit the crown!" : "Roots: " + (int)e.bossProgress + "/3 · hit the ring"; break;
                case 5: s = e.bossWindow > 0 ? "ОСТЫВАЕТ — атакуй!" : "Броня раскалена · жди его удара"; t = e.bossWindow > 0 ? "COOLING — attack!" : "Hot armor · wait for its strike"; break;
                case 6: s = "Держись светящейся луны"; t = "Stay on the glowing moon"; break;
                case 7: s = e.bossWindow > 0 ? "УДАР СБИТ!" : e.Telegraph ? "ЖАЛО — сбей удар!" : "Атакуй тело · следи за жалом"; t = e.bossWindow > 0 ? "STRIKE CANCELED!" : e.Telegraph ? "STINGER — interrupt!" : "Hit the body · watch the stinger"; break;
                case 8: s = e.elapsed % 6 < 4.5f ? "ВДОХ — атакуй!" : "ВЫДОХ — дождись раскрытия"; t = e.elapsed % 6 < 4.5f ? "INHALE — attack!" : "EXHALE — wait for it to open"; break;
                case 9: s = e.bossWindow > 0 ? "ПЕРЕГРЕВ — отпусти" : "Жар " + (e.bossProgress * 100).ToString("0") + "% · пауза охлаждает"; t = e.bossWindow > 0 ? "OVERHEATED — release" : "Heat " + (e.bossProgress * 100).ToString("0") + "% · release to cool"; break;
                case 10: s = e.bossWindow > 0 ? "КОЛОКОЛ ОТКРЫТ — атакуй!" : e.elapsed % 2 < .7f ? "БЕЙ В ДОЛЮ · " + e.bossStage + "/3" : "Жди световой доли · " + e.bossStage + "/3"; t = e.bossWindow > 0 ? "BELL OPEN — attack!" : e.elapsed % 2 < .7f ? "HIT THE BEAT · " + e.bossStage + "/3" : "Wait for the light pulse · " + e.bossStage + "/3"; break;
            }
            return en ? t : s;
        }
    }
}
