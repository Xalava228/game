using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeThief
{
    public sealed class UIManager : MonoBehaviour
    {
        GameManager g;
        Canvas canvas;
        RectTransform root, battle, page, fx, enemyRect;
        TMP_FontAsset font;
        Image enemyImage, enemyBar, playerBar;
        TMP_Text enemySeconds, playerSeconds, warning, buffText, combatMessage;
        Image combatBanner;
        string lastCombatMessage;
        Color lastCombatColor;
        float combatUntil;
        GameObject pauseShade;
        public EnemyInput input;
        readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
        readonly List<Button> guarded = new List<Button>();
        readonly List<Floater> floaters = new List<Floater>();
        Color ink = Hex("24283f"), muted = Hex("515c60"), cream = Hex("fffaf0"), mint = Hex("17695f"), purple = Hex("594477"), gold = Hex("a15d12"), red = Hex("a33337");
        float unit = 1, hit, attackFlash, magicParticle;
        int sw, sh;
        bool portrait;
        class Floater
        {
            public RectTransform rt;
            public Graphic visual;
            public Vector2 start, velocity;
            public float age;
        }

        public static Color Hex(string s)
        {
            ColorUtility.TryParseHtmlString("#" + s, out var c);
            return c;
        }

        public void Init(GameManager game)
        {
            g = game;
            font = Resources.Load<TMP_FontAsset>("Fonts/Nunito SDF");
            if (!font)
            {
                Debug.LogError("Nunito SDF font is missing. Run Time Thief / Prepare Project.");
                return;
            }

            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            root = (RectTransform)go.transform;
            if (!FindFirstObjectByType<EventSystem>())
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            Canvas.ForceUpdateCanvases();
            sw = Screen.width;
            sh = Screen.height;
            Refresh();
        }

        Sprite Art(string key)
        {
            if (art.TryGetValue(key, out var s))
                return s;
            s = Resources.Load<Sprite>("Art/" + key);
            if (!s)
            {
                var t = Resources.Load<Texture2D>("Art/" + key);
                if (t)
                    s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), 100);
            }

            art[key] = s;
            return s;
        }

        RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = new Vector2(x - w / 2, y - h / 2);
            r.anchorMax = new Vector2(x + w / 2, y + h / 2);
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        Image Img(Transform parent, string key, float x, float y, float w, float h, Color? color = null, bool preserve = true)
        {
            var r = Box(parent, key, x, y, w, h);
            var i = r.gameObject.AddComponent<Image>();
            i.sprite = Art(key);
            i.preserveAspect = preserve;
            i.raycastTarget = false;
            i.color = color ?? Color.white;
            return i;
        }

        Image Panel(Transform p, float x, float y, float w, float h, Color color)
        {
            var i = Img(p, "panel", x, y, w, h, color, false);
            i.type = Image.Type.Sliced;
            return i;
        }

        TMP_Text Text(Transform p, string value, float x, float y, float w, float h, float size, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var r = Box(p, "Text", x, y, w, h);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontStyle = FontStyles.Normal;
            t.text = value;
            size = Mathf.Max(size, portrait ? 14.5f : 14f);
            t.fontSize = size * unit;
            t.color = color ?? ink;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.enableAutoSizing = true;
            t.fontSizeMin = size * unit * .88f;
            t.fontSizeMax = size * unit;
            return t;
        }

        Button Button(Transform p, string label, float x, float y, float w, float h, Action action, Color? color = null, string icon = null, bool enabled = true)
        {
            var bg = Panel(p, x, y, w, h, color ?? ink);
            bg.raycastTarget = true;
            var b = bg.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            b.interactable = enabled;
            var colors = b.colors;
            colors.highlightedColor = Hex("e9e1cc");
            colors.pressedColor = Hex("b2bfb4");
            colors.disabledColor = new Color(.65f, .65f, .65f, .65f);
            b.colors = colors;
            b.onClick.AddListener(() =>
            {
                if (g.ActionsReady)
                    action();
            });
            bool iconOnly = icon != null && (string.IsNullOrEmpty(label) || label == "×");
            if (iconOnly)
            {
                Img(bg.transform, "icon-" + icon, .5f, .5f, .6f, .62f, cream);
                if (label == "×") Text(bg.transform, "×", .78f, .28f, .44f, .48f, 17, cream);
            }
            else
                Text(bg.transform, label, icon == null ? .5f : .55f, .5f, icon == null ? .92f : .78f, .85f, 17, cream);
            if (icon != null && !iconOnly)
                Img(bg.transform, "icon-" + icon, .11f, .5f, .15f, .6f, cream);
            if (enabled)
                guarded.Add(b);
            return b;
        }

        void Icon(Transform p, string key, float x, float y, float size, Color bg)
        {
            var b = Panel(p, x, y, size, portrait ? size * .65f : size * 1.4f, bg);
            Img(b.transform, "icon-" + key, .5f, .5f, .57f, .64f);
        }

        public void Refresh()
        {
            if (!canvas || !font)
                return;
            foreach (Transform child in root)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            guarded.Clear();
            floaters.Clear();
            Canvas.ForceUpdateCanvases();
            portrait = Screen.width < Screen.height;
            // WebGL Screen dimensions include the render DPR (capped at 1.5 by the template).
            bool compactLandscape = !portrait && Screen.width > Screen.height * 2 && Screen.height < 850;
            unit = portrait ? root.rect.width / 430f : Mathf.Min(root.rect.width / 1100f, root.rect.height / (compactLandscape ? 500f : 760f));
            unit = Mathf.Max(.55f, unit);
            var backdrop = Img(root, "arena", .5f, .5f, 1, 1, null, false);
            var fit = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = backdrop.sprite.rect.width / backdrop.sprite.rect.height;
            battle = Box(root, "Battle", .5f, .5f, 1, 1);
            page = Box(root, "Page", .5f, .5f, 1, 1);
            fx = Box(root, "Effects", .5f, .5f, 1, 1);
            bool inBattle = g.state == GameState.Fighting || g.state == GameState.Intro;
            BuildBattle();
            battle.gameObject.SetActive(inBattle);
            page.gameObject.SetActive(!inBattle || g.state == GameState.Intro);
            if (g.state == GameState.Waiting)
                Menu();
            else if (g.state == GameState.Intro)
                Intro();
            else if (g.state == GameState.Victory)
                Results();
            else if (g.state == GameState.Stats)
                Stats();
            else if (g.state == GameState.Shop)
                Shop();
            else if (g.state == GameState.RewardSelection)
                Rewards();
            else if (g.state == GameState.GameOver)
                GameOver();
            else if (g.state == GameState.Transition)
                Text(page, g.T("Открываем следующую минуту…", "Opening the next minute…"), .5f, .5f, .8f, .15f, 26);
            if (g.state != GameState.Waiting)
            {
                Button(root, g.music.Muted ? "×" : "", portrait ? .805f : .882f, .947f, portrait ? .09f : .043f, .062f, () =>
                {
                    g.music.Toggle();
                    Refresh();
                }, ink, "sound");
                Img(root, "logo", portrait ? .08f : .047f, .948f, portrait ? .09f : .042f, .058f);
                if (!portrait)
                    Text(root, g.T("ВОР ВРЕМЕНИ", "TIME THIEF"), .133f, .948f, .12f, .05f, 14, ink, TextAlignmentOptions.Left);
            }

            if (g.state == GameState.Fighting)
                Button(root, "", .94f, .947f, portrait ? .09f : .043f, .062f, () => g.TogglePause(), ink, "pause");
            pauseShade = Box(root, "Pause", .5f, .5f, 1, 1).gameObject;
            var shade = pauseShade.AddComponent<Image>();
            shade.color = new Color(.97f, .95f, .90f, .97f);
            shade.raycastTarget = true;
            Text(pauseShade.transform, g.T("Время на паузе", "Time is paused"), .5f, .59f, .8f, .1f, 38);
            Text(pauseShade.transform, g.T("Твои секунды в безопасности", "Your seconds are safe"), .5f, .49f, .8f, .08f, 18, muted);
            Button(pauseShade.transform, g.T("Продолжить", "Resume"), .5f, .36f, portrait ? .65f : .25f, .09f, () => g.TogglePause(), mint, "play");
            Button(pauseShade.transform, g.T("В меню", "Main menu"), .5f, .24f, portrait ? .65f : .25f, .07f, () => g.Menu(), ink);
            pauseShade.SetActive(g.Paused && g.state == GameState.Fighting);
        }

        void BuildBattle()
        {
            var e = g.enemy?.data;
            string name = e == null ? "" : g.English ? e.nameEn : e.nameRu;
            float width = portrait ? .86f : .42f;
            var levelTag = Panel(battle, .5f, .951f, portrait ? .48f : .18f, .049f, ink);
            Text(levelTag.transform, g.T("УРОВЕНЬ ", "LEVEL ") + g.level.ToString("D2"), .5f, .5f, .94f, .9f, 17, cream);
            Panel(battle, .5f, .833f, portrait ? .97f : .64f, .155f, cream);
            Text(battle, name, .5f, .865f, portrait ? .9f : .6f, .075f, 34);
            enemySeconds = Text(battle, "", .5f, .805f, .6f, .043f, 19, purple);
            var rail = Panel(battle, .5f, .765f, width, .013f, Hex("e0dacd"));
            enemyBar = Img(rail.transform, "panel", .5f, .5f, 1, 1, purple, false);
            enemyBar.type = Image.Type.Filled;
            enemyBar.fillMethod = Image.FillMethod.Horizontal;
            enemyBar.fillAmount = e == null ? 1 : g.enemy.time / e.maxTime;
            enemyImage = Img(battle, e?.artKey ?? "moth", .5f, .477f, portrait ? .92f : .46f, .59f);
            if (e?.sprite)
                enemyImage.sprite = e.sprite;
            enemyImage.raycastTarget = true;
            input = enemyImage.gameObject.AddComponent<EnemyInput>();
            input.Init(g);
            enemyRect = enemyImage.rectTransform;
            combatBanner = Panel(battle, .5f, .32f, portrait ? .93f : .53f, .072f, red);
            combatMessage = Text(combatBanner.transform, "", .5f, .5f, .96f, .94f, 17, cream);
            combatBanner.gameObject.SetActive(false);
            warning = Text(battle, "", .5f, .252f, .94f, .05f, 17, red);
            Text(battle, g.T("Нажимай — кради. Удерживай — колдуй.", "Tap to steal. Hold to cast."), .5f, .207f, portrait ? .96f : .56f, .035f, 15, muted);
            var p = Panel(battle, .5f, .126f, portrait ? .89f : .49f, .135f, ink);
            Img(p.transform, "icon-time", .085f, .5f, .09f, .54f);
            Text(p.transform, g.T("ТВОЁ ВРЕМЯ", "YOUR TIME"), .48f, .79f, .62f, .23f, 14, cream, TextAlignmentOptions.Left);
            playerSeconds = Text(p.transform, "", .55f, .51f, .78f, .42f, 30, cream, TextAlignmentOptions.Left);
            var pr = Panel(p.transform, .57f, .18f, .76f, .095f, Hex("494e62"));
            playerBar = Img(pr.transform, "panel", .5f, .5f, 1, 1, Hex("93dfc6"), false);
            playerBar.type = Image.Type.Filled;
            playerBar.fillMethod = Image.FillMethod.Horizontal;
            playerBar.fillAmount = g.player.CurrentTime / g.player.MaxTime;
            buffText = Text(battle, "", .5f, .033f, .95f, .035f, 12, muted);
        }

        void Menu()
        {
            Img(page, "logo", portrait ? .5f : .12f, portrait ? .87f : .86f, portrait ? .15f : .067f, .13f);
            if (!portrait)
                Text(page, "01 / TIME THIEF", .25f, .86f, .19f, .05f, 12, muted, TextAlignmentOptions.Left);
            float x = portrait ? .5f : .28f;
            Text(page, g.T("ВОР\nВРЕМЕНИ", "TIME\nTHIEF"), x, portrait ? .70f : .635f, portrait ? .88f : .43f, portrait ? .19f : .29f, 70, ink, portrait ? TextAlignmentOptions.Center : TextAlignmentOptions.Left);
            Text(page, g.T("Укради секунду. Измени вечность.", "Steal a second. Change forever."), x, portrait ? .565f : .435f, portrait ? .9f : .43f, .075f, 20, muted, portrait ? TextAlignmentOptions.Center : TextAlignmentOptions.Left);
            if (!portrait)
            {
                Img(page, "witch", .71f, .51f, .30f, .79f);
                Img(page, "moth", .87f, .31f, .17f, .32f);
            }

            bool resume = g.save.activeRun;
            Button(page, resume ? g.T("Продолжить забег", "Continue run") : g.T("Начать приключение", "Start adventure"), x, portrait ? .41f : .29f, portrait ? .76f : .34f, .093f, () =>
            {
                if (resume)
                    g.ResumeRun();
                else
                    g.NewRun();
            }, mint, "play");
            if (resume)
                Button(page, g.T("Новый забег", "New run"), x, portrait ? .29f : .17f, portrait ? .76f : .34f, .075f, ConfirmNew, ink);
            Text(page, g.T("РЕКОРД  ", "BEST  ") + g.save.bestLevel + g.T("   /   ПОБЕД  ", "   /   WINS  ") + g.save.totalEnemiesDefeated, x, portrait ? .20f : .075f, portrait ? .9f : .45f, .047f, 13, muted);
            Button(page, g.music.Muted ? g.T("Звук: выкл.", "Sound: off") : g.T("Звук: вкл.", "Sound: on"), portrait ? .29f : .80f, portrait ? .085f : .947f, portrait ? .37f : .14f, .055f, () =>
            {
                g.music.Toggle();
                Refresh();
            }, ink, "sound");
            Button(page, g.English ? "RU" : "EN", portrait ? .72f : .93f, portrait ? .085f : .947f, portrait ? .17f : .055f, .055f, () =>
            {
                g.English = !g.English;
                Refresh();
            }, purple);
        }

        void ConfirmNew()
        {
            foreach (Transform c in page)
            {
                c.gameObject.SetActive(false);
                Destroy(c.gameObject);
            }

            Text(page, g.T("Начать сначала?", "Start again?"), .5f, .65f, .9f, .12f, 36);
            Text(page, g.T("Текущий забег будет заменён. Рекорд останется.", "This replaces your current run. Your record stays."), .5f, .52f, .8f, .1f, 18, muted);
            Button(page, g.T("Новый забег", "New run"), portrait ? .5f : .65f, portrait ? .35f : .33f, portrait ? .75f : .25f, .09f, () => g.NewRun(), mint);
            Button(page, g.T("Назад", "Back"), portrait ? .5f : .35f, portrait ? .23f : .33f, portrait ? .75f : .25f, .09f, Refresh, ink);
        }

        string Hint()
        {
            if (g.enemy == null)
                return "";
            var e = g.enemy.data;
            if (e.type == EncounterType.Boss && !string.IsNullOrEmpty(e.hintRu))
                return g.English ? e.hintEn : e.hintRu;
            if (e.physicalDefense > .25f)
                return g.T("Крепкая броня. Удерживай, чтобы колдовать!", "Strong armor. Hold to use magic!");
            if (e.magicDefense > .25f)
                return g.T("Магический щит. Быстро нажимай!", "Magic shield. Use quick taps!");
            if (e.Has(Modifier.Thorny) || e.ability == Ability.Thorns)
                return g.T("Каждый четвёртый клик отражается. Попробуй магию.", "Every fourth tap reflects damage. Try magic.");
            if (g.level == 1)
                return g.T("Нажимай на хранителя и забирай его секунды.", "Tap the keeper to take its seconds.");
            if (g.level == 2)
                return g.T("Теперь попробуй удерживать палец на хранителе.", "Now try holding your finger on the keeper.");
            return g.T("Следи за вспышкой: хранитель готовит ответный удар.", "Watch the flash: the keeper is preparing a strike.");
        }

        void Intro()
        {
            var p = Panel(page, .5f, .275f, portrait ? .94f : .59f, .245f, cream);
            Text(p.transform, g.enemy.data.type == EncounterType.Boss ? g.T("ВЕЛИКИЙ ХРАНИТЕЛЬ", "GRAND KEEPER") : g.enemy.data.type == EncounterType.MiniBoss ? g.T("МИНИ-БОСС", "MINI-BOSS") : g.T("ТВОЯ СЛЕДУЮЩАЯ МИНУТА", "YOUR NEXT MINUTE"), .5f, .83f, .93f, .18f, 15, purple);
            Text(p.transform, Hint(), .5f, .57f, .90f, .28f, 17);
            Button(p.transform, g.T("В бой", "Let's go"), .74f, .21f, .44f, .29f, () => g.BeginFight(), mint, "play");
            Text(p.transform, g.T("Время остановлено", "Time is paused"), .26f, .21f, .42f, .24f, 12, muted);
        }

        void Title(string eyebrow, string title)
        {
            Panel(page, .5f, .825f, .965f, .185f, cream);
            Text(page, eyebrow, .5f, .88f, .94f, .055f, 16, purple);
            Text(page, title, .5f, .78f, .92f, .10f, 38);
        }

        void Results()
        {
            Title(g.T("ЕЩЁ ОДНА СЕКУНДА В ТВОЕЙ ИСТОРИИ", "ANOTHER SECOND IN YOUR STORY"), g.T("Уровень ", "Level ") + g.level + g.T(" пройден!", " complete!"));
            float x = portrait ? .5f : .68f;
            if (!portrait)
            {
                Img(page, "hero", .27f, .415f, .32f, .62f);
            }
            var card = Panel(page, x, portrait ? .515f : .475f, portrait ? .91f : .43f, portrait ? .33f : .43f, cream);
            Text(card.transform, "+" + g.lastReward + g.T(" осколков", " shards"), .5f, .86f, .94f, .18f, 34, mint);
            string bonus = g.miniReward == null ? g.T("ПОБЕДА ДЕЛАЕТ ТЕБЯ СИЛЬНЕЕ", "EVERY VICTORY MAKES YOU STRONGER") :
                g.T("ПОЛУЧЕН ДАР: ", "GIFT RECEIVED: ") + RewardText(g.miniReward);
            Text(card.transform, bonus, .5f, .67f, .94f, .17f, 17, g.miniReward == null ? ink : purple);
            Text(card.transform, GrowthText(), .5f, .405f, .92f, .35f, portrait ? 14.5f : 16, ink);
            Text(card.transform, g.player.CurrentTime.ToString("0.0") + " / " + g.player.MaxTime.ToString("0.0") + g.T(" сек в запасе", " seconds in reserve"), .5f, .12f, .94f, .13f, 16, muted);
            if (g.platform.CanAd && !g.rewardDoubled)
                Button(page, g.T("Реклама · ещё +", "Ad · extra +") + g.lastReward, x, portrait ? .315f : .23f, portrait ? .87f : .39f, .065f, () => g.Bonus(), purple, "ticket");
            Button(page, g.T("Статы", "Stats"), portrait ? .265f : .54f, portrait ? .205f : .115f, portrait ? .41f : .13f, .072f, () => g.OpenStats(), ink, "crit");
            Button(page, g.T("Магазин", "Shop"), portrait ? .735f : .695f, portrait ? .205f : .115f, portrait ? .41f : .14f, .072f, () => g.OpenShop(), ink, "shop");
            Button(page, g.T("Дальше  ", "Next  ") + (g.level + 1), portrait ? .735f : .875f, portrait ? .091f : .115f, portrait ? .41f : .17f, .082f, () => g.Continue(), mint, "arrow");
            Text(page, g.T("Время остановлено", "Time is paused"), portrait ? .265f : .25f, portrait ? .09f : .10f, portrait ? .42f : .35f, .05f, 13, muted);
        }

        string[] StatNames => g.English ? new[]{"Max time", "Attack", "Crit chance", "Crit multiplier", "Armor", "Magic resistance"} : new[]{"Макс. время", "Атака", "Шанс крита", "Множитель крита", "Броня", "Сопр. магии"};
        string[] StatIcons => new[]{"time", "attack", "crit", "multiplier", "armor", "resist"};
        string StatValue(Stat s)
        {
            float v = g.player.Get(s);
            return s == Stat.CritChance ? (v * 100).ToString("0.##") + "%" : s == Stat.CritMultiplier ? "×" + v.ToString("0.###") : s == Stat.MaxTime ? v.ToString("0.##") + g.T(" сек", " sec") : v.ToString("0.###");
        }

        void Stats()
        {
            Title(g.T("ТВОЙ ЗАБЕГ", "YOUR RUN"), g.T("Сила каждой секунды", "Every second counts"));
            for (int i = 0; i < 6; i++)
            {
                int col = i % 2, row = i / 2;
                float x = .28f + col * .44f, y = .625f - row * .163f;
                var p = Panel(page, x, y, .39f, .139f, cream);
                Img(p.transform, "icon-" + StatIcons[i], .14f, .5f, .16f, .46f, i % 2 == 0 ? mint : purple);
                Text(p.transform, StatNames[i], .60f, .72f, .69f, .32f, 16, muted, TextAlignmentOptions.Left);
                Text(p.transform, StatValue((Stat)i), .60f, .33f, .69f, .40f, 28, ink, TextAlignmentOptions.Left);
            }

            Text(page, g.player.CurrentTime.ToString("0.##") + " / " + g.player.MaxTime.ToString("0.##") + g.T(" секунд сейчас", " seconds now"), .5f, .18f, .8f, .06f, 18, muted);
            Button(page, g.T("Назад", "Back"), .75f, .084f, .35f, .077f, () => g.Back(), ink);
        }

        void Shop()
        {
            Title(g.T("ОСКОЛКИ  ", "SHARDS  ") + g.shop.shards, g.T("Лавка потерянных минут", "The minute shop"));
            string[] names = g.English ? new[]{"+1 sec max time", "+0.15 attack", "Armor +30% & +3", "Magic resist +30% & +3", "Tap power +50%", "Magic power +50%", "Crit chance +15%", "Freeze 2 seconds", "Double shards"} : new[]{"+1 сек макс. времени", "+0,15 атаки", "Броня +30% и +3", "Маг. защита +30% и +3", "Сила клика +50%", "Сила магии +50%", "Шанс крита +15%", "Заморозка на 2 сек", "Двойные осколки"};
            string[] icons = {"time", "attack", "armor", "resist", "attack", "magic", "crit", "freeze", "shard"};
            int cols = portrait ? 2 : 3;
            for (int i = 0; i < 9; i++)
            {
                int item = i, col = i % cols, row = i / cols;
                float w = portrait ? .44f : .285f, h = portrait ? .10f : .15f, x = portrait ? .27f + col * .46f : .197f + col * .303f, y = portrait ? .665f - row * .114f : .625f - row * .185f;
                var p = Panel(page, x, y, w, h, cream);
                Img(p.transform, "icon-" + icons[i], .11f, .57f, .13f, .4f, i < 2 ? mint : purple);
                Text(p.transform, names[i], .59f, portrait ? .70f : .74f, .73f, portrait ? .50f : .35f, portrait ? 13 : 16, ink, TextAlignmentOptions.Left);
                string duration = i < 2 ? g.T("Весь забег", "Whole run") : i < 4 ? g.T("3 боя", "3 battles") : g.T("1 бой", "1 battle");
                Text(p.transform, duration, .39f, .32f, .37f, .3f, 11, muted, TextAlignmentOptions.Left);
                bool owned = i >= 2 && g.shop.Has((BuffType)(i - 2));
                Button(p.transform, owned ? g.T("Куплено", "Owned") : g.shop.Cost(i).ToString(), .79f, .27f, .34f, .35f, () => g.Buy(item), owned ? muted : ink, null, g.shop.CanBuy(i));
            }

            Button(page, g.T("Назад", "Back"), .75f, .075f, .35f, .073f, () => g.Back(), ink);
            Text(page, g.T("Усиления действуют в этом забеге", "Upgrades last for this run"), .31f, .075f, .48f, .06f, 12, muted);
        }

        string RewardText(Reward r, bool preview = false)
        {
            float amount = preview && r.stat == Stat.CritChance ? Mathf.Min(r.amount, Mathf.Max(0, .75f - g.player.CritChance)) : r.amount;
            string value = r.stat == Stat.CritChance ? "+" + (amount * 100).ToString("0.#") + g.T(" п.п.", " pp") : r.percent ? "+" + (amount * 100).ToString("0") + "%" : "+" + amount.ToString("0.##");
            return value + " " + StatNames[(int)r.stat];
        }

        string GrowthText()
        {
            var v = g.config.growth;
            float[] d = g.save.lastGrowth;
            if (d == null || d.Length != 6) d = new[] { v.MaxTime, v.Attack, v.CritChance, v.CritMultiplier, v.Armor, v.MagicResistance };
            return g.T("За этот бой: ", "This battle: ") +
                g.T("время +", "time +") + d[0].ToString("0.##") + g.T(" сек · атака +", " sec · attack +") + d[1].ToString("0.###") + "\n" +
                g.T("Шанс крита +", "Crit chance +") + (d[2] * 100).ToString("0.##") + g.T(" п.п. · множитель +", " pp · multiplier +") + d[3].ToString("0.###") + "\n" +
                g.T("Броня +", "Armor +") + d[4].ToString("0.##") + g.T(" · маг. защита +", " · magic resist +") + d[5].ToString("0.##");
        }

        void Rewards()
        {
            Title(g.T("ВЕЛИКИЙ ХРАНИТЕЛЬ ПОБЕЖДЁН", "GRAND KEEPER DEFEATED"), g.T("Выбери свой дар", "Choose your gift"));
            Text(page, g.T("Одна карта. Сила до конца забега.", "One card. Power for the whole run."), .5f, .685f, .9f, .07f, 18, muted);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var r = g.rewards[i];
                bool available = r.stat != Stat.CritChance || g.player.CritChance < .75f;
                string choose = available ? g.T("Выбрать", "Choose") : g.T("Предел 75%", "75% limit");
                float x = portrait ? .5f : .19f + i * .31f, y = portrait ? .545f - i * .174f : .43f;
                var p = Panel(page, x, y, portrait ? .86f : .28f, portrait ? .15f : .39f, cream);
                if (portrait)
                {
                    Img(p.transform, "icon-" + StatIcons[(int)r.stat], .12f, .5f, .13f, .45f, purple);
                    Text(p.transform, RewardText(r, true), .54f, .71f, .7f, .34f, 20);
                    Button(p.transform, choose, .73f, .26f, .45f, .36f, () => g.ChooseReward(index), mint, null, available);
                }
                else
                {
                    Img(p.transform, "icon-" + StatIcons[(int)r.stat], .5f, .78f, .22f, .25f, purple);
                    Text(p.transform, RewardText(r, true), .5f, .48f, .93f, .23f, 24);
                    Button(p.transform, choose, .5f, .17f, .85f, .18f, () => g.ChooseReward(index), mint, null, available);
                }
            }
        }

        void GameOver()
        {
            Title(g.T("КАЖДЫЙ ЗАБЕГ — НОВАЯ ИСТОРИЯ", "EVERY RUN IS A NEW STORY"), g.T("Время закончилось", "Time ran out"));
            float x = portrait ? .5f : .66f;
            if (!portrait)
                Img(page, "hero", .25f, .43f, .35f, .65f);
            Text(page, g.level.ToString("D2"), x, .58f, .6f, .14f, 76, purple);
            Text(page, g.T("ДОСТИГНУТЫЙ УРОВЕНЬ", "LEVEL REACHED"), x, .475f, .7f, .045f, 14, muted);
            Text(page, g.T("Побеждено: ", "Defeated: ") + g.defeated + g.T("   ·   Рекорд: ", "   ·   Best: ") + g.save.bestLevel, x, .39f, portrait ? .94f : .47f, .08f, 18);
            if (g.platform.CanAd && !g.reviveUsed)
                Button(page, g.T("Реклама · вернуть время", "Ad · restore time"), x, .295f, portrait ? .87f : .45f, .07f, () => g.Revive(), purple, "ticket");
            Button(page, g.T("Сыграть снова", "Play again"), x, .185f, portrait ? .87f : .45f, .084f, () => g.NewRun(), mint, "play");
            Button(page, g.T("Главное меню", "Main menu"), x, .08f, portrait ? .87f : .45f, .064f, () => g.Menu(), ink);
        }

        void Update()
        {
            if (!canvas)
                return;
            if (Screen.width != sw || Screen.height != sh)
            {
                sw = Screen.width;
                sh = Screen.height;
                Refresh();
            }

            foreach (var b in guarded)
                if (b)
                    b.interactable = g.ActionsReady;
            if (pauseShade)
                pauseShade.SetActive(g.Paused && g.state == GameState.Fighting);
            if (g.enemy != null && battle.gameObject.activeSelf)
            {
                float dt = Time.unscaledDeltaTime;
                enemySeconds.text = g.enemy.time.ToString("0.0") + g.T(" сек у хранителя", " seconds to steal");
                playerSeconds.text = g.player.CurrentTime.ToString("0.0") + " / " + g.player.MaxTime.ToString("0.0") + g.T(" сек", " sec");
                enemyBar.fillAmount = Mathf.Lerp(enemyBar.fillAmount, g.enemy.time / g.enemy.data.maxTime, 1 - Mathf.Exp(-dt * 14));
                playerBar.fillAmount = Mathf.Lerp(playerBar.fillAmount, g.player.CurrentTime / g.player.MaxTime, 1 - Mathf.Exp(-dt * 14));
                playerBar.color = g.player.CurrentTime < Mathf.Min(2, g.player.MaxTime * .3f) ? red : Hex("93dfc6");
                bool holding = input && input.Holding;
                hit = Mathf.MoveTowards(hit, 0, dt * 5);
                attackFlash = Mathf.MoveTowards(attackFlash, 0, dt * 3);
                float bob = g.Paused ? 0 : Mathf.Sin(Time.unscaledTime * 2) * .008f;
                enemyRect.localScale = new Vector3(1 + hit * .14f + (holding ? Mathf.Sin(Time.unscaledTime * 41) * .008f : 0), 1 - hit * .12f + bob, 1);
                enemyImage.color = Color.Lerp(Color.white, red, attackFlash * .65f);
                bool nextHeavy = g.enemy.data.ability == Ability.HeavyStrike && (g.enemy.attackCount + 1) % 3 == 0;
                warning.text = g.IsFighting && g.enemy.Telegraph ? (nextHeavy ? g.T("Мощный удар через ", "Heavy strike in ") : g.T("Удар через ", "Strike in ")) + Mathf.Max(0, g.enemy.timer).ToString("0.0") + g.T(" сек!", " sec!") : g.IsFighting && g.enemy.Shielded ? g.T("Щит! Скоро исчезнет", "Shield! It will fade soon") : holding ? g.T("Магия крадёт секунды…", "Magic steals seconds…") : "";
                if (combatBanner)
                {
                    combatBanner.gameObject.SetActive(g.state == GameState.Fighting && Time.unscaledTime < combatUntil);
                    combatBanner.color = lastCombatColor;
                    combatMessage.text = lastCombatMessage;
                }
                buffText.text = "";
                foreach (var b in g.shop.buffs)
                    buffText.text += (g.English ? b.type.ToString() : new[]{"Броня", "Маг. барьер", "Супер-клик", "Магия", "Крит", "Заморозка", "Осколки ×2"}[(int)b.type]) + " · " + b.remainingBattles + "   ";
                if (holding)
                {
                    magicParticle += dt;
                    if (magicParticle > .10f)
                    {
                        magicParticle = 0;
                        Shard(new Vector2(.5f + UnityEngine.Random.Range(-.10f, .10f), .41f));
                    }
                }
            }

            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i];
                if (!f.rt)
                {
                    floaters.RemoveAt(i);
                    continue;
                }

                f.age += Time.unscaledDeltaTime;
                f.rt.anchoredPosition = f.start + f.velocity * f.age;
                var c = f.visual.color;
                c.a = 1 - f.age / 1.05f;
                f.visual.color = c;
                if (f.age > 1.05f)
                {
                    Destroy(f.rt.gameObject);
                    floaters.RemoveAt(i);
                }
            }
        }

        void Float(string value, Color color, Vector2 at, Vector2 velocity)
        {
            if (floaters.Count >= 24)
                return;
            var t = Text(fx, value, at.x, at.y, portrait ? .95f : .5f, .08f, 24, color);
            t.enableAutoSizing = false;
            var rt = t.rectTransform;
            floaters.Add(new Floater{rt = rt, visual = t, start = rt.anchoredPosition, velocity = velocity});
        }

        void Shard(Vector2 at)
        {
            if (floaters.Count >= 24) return;
            var image = Img(fx, "icon-shard", at.x, at.y, .018f, .026f, mint);
            var rt = image.rectTransform;
            floaters.Add(new Floater { rt = rt, visual = image, start = rt.anchoredPosition,
                velocity = new Vector2((.5f - at.x) * root.rect.width, -root.rect.height * .3f) });
        }

        public void Hit(float stolen, bool critical)
        {
            hit = critical ? 1.6f : 1;
            Float((critical ? g.T("ТВОЙ КРИТ! −", "YOUR CRIT! −") : "−") + stolen.ToString("0.0") + g.T(" сек", " sec"), critical ? purple : mint, new Vector2(.5f + UnityEngine.Random.Range(-.08f, .08f), .55f), new Vector2(0, 85 * unit));
            if (critical) CombatNotice(g.T("ТВОЙ КРИТ: украдено ", "YOUR CRIT: stole ") + stolen.ToString("0.0") + g.T(" сек", " sec"), purple);
        }

        void CombatNotice(string message, Color color)
        {
            lastCombatMessage = message;
            lastCombatColor = color;
            combatUntil = Time.unscaledTime + 2.3f;
        }

        public void EnemyHit(float taken, bool magic, bool heavy = false, bool reflected = false)
        {
            attackFlash = 1;
            string type = reflected ? g.T("ОТРАЖЕНИЕ КЛИКА", "REFLECTED TAP") : heavy ? g.T("МОЩНЫЙ УДАР БОССА", "HEAVY BOSS STRIKE") : magic ? g.T("МАГИЯ ХРАНИТЕЛЯ", "KEEPER MAGIC") : g.T("УДАР ХРАНИТЕЛЯ", "KEEPER STRIKE");
            CombatNotice(type + "\n−" + taken.ToString("0.00") + g.T(" сек твоего времени", " sec of your time"), red);
        }
    }
}
