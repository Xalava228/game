using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;

namespace TimeThief.Editor
{
    [InitializeOnLoad]
    public static class FirstOpenSetup
    {
        static FirstOpenSetup()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists("Assets/TimeThief/Resources/GameConfig.asset"))
                    ProjectBuilder.Prepare();
            };
        }
    }

    public static class ProjectBuilder
    {
        const string ResourcesPath = "Assets/TimeThief/Resources/";
        [MenuItem("Time Thief/Prepare Project")]
        public static void Prepare()
        {
            foreach (string file in Directory.GetFiles(ResourcesPath + "Art", "*.png"))
            {
                var t = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                t.textureType = TextureImporterType.Sprite;
                t.spriteImportMode = SpriteImportMode.Single;
                t.alphaIsTransparency = true;
                t.mipmapEnabled = false;
                t.textureCompression = TextureImporterCompression.Uncompressed;
                t.maxTextureSize = 2048;
                t.filterMode = FilterMode.Bilinear;
                if (file.EndsWith("panel.png"))
                    t.spriteBorder = new Vector4(24, 24, 24, 24);
                t.SaveAndReimport();
            }

            string fp = ResourcesPath + "Fonts/Nunito SDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fp);
            if (!font)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(ResourcesPath + "Fonts/Nunito.ttf");
                font = TMP_FontAsset.CreateFontAsset(source, 64, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
                font.name = "Nunito SDF";
                string chars = "";
                for (int i = 32; i < 127; i++)
                    chars += (char)i;
                for (int i = 0x400; i <= 0x45f; i++)
                    chars += (char)i;
                chars += "×−–—…·→✦Ёё«»";
                font.TryAddCharacters(chars, out string missing);
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(font, fp);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (var tex in font.atlasTextures)
                    AssetDatabase.AddObjectToAsset(tex, font);
                EditorUtility.SetDirty(font);
                Debug.Log("Font atlas ready. Missing optional glyphs: " + missing);
            }

            var c = LoadOrCreate<GameConfig>("GameConfig.asset");
            string[] keys = {"moth", "clerk", "witch", "countess", "archivist", "hero", "slime", "owl"};
            string[] ru = {"Минутный мотылёк", "Талонник", "Маятница", "Графиня минут", "Архивариус", "Ноль", "Тик-Так", "Совушка-полуночник"};
            string[] en = {"Minute Moth", "The Ticket Clerk", "Pendula", "Countess of Minutes", "The Archivist", "Zero", "Tick-Tock", "Midnight Owl"};
            c.enemies = new EnemyData[keys.Length];
            Directory.CreateDirectory(ResourcesPath + "Enemies");
            for (int i = 0; i < keys.Length; i++)
            {
                var e = LoadOrCreate<EnemyData>("Enemies/" + keys[i] + ".asset");
                e.nameRu = ru[i];
                e.nameEn = en[i];
                e.artKey = keys[i];
                e.enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(ResourcesPath + "Art/" + keys[i] + ".png");
                e.attackType = i % 2 == 0 ? AttackType.Magic : AttackType.Physical;
                EditorUtility.SetDirty(e);
                c.enemies[i] = e;
            }

            string[] br = {"Король будильников", "Леди Маятница", "Лунный мотылёк", "Графиня вечности", "Великий Архивариус", "Хранитель полуночи", "Хозяйка колючих минут", "Сборщик секунд", "Солнечная графиня", "Хранитель последнего часа"};
            string[] be = {"Alarm King", "Lady Pendula", "Moon Moth", "Countess of Forever", "Grand Archivist", "Midnight Keeper", "Lady of Thorny Minutes", "The Second Collector", "Sun Countess", "The Last Hour"};
            string[] hintsRu = {"После каждого удара атакует чуть быстрее.", "Иногда поднимает щит. Дождись, пока он погаснет.", "Меняет защиту. Чередуй клики и удерживание.", "Понемногу восстанавливает время. Не зевай!", "Каждый третий удар сильнее. Следи за вспышкой.", "Каждый третий удар меняет тип атаки.", "Отражает каждый четвёртый клик. Используй магию.", "Возвращает себе больше украденного времени.", "Долгая магия ослабевает. Делай перерывы.", "На исходе времени ускоряется. Заверши бой!"};
            string[] hintsEn = {"Attacks a little faster after each strike.", "Sometimes raises a shield. Wait for it to fade.", "Switches defenses. Alternate tapping and holding.", "Slowly restores time. Keep stealing!", "Every third strike hits harder. Watch the flash.", "Every third strike changes its attack type.", "Reflects every fourth tap. Use magic.", "Restores more of the time it steals.", "Long magic holds weaken. Take short breaks.", "Speeds up when low on time. Finish the fight!"};
            string[] bk = {"clerk", "witch", "moth", "countess", "archivist", "owl", "witch", "clerk", "countess", "archivist"};
            c.bosses = new BossData[10];
            Directory.CreateDirectory(ResourcesPath + "Bosses");
            for (int i = 0; i < 10; i++)
            {
                var b = LoadOrCreate<BossData>("Bosses/Boss" + (i + 1).ToString("D2") + ".asset");
                b.bossName = br[i];
                b.nameEn = be[i];
                b.hintRu = hintsRu[i];
                b.hintEn = hintsEn[i];
                b.artKey = "boss-" + (i + 1).ToString("D2");
                b.bossSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ResourcesPath + "Art/" + b.artKey + ".png");
                b.attackType = i % 2 == 0 ? AttackType.Physical : AttackType.Magic;
                b.specialAbilityType = (Ability)(i + 1);
                b.baseMaxTime = 8 + i;
                b.attackPower = .3f + i * .06f;
                b.attackCooldown = 3.8f;
                b.physicalDefense = i == 1 ? .3f : 0;
                b.magicDefense = i == 0 ? .3f : 0;
                EditorUtility.SetDirty(b);
                c.bosses[i] = b;
            }

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Assets/TimeThief/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().orthographic = true;
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = UIManager.Hex("f7f1e5");
            new GameObject("TimeThief").AddComponent<GameManager>().config = c;
            EditorSceneManager.SaveScene(scene, "Assets/TimeThief/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[]{new EditorBuildSettingsScene("Assets/TimeThief/Scenes/Main.unity", true)};
            Settings();
            AssetDatabase.SaveAssets();
            Debug.Log("TIME_THIEF_PREPARED");
        }

        static T LoadOrCreate<T>(string path)
            where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(ResourcesPath + path);
            if (asset)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, ResourcesPath + path);
            return asset;
        }

        static void Settings()
        {
            PlayerSettings.companyName = "Xalava";
            PlayerSettings.productName = "TimeThief";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.WebGL.template = "PROJECT:TimeThief";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.initialMemorySize = 64;
            PlayerSettings.WebGL.maximumMemorySize = 256;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
        }

        [MenuItem("Time Thief/Build WebGL Release")]
        public static void Build()
        {
            if (!File.Exists(ResourcesPath + "GameConfig.asset") || !File.Exists("Assets/TimeThief/Scenes/Main.unity"))
                Prepare();
            else
                Settings();
            SelfTests.Run();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes = new[]{"Assets/TimeThief/Scenes/Main.unity"}, locationPathName = Path.GetFullPath("../Builds/WebGL"), target = BuildTarget.WebGL, options = BuildOptions.None});
            Debug.Log("TIME_THIEF_BUILD " + report.summary.result + " bytes=" + report.summary.totalSize);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("WebGL build failed");
        }
    }

    public static class SelfTests
    {
        static int checks;
        static void Check(bool condition, string label)
        {
            checks++;
            if (!condition)
                throw new Exception("FAILED: " + label);
        }

        [MenuItem("Time Thief/Run Core Checks")]
        public static void Run()
        {
            checks = 0;
            var c = Resources.Load<GameConfig>("GameConfig");
            Check(c && c.bosses.Length == 10, "10 boss assets");
            var p = new PlayerStats{CurrentTime = 4.9f, MaxTime = 5};
            Check(Mathf.Abs(p.AddTime(.5f) - .1f) < .0001f, "capacity caps transferred time");
            Check(p.LoseTime(999) == 5 && p.CurrentTime == 0, "overkill cannot steal phantom time");
            Check(PlayerStats.Reduced(2, -5, 10) == 2, "negative armor cannot amplify damage");
            Check(PlayerStats.Reduced(2, 100000, 10) > 0, "diminishing returns remains positive");
            Check(EnemyGenerator.TypeAt(10) == EncounterType.MiniBoss, "level 10 miniboss");
            Check(EnemyGenerator.TypeAt(50) == EncounterType.Boss, "boss priority at 50");
            Check(EnemyGenerator.TypeAt(275) == EncounterType.Boss, "endless boss after 250");
            for (int seed = 1; seed <= 4; seed++)
                for (int level = 1; level <= 1100; level++)
                {
                    var e = EnemyGenerator.Generate(c, level, seed);
                    Check(e.maxTime > 0 && e.power > 0 && e.cooldown >= c.minimumCooldown, "valid difficulty");
                    Check(!(e.physicalDefense > .35f && e.magicDefense > .35f), "both defenses cannot be high");
                    Check(e.modifiers.Distinct().Count() == e.modifiers.Length, "no duplicate modifiers");
                    Check(e.sprite != null, "every encounter has artwork");
                }

            var late = EnemyGenerator.Generate(c, 1000000, 3);
            Check(float.IsFinite(late.maxTime) && late.maxTime < 1e8f, "bounded linear long-run scaling");
            var shop = new ShopManager{shards = 100};
            p = new PlayerStats();
            Check(shop.Buy(0, p) && p.MaxTime == 6, "max-time purchase");
            Check(shop.Buy(1, p) && Mathf.Abs(p.Attack - 1.15f) < .001f, "attack purchase");
            Check(!shop.Buy(99, p), "invalid item rejected");
            Check(shop.Buy(2, p) && shop.Has(BuffType.Armor), "buff purchase");
            Check(!shop.Buy(2, p), "duplicate buff blocked");
            shop.EndBattle();
            shop.EndBattle();
            Check(shop.Has(BuffType.Armor), "3-battle buff survives twice");
            shop.EndBattle();
            Check(!shop.Has(BuffType.Armor), "buff expires on third battle");
            var rewards = RewardManager.Choices(1, 25, true);
            Check(rewards.Length == 3 && rewards.Select(r => r.stat).Distinct().Count() == 3, "three distinct boss choices");
            p.CritChance = .74f;
            p.Upgrade(Stat.CritChance, .3f);
            Check(p.CritChance == .75f, "crit cap");
            Check(!new PlayerStats{Attack = float.NaN}.Valid(), "reject corrupted save stats");
            Directory.CreateDirectory("../QA");
            File.WriteAllText("../QA/core-checks.txt", checks + " checks passed; 4400 generated encounters, levels 10/25/50/250/275, level 1,000,000, transfer caps, shop, buff expiry, rewards and corrupted stats.\n");
            Debug.Log("TIME_THIEF_TESTS_PASSED " + checks);
        }
    }
}
