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
        TMP_FontAsset font, titleFont;
        Image enemyImage, enemyBar, playerBar;
        TMP_Text enemySeconds, playerSeconds, playerCapacity, warning, buffText, combatMessage, affinity;
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
        bool portrait, detailsOpen;
        GameState lastLayoutState;
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
            titleFont = Resources.Load<TMP_FontAsset>("Fonts/Alegreya SDF");
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
            i.color = key.StartsWith("icon-") ? TextColor(color ?? cream) : color ?? Color.white;
            return i;
        }

        // Map the existing semantic color roles into the illustrated midnight palette.
        Color TextColor(Color color)
        {
            if (color == ink || color == cream) return Hex("f4ead3");
            if (color == muted) return Hex("b9bfcb");
            if (color == mint) return Hex("95e0c5");
            if (color == purple || color == gold) return Hex("ecd096");
            if (color == red) return Hex("ffb6a9");
            return color;
        }
        Color SurfaceColor(Color color)
        {
            if (color == cream) return Hex("202b40");
            if (color == ink) return Hex("141e30");
            if (color == mint) return Hex("20574f");
            if (color == purple) return Hex("39344f");
            if (color == red) return Hex("792e36");
            return color;
        }
        Image Panel(Transform p, float x, float y, float w, float h, Color color)
        {
            var i = Img(p, "panel", x, y, w, h, color, false);
            i.color = SurfaceColor(color);
            i.type = Image.Type.Sliced;
            if (h > .045f && w > .08f)
            {
                var frame = Img(i.transform, "frame-line", .5f, .5f, 1, 1, null, false);
                frame.type = Image.Type.Sliced;
            }
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
            t.color = TextColor(color ?? ink);
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.enableAutoSizing = true;
            t.fontSizeMin = size * unit * .88f;
            t.fontSizeMax = size * unit;
            if (!value.Contains("\n") && r.rect.height > 0)
            {
                // Keep single-line labels inside shallow landscape cards.
                t.fontSizeMax = Mathf.Min(t.fontSizeMax, r.rect.height * .70f);
                t.fontSizeMin = t.fontSizeMax * .88f;
            }
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

            if (lastLayoutState != g.state) detailsOpen = false;
            lastLayoutState = g.state;
            guarded.Clear();
            floaters.Clear();
            Canvas.ForceUpdateCanvases();
            portrait = Screen.width < Screen.height;
            // WebGL Screen dimensions include the render DPR (capped at 1.5 by the template).
            bool compactLandscape = !portrait && Screen.width > Screen.height * 2 && Screen.height < 850;
            unit = portrait ? root.rect.width / 430f : Mathf.Min(root.rect.width / 1100f, root.rect.height / (compactLandscape ? 500f : 760f));
            unit = Mathf.Max(.55f, unit);
            var backdrop = Img(root, "arena-clean", .5f, .5f, 1, 1, null, false);
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
                Button(root, g.music.Muted ? "×" : "", portrait ? .765f : .882f, .960f, portrait ? .14f : .043f, portrait ? .077f : .062f, () =>
                {
                    g.music.Toggle();
                    Refresh();
                }, ink, "sound");
                Img(root, "logo", portrait ? .08f : .047f, .960f, portrait ? .115f : .052f, .068f);
            }

            if (g.state == GameState.Fighting)
                Button(root, "", portrait ? .92f : .94f, .960f, portrait ? .14f : .043f, portrait ? .077f : .062f, () => g.TogglePause(), ink, "pause");
            pauseShade = Box(root, "Pause", .5f, .5f, 1, 1).gameObject;
            var shade = pauseShade.AddComponent<Image>();
            shade.color = new Color(.055f, .075f, .125f, .98f);
            shade.raycastTarget = true;
            Text(pauseShade.transform, g.T("Время на паузе", "Time is paused"), .5f, .59f, .8f, .1f, 38);
            Text(pauseShade.transform, g.T("Твои секунды в безопасности", "Your seconds are safe"), .5f, .49f, .8f, .08f, 18, muted);
            Button(pauseShade.transform, g.T("Продолжить", "Resume"), .5f, .36f, portrait ? .65f : .25f, .09f, () => g.TogglePause(), mint, "play");
            Button(pauseShade.transform, g.T("В меню", "Main menu"), .5f, .24f, portrait ? .65f : .25f, .07f, () => g.Menu(), ink);
            pauseShade.SetActive(g.Paused && g.state == GameState.Fighting && !detailsOpen);
            if (detailsOpen) Tactics();
        }

        void BuildBattle()
        {
            var e = g.enemy?.data;
            string name = e == null ? "" : g.English ? e.nameEn : e.nameRu;
            var heading = Panel(battle, .5f, .872f, portrait ? .97f : .63f, .095f, cream);
            var tag = Panel(battle, .45f, .953f, portrait ? .47f : .20f, .038f, ink);
            Text(tag.transform, g.T("УРОВЕНЬ ", "LEVEL ") + g.level, .5f, .5f, .94f, .95f, 16, cream);
            var nameText = Text(heading.transform, name, .5f, .73f, .94f, .47f, portrait ? 25 : 30);
            if (titleFont) nameText.font = titleFont;
            FitLine(nameText);
            enemySeconds = Text(heading.transform, "", .5f, .28f, .90f, .28f, 16, purple);
            var rail = Panel(battle, .5f, .832f, portrait ? .88f : .48f, .010f, Hex("36445a"));
            enemyBar = Img(rail.transform, "panel", .5f, .5f, 1, 1, Hex("c9ac72"), false);
            enemyBar.type = Image.Type.Filled;
            enemyBar.fillMethod = Image.FillMethod.Horizontal;
            enemyBar.fillAmount = e == null ? 1 : Mathf.Clamp01(g.enemy.time / e.maxTime);
            var counter = Panel(battle, .5f, .797f, portrait ? .94f : .53f, .040f, ink);
            affinity = Text(counter.transform, "", .5f, .5f, .96f, .95f, 15, cream);
            FitLine(affinity);

            // The same large stage is used before and during combat.
            enemyImage = Img(battle, e?.artKey ?? "moth", .5f, .512f, portrait ? .94f : .52f, .52f);
            if (e?.sprite) enemyImage.sprite = e.sprite;
            enemyImage.raycastTarget = true;
            input = enemyImage.gameObject.AddComponent<EnemyInput>();
            input.Init(g);
            enemyRect = enemyImage.rectTransform;
            combatBanner = Panel(battle, .5f, .305f, portrait ? .94f : .56f, .066f, red);
            combatMessage = Text(combatBanner.transform, "", .5f, .5f, .96f, .94f, 16, cream);
            combatBanner.gameObject.SetActive(false);

            string condition = e == null ? "" : EnemyGenerator.ConditionName(e.condition, g.English);
            Button(battle, condition + g.T(" · тактика", " · tactics"), .5f, .224f, portrait ? .91f : .48f, .042f, ToggleDetails, purple);
            var p = Panel(battle, .5f, .113f, portrait ? .95f : .56f, .170f, ink);
            playerCapacity = Text(p.transform, g.T("ТВОЁ ВРЕМЯ", "YOUR TIME"), .28f, .82f, .48f, .19f, 14, cream);
            playerSeconds = Text(p.transform, "", .28f, .49f, .48f, .36f, 30, cream);
            var pr = Panel(p.transform, .28f, .16f, .43f, .060f, Hex("494e62"));
            playerBar = Img(pr.transform, "panel", .5f, .5f, 1, 1, Hex("93dfc6"), false);
            playerBar.type = Image.Type.Filled;
            playerBar.fillMethod = Image.FillMethod.Horizontal;
            playerBar.fillAmount = g.player.CurrentTime / g.player.MaxTime;
            warning = Text(p.transform, "", .775f, .69f, .40f, .29f, 14, cream);
            buffText = Text(p.transform, "", .775f, .25f, .40f, .20f, 12, Hex("bdebdc"));
            if (g.state == GameState.Intro)
            {
                warning.gameObject.SetActive(false);
                buffText.gameObject.SetActive(false);
                Button(p.transform, g.T("В бой", "Fight"), .775f, .48f, .38f, .62f, () => g.BeginFight(), mint, "play");
            }
        }

        void FitLine(TMP_Text text)
        {
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Min(text.fontSizeMax, 12 * unit);
        }

        void ToggleDetails()
        {
            detailsOpen = !detailsOpen;
            if (g.state == GameState.Fighting) g.TogglePause();
            else Refresh();
        }

        void Tactics()
        {
            var shade = Img(root, "panel", .5f, .5f, 1, 1, new Color(.09f, .10f, .17f, .72f), false);
            shade.raycastTarget = true;
            var panel = Panel(root, .5f, .50f, portrait ? .95f : .60f, .75f, cream);
            var title = Text(panel.transform, g.English ? g.enemy.data.nameEn : g.enemy.data.nameRu, .5f, .92f, .91f, .08f, 27);
            FitLine(title);
            Text(panel.transform, g.T("Ближайший удар: ", "Next strike: ") + g.enemy.NextStrikeDamage.ToString("0.0") + g.T(" сек", " sec"), .5f, .866f, .9f, .045f, 14, muted);
            Text(panel.transform, AffinityHint(), .5f, .755f, .91f, .16f, 18, purple);
            Text(panel.transform, Hint(), .5f, .565f, .91f, .20f, 16);
            Text(panel.transform, EnemyGenerator.ConditionName(g.enemy.data.condition, g.English) + "\n" + EnemyGenerator.ConditionHint(g.enemy.data.condition, g.English), .5f, .40f, .91f, .15f, 16, mint);
            Text(panel.transform, g.T("Тебе возвращается до ", "You recover up to ") + (g.enemy.RecoveryFraction * 100).ToString("0") + g.T("% урона.\nВраг получает все потерянные тобой секунды.", "% of damage.\nThe enemy receives every second you lose."), .5f, .25f, .91f, .13f, 15);
            Button(panel.transform, g.T("Понятно", "Got it"), .5f, .10f, .79f, .095f, ToggleDetails, mint);
        }

        void Menu()
        {
            float x = portrait ? .5f : .265f;
            // The illustration, title and actions each have their own region.
            Img(page, "witch", portrait ? .5f : .725f, portrait ? .505f : .49f, portrait ? .94f : .49f, portrait ? .385f : .87f);
            if (!portrait) Img(page, "moth", .875f, .25f, .19f, .28f);
            Img(page, "logo", portrait ? .115f : .092f, .943f, portrait ? .15f : .073f, .09f);
            var first = Text(page, g.T("ВОР", "TIME"), x, portrait ? .852f : .738f, portrait ? .91f : .43f, .12f, 98, gold, portrait ? TextAlignmentOptions.Center : TextAlignmentOptions.Left);
            var second = Text(page, g.T("ВРЕМЕНИ", "THIEF"), x, portrait ? .759f : .60f, portrait ? .91f : .43f, .12f, 98, gold, portrait ? TextAlignmentOptions.Center : TextAlignmentOptions.Left);
            if (titleFont) first.font = second.font = titleFont;
            FitLine(first); FitLine(second);
            Text(page, g.T("Укради секунду. Измени вечность.", "Steal a second. Change forever."), x, portrait ? .289f : .474f, portrait ? .94f : .43f, .042f, 18, muted, portrait ? TextAlignmentOptions.Center : TextAlignmentOptions.Left);
            bool resume = g.save.activeRun;
            Button(page, resume ? g.T("Продолжить забег", "Continue run") : g.T("Начать приключение", "Start adventure"), x, portrait ? .223f : .348f, portrait ? .87f : .37f, .085f, () =>
            {
                if (resume) g.ResumeRun();
                else g.NewRun();
            }, mint, "play");
            if (resume)
                Button(page, g.T("Новый забег", "New run"), x, portrait ? .144f : .245f, portrait ? .87f : .37f, .055f, ConfirmNew, ink);
            MenuRecords(x);
            Button(page, g.music.Muted ? "×" : "", portrait ? .765f : .855f, .943f, portrait ? .14f : .043f, portrait ? .077f : .057f, () => { g.music.Toggle(); Refresh(); }, ink, "sound");
            Button(page, g.English ? "RU" : "EN", portrait ? .92f : .93f, .943f, portrait ? .14f : .055f, portrait ? .077f : .057f, () => { g.English = !g.English; Refresh(); }, ink);
        }

        void MenuRecords(float x)
        {
            // This bounded region can become the Yandex leaderboard entry without moving the main actions.
            var records = Panel(page, x, portrait ? .065f : .13f, portrait ? .87f : .37f, portrait ? .08f : .12f, cream);
            records.name = "MenuRecords";
            Text(records.transform, g.T("РЕКОРД ЗАБЕГА", "RUN RECORD"), .27f, .74f, .47f, .24f, 13, muted);
            Text(records.transform, g.save.bestLevel.ToString(), .27f, .34f, .45f, .40f, 25, gold);
            Text(records.transform, g.T("БОССОВ ПОБЕЖДЕНО", "BOSSES DEFEATED"), .76f, .74f, .45f, .24f, 13, muted);
            Text(records.transform, g.save.totalBossesDefeated.ToString(), .76f, .34f, .45f, .40f, 25, mint);
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


        string AffinityHint()
        {
            bool magic = g.enemy.UsesMagic;
            return magic ? g.T("МАГИЯ · защита от магии 90%\nАтакуй КЛИКАМИ", "MAGIC · 90% magic resistance\nAttack with TAPS")
                : g.T("ФИЗИКА · защита от клика 90%\nУДЕРЖИВАЙ для магии", "PHYSICAL · 90% tap resistance\nHOLD to cast magic");
        }

        string Hint()
        {
            var e = g.enemy.data;
            string[] ru = { "Следи за своим запасом секунд.", "Ускоряется после каждого удара.", "Поднимает щит на короткое время.", "Меняет тип каждые 4 секунды.", "Восстанавливает часть своего времени.", "Каждый третий удар мощнее на 50%.", "Каждый третий удар меняет тип.", "Отражает каждый четвёртый клик.", "Крадёт ударами на 20% больше.", "Магия ослабевает после 2 сек удержания.", "Ускоряется, когда осталось мало времени." };
            string[] en = { "Watch your remaining seconds.", "Speeds up after each strike.", "Raises a shield for a short time.", "Switches element every 4 seconds.", "Regenerates some of its time.", "Every third strike is 50% stronger.", "Every third strike switches element.", "Reflects every fourth tap.", "Steals 20% more with each strike.", "Magic weakens after a 2-second hold.", "Speeds up when low on time." };
            string result = (g.English ? en : ru)[(int)e.ability];
            if (e.type == EncounterType.Boss) result += g.T("\nНа половине времени: смена типа, сильнее удары, меньше возврат.", "\nAt half time: element switch, stronger strikes, less recovery.");
            string[] modsRu = { "броня", "маг. щит", "хрупкость", "слабость к магии", "быстрый", "тяжёлый удар", "поглощение", "шипы", "нестабильность", "пульсирующий щит", "ярость", "перегрев" };
            string[] modsEn = { "armored", "magic shield", "fragile", "magic weakness", "fast", "heavy strike", "absorption", "thorns", "unstable", "pulse shield", "fury", "heat" };
            if (e.modifiers.Length > 0)
            {
                var names = new List<string>();
                foreach (var modifier in e.modifiers) names.Add((g.English ? modsEn : modsRu)[(int)modifier]);
                result += "\n" + string.Join(" · ", names);
            }
            return result;
        }

        void Intro() { /* The shared combat HUD contains the start button. */ }

        void Title(string eyebrow, string title)
        {
            Panel(page, .5f, .825f, .965f, .185f, cream);
            Text(page, eyebrow, .5f, .88f, .94f, .055f, 16, purple);
            Text(page, title, .5f, .78f, .92f, .10f, portrait ? 31 : 38);
        }

        void Results()
        {
            Title(g.T("ЕЩЁ ОДНА СЕКУНДА В ТВОЕЙ ИСТОРИИ", "ANOTHER SECOND IN YOUR STORY"), g.T("Уровень ", "Level ") + g.level + g.T(" пройден!", " complete!"));
            var next = EnemyGenerator.Generate(g.config, g.level + 1, g.seed);
            var preview = new EnemyController(g, next);
            var nextPanel = Panel(page, .5f, .707f, portrait ? .92f : .66f, .038f, cream);
            var nextText = Text(nextPanel.transform, g.T("Далее: ", "Next: ") + (next.attackType == AttackType.Magic ? g.T("магия", "magic") : g.T("физика", "physical")) + g.T(" · удар −", " · strike −") + preview.NextStrikeDamage.ToString("0.0") + g.T(" сек", " sec") + (next.type == EncounterType.Boss ? g.T(" · БОСС", " · BOSS") : ""), .5f, .5f, .96f, .95f, 14, purple);
            FitLine(nextText);
            float x = portrait ? .5f : .68f;
            if (!portrait)
            {
                Img(page, "hero", .27f, .40f, .32f, .55f);
            }
            var card = Panel(page, x, portrait ? .515f : .475f, portrait ? .91f : .43f, portrait ? .33f : .41f, cream);
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

        string[] StatNames => g.English ? new[]{"Max time", "Attack", "Crit chance", "Crit multiplier", "Armor", "Magic resistance"} : new[]{"Макс. время", "Атака", "Шанс крита", portrait ? "Множ. крита" : "Множитель крита", "Броня", "Сопр. магии"};
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
            Title(g.T("ОСКОЛКИ  ", "SHARDS  ") + g.shop.shards, portrait ? g.T("Лавка минут", "Minute shop") : g.T("Лавка потерянных минут", "The minute shop"));
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
            if (!g.reviveUsed)
            {
                Button(page, g.T("Вернуться к жизни · реклама", "Revive · watch an ad"), x, .295f, portrait ? .87f : .45f, .07f, () => g.Revive(), mint, "ticket", g.platform.CanAd);
                Text(page, g.platform.CanAd ? g.T("Полный запас времени · один раз за забег", "Full time reserve · once per run") : g.T("Реклама сейчас недоступна", "Ads are currently unavailable"), x, .244f, portrait ? .91f : .48f, .026f, 13, muted);
            }
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
                pauseShade.SetActive(g.Paused && g.state == GameState.Fighting && !detailsOpen);
            if (g.enemy != null && battle.gameObject.activeSelf)
            {
                float dt = Time.unscaledDeltaTime;
                affinity.text = g.enemy.UsesMagic ? g.T("МАГИЯ · бей короткими кликами", "MAGIC · use short taps") : g.T("ФИЗИКА · удерживай для магии", "PHYSICAL · hold to cast magic");
                enemySeconds.text = g.enemy.time.ToString("0.0") + g.T(" сек  /  +", " sec  /  +") + g.enemy.FlowRate.ToString("0.##") + g.T(" сек/с", " sec/s");
                playerSeconds.text = g.player.CurrentTime.ToString("0.0") + g.T(" сек", " sec");
                playerCapacity.text = g.T("ЗАПАС / ", "CAPACITY / ") + g.player.MaxTime.ToString("0.0");
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
                warning.text = (nextHeavy ? g.T("Мощный −", "Heavy −") : g.T("Удар −", "Strike −")) + g.enemy.NextStrikeDamage.ToString("0.0") + "\n" + g.T("через ", "in ") + Mathf.Max(0, g.enemy.timer).ToString("0.0") + g.T(" с", " s");
                warning.color = g.enemy.Telegraph ? Hex("ffd084") : TextColor(cream);
                if (combatBanner)
                {
                    combatBanner.gameObject.SetActive(g.state == GameState.Fighting && Time.unscaledTime < combatUntil);
                    combatBanner.color = SurfaceColor(lastCombatColor);
                    combatMessage.text = lastCombatMessage;
                }
                buffText.text = g.T("Возврат ", "Recover ") + (g.enemy.RecoveryFraction * 100).ToString("0") + "%" + (g.shop.buffs.Count > 0 ? " · " + g.shop.buffs.Count + g.T(" усил.", " buffs") : "");
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

        public void Hit(float stolen, float recovered, bool critical)
        {
            hit = critical ? 1.6f : 1;
            Float((critical ? g.T("ТВОЙ КРИТ! −", "YOUR CRIT! −") : "−") + stolen.ToString("0.0") + g.T(" сек", " sec"), critical ? purple : mint, new Vector2(.5f + UnityEngine.Random.Range(-.08f, .08f), .55f), new Vector2(0, 85 * unit));
            if (critical) CombatNotice(g.T("ТВОЙ КРИТ: урон ", "YOUR CRIT: damage ") + stolen.ToString("0.00") + g.T(" · тебе +", " · you +") + recovered.ToString("0.00") + g.T(" сек", " sec"), purple);
            else if (!g.enemy.UsesMagic) CombatNotice(g.T("Клик поглощён на 90% · удерживай для магии", "Tap resisted by 90% · hold to cast magic"), purple);
        }

        void CombatNotice(string message, Color color)
        {
            lastCombatMessage = message;
            lastCombatColor = color;
            combatUntil = Time.unscaledTime + 2.3f;
        }

        public void BossPhase()
        {
            CombatNotice(g.T("ВТОРАЯ ФАЗА! Тип атаки изменился", "SECOND PHASE! Attack element changed"), purple);
            g.music.Sfx(g.config.bossIntroSound);
        }

        public void EnemyHit(float taken, bool magic, bool heavy = false, bool reflected = false)
        {
            attackFlash = 1;
            string type = reflected ? g.T("ОТРАЖЕНИЕ КЛИКА", "REFLECTED TAP") : heavy ? (g.enemy.data.type == EncounterType.Boss ? g.T("МОЩНЫЙ УДАР БОССА", "HEAVY BOSS STRIKE") : g.T("МОЩНЫЙ УДАР", "HEAVY STRIKE")) : magic ? g.T("МАГИЯ ХРАНИТЕЛЯ", "KEEPER MAGIC") : g.T("УДАР ХРАНИТЕЛЯ", "KEEPER STRIKE");
            CombatNotice(type + "\n−" + taken.ToString("0.00") + g.T(" сек тебе / +", " sec you / +") + taken.ToString("0.00") + g.T(" сек врагу", " sec enemy"), red);
        }
    }
}
