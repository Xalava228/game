using UnityEngine;

namespace TimeThief
{
    public sealed class GameManager : MonoBehaviour
    {
        public GameConfig config;
        public PlayerStats player;
        public ShopManager shop;
        public EnemyController enemy;
        public UIManager ui;
        public MusicManager music;
        public PlatformBridge platform;
        public SaveData save;
        public GameState state = GameState.Waiting;
        public int level = 1, seed, defeated, lastReward;
        public bool reviveUsed, rewardDoubled;
        public float lastTimeReward, lastTimeAwarded;
        public Reward[] rewards;
        public Reward miniReward;
        bool manualPause, platformPause, adPause;
        float saveTimer, lastAd, guard;
        public bool English;
        public bool Paused => manualPause || platformPause || adPause;
        public bool IsFighting => state == GameState.Fighting && !Paused;
        public bool ActionsReady => !platformPause && !adPause && Time.unscaledTime >= guard;
        public string T(string ru, string en) => English ? en : ru;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindFirstObjectByType<GameManager>())
                return;
            new GameObject("TimeThief").AddComponent<GameManager>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            config = Resources.Load<GameConfig>("GameConfig");
            if (!config || config.enemies == null || config.enemies.Length == 0 || config.bosses == null || config.bosses.Length != 10)
            {
                Debug.LogError("GameConfig: assign enemy templates and all 10 BossData assets.");
                enabled = false;
                return;
            }

            platform = gameObject.AddComponent<PlatformBridge>();
            platform.Init(this);
            English = PlatformBridge.Language() != "ru";
            save = SaveManager.Load();
            player = config.startingStats.Copy();
            shop = new ShopManager();
            music = gameObject.AddComponent<MusicManager>();
            music.Init(config);
            ui = gameObject.AddComponent<UIManager>();
            ui.Init(this);
            SetState(GameState.Waiting);
            StartCoroutine(ReadyAfterFrame());
        }

        System.Collections.IEnumerator ReadyAfterFrame()
        {
            yield return new WaitForEndOfFrame();
            platform.Ready();
        }

        void Update()
        {
            if (ui == null)
                return;
            if (IsFighting)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
                if (!(shop.Has(BuffType.Freeze) && enemy.elapsed < 2))
                {
                    enemy.DrainPlayer(dt * enemy.FlowRate);
                    if (player.CurrentTime <= 0)
                    {
                        Lose();
                        return;
                    }
                }

                enemy.Tick(dt);
            }

            if (state != GameState.Waiting && state != GameState.GameOver && Time.unscaledTime - saveTimer > 5)
            {
                Persist();
                saveTimer = Time.unscaledTime;
            }
        }

        public void NewRun()
        {
            if (!ActionsReady)
                return;
            seed = Random.Range(1, int.MaxValue);
            level = 1;
            defeated = 0;
            reviveUsed = false;
            rewardDoubled = false;
            player = config.startingStats.Copy();
            shop = new ShopManager();
            save.activeRun = true;
            Prepare();
        }

        public void ResumeRun()
        {
            if (!save.activeRun || !ActionsReady)
                return;
            seed = save.seed;
            level = save.level;
            defeated = save.defeated;
            lastReward = save.lastReward;
            lastTimeReward = save.lastTimeReward; lastTimeAwarded = save.lastTimeAwarded;
            miniReward = save.lastGift;
            reviveUsed = save.reviveUsed;
            rewardDoubled = save.rewardDoubled;
            player = save.player.Copy();
            shop = save.shop;
            enemy = new EnemyController(this, EnemyGenerator.Generate(config, level, seed));
            enemy.time = Mathf.Clamp(save.enemyTime, 0, 1e12f);
            enemy.timer = Mathf.Clamp(save.enemyTimer, 0, enemy.data.cooldown);
            enemy.elapsed = Mathf.Max(0, save.elapsed);
            enemy.attackCount = save.enemyAttacks;
            enemy.physicalHits = save.physicalHits;
            enemy.bossPhase2 = save.bossPhase2;
            // Old encounters keep life, upgrades and phase; only incompatible boss puzzles reset.
            if (save.bossRulesVersion == BossRules.Version)
            {
                enemy.bossStage = save.bossStage; enemy.bossProgress = save.bossProgress; enemy.bossWindow = save.bossWindow;
                enemy.bossIdle = save.bossIdle; enemy.bossStoredDamage = save.bossStoredDamage;
            }
            if (save.phase == "Victory")
            {
                SetState(GameState.Victory);
            }
            else if (save.phase == "RewardSelection")
            {
                rewards = RewardManager.Choices(seed, level, true);
                SetState(GameState.RewardSelection);
            }
            else if (save.phase == "GameOver" || player.CurrentTime <= 0)
            {
                SetState(GameState.GameOver);
            }
            else
            {
                SetState(GameState.Intro);
            }

        }

        void PlayEncounterMusic()
        {
            if (enemy.data.type == EncounterType.Boss)
            {
                var b = enemy.data.boss;
                music.PlayBossMusic(level > 250 && config.proceduralBossMusic ? config.proceduralBossMusic : b ? b.bossMusic : null);
            }
            else
                music.PlayNormalMusic();
        }

        void Prepare()
        {
            enemy = new EnemyController(this, EnemyGenerator.Generate(config, level, seed));
            miniReward = null;
            rewardDoubled = false;
            lastReward = 0; lastTimeReward = lastTimeAwarded = 0;
            save.bestLevel = Mathf.Max(save.bestLevel, level);
            PlayEncounterMusic();
            SetState(GameState.Intro);
            if (enemy.data.type != EncounterType.Normal)
                music.Sfx(enemy.data.type == EncounterType.Boss ? config.bossIntroSound : config.miniBossIntroSound);
            Persist();
        }

        public void BeginFight()
        {
            if (state != GameState.Intro || !ActionsReady)
                return;
            SetState(GameState.Fighting);
            Persist();
        }

        public void Win()
        {
            if (!IsFighting)
                return;
            state = GameState.Transition;
            ui.input.Cancel();
            defeated++;
            save.totalEnemiesDefeated++;
            lastTimeReward = RewardManager.TimeReward(enemy.data) * (shop.Has(BuffType.DoubleTime) ? 2 : 1);
            shop.EndBattle();
            var previous = player.Copy();
            player.Grow(config.growth);
            save.lastGrowth = new float[6];
            for (int i = 0; i < 6; i++) save.lastGrowth[i] = player.Get((Stat)i) - previous.Get((Stat)i);
            lastTimeAwarded = player.AddTime(lastTimeReward);
            music.Sfx(config.victorySound);
            if (enemy.data.type == EncounterType.Boss)
            {
                save.totalBossesDefeated++;
                rewards = RewardManager.Choices(seed, level, true);
                SetState(GameState.RewardSelection);
            }
            else
            {
                if (enemy.data.type == EncounterType.MiniBoss)
                {
                    miniReward = RewardManager.Choices(seed, level, false)[0];
                    miniReward.Apply(player);
                }

                SetState(GameState.Victory);
            }

            Persist();
        }

        public void ChooseReward(int i)
        {
            if (state != GameState.RewardSelection || !ActionsReady || i < 0 || i >= rewards.Length)
                return;
            var chosen = rewards[i];
            if (chosen.stat == Stat.CritChance && player.CritChance >= .75f) return;
            float before = player.Get(chosen.stat);
            chosen.Apply(player);
            miniReward = new Reward { stat = chosen.stat, percent = chosen.percent,
                amount = chosen.percent ? chosen.amount : player.Get(chosen.stat) - before };
            SetState(GameState.Victory);
            music.Sfx(config.upgradeSound);
            Persist();
        }

        public void Continue()
        {
            if (state != GameState.Victory || !ActionsReady)
                return;
            ui.input.Cancel();
            SetState(GameState.Transition);
            if (defeated % 5 == 0 && Time.unscaledTime - lastAd > 180 && platform.CanAd)
            {
                lastAd = Time.unscaledTime;
                platform.Ad(false, _ => Next());
            }
            else
                Next();
        }

        void Next()
        {
            level++;
            Prepare();
        }

        public void Lose()
        {
            if (!IsFighting)
                return;
            player.CurrentTime = 0;
            ui.input.Cancel();
            SetState(GameState.GameOver);
            music.Sfx(config.gameOverSound);
            Persist();
        }

        public void Bonus()
        {
            if (state != GameState.Victory || rewardDoubled || !ActionsReady)
                return;
            platform.Ad(true, ok =>
            {
                if (ok && !rewardDoubled)
                {
                    rewardDoubled = true;
                    lastTimeAwarded += player.AddTime(lastTimeReward);
                    Persist();
                }

                ui.Refresh();
            });
        }

        public void Revive()
        {
            if (state != GameState.GameOver || reviveUsed || !ActionsReady)
                return;
            platform.Ad(true, ok =>
            {
                if (ok && !reviveUsed)
                {
                    reviveUsed = true;
                    player.CurrentTime = player.MaxTime;
                    enemy.timer = Mathf.Max(enemy.timer, 2);
                    SetState(GameState.Intro);
                    Persist();
                }
                else
                    ui.Refresh();
            });
        }

        public void OpenStats()
        {
            if (state == GameState.Victory && ActionsReady)
                SetState(GameState.Stats);
        }

        public void OpenShop()
        {
            if (state == GameState.Victory && ActionsReady)
            {
                SetState(GameState.Shop);
                music.Sfx(config.shopSound);
            }
        }

        public void Back()
        {
            if ((state == GameState.Shop || state == GameState.Stats) && ActionsReady)
                SetState(GameState.Victory);
        }

        public void Buy(int item)
        {
            if (state == GameState.Shop && ActionsReady && shop.Buy(item, player))
            {
                Persist();
                music.Sfx(config.upgradeSound);
                ui.Refresh();
            }
        }

        public void Menu()
        {
            Persist();
            manualPause = false;
            SetState(GameState.Waiting);
            SyncPause();
        }

        public void TogglePause()
        {
            if (state == GameState.Fighting)
            {
                manualPause = !manualPause;
                SyncPause();
                ui.Refresh();
            }
        }

        public void SetPlatformPause(bool value)
        {
            if (platformPause == value)
                return;
            platformPause = value;
            SyncPause();
            if (value)
                Persist();
        }

        public void SetAdPause(bool value)
        {
            adPause = value;
            SyncPause();
        }

        void SyncPause()
        {
            ui?.input?.Cancel();
            AudioListener.pause = Paused;
            platform?.Gameplay(IsFighting);
        }

        public void SetState(GameState s)
        {
            state = s;
            if (s == GameState.Intro || s == GameState.Fighting)
                PlayEncounterMusic();
            else if (s != GameState.Transition)
                music?.PlayMenuMusic();
            guard = Time.unscaledTime + (s == GameState.Victory || s == GameState.RewardSelection || s == GameState.GameOver ? .65f : .15f);
            ui?.input?.Cancel();
            platform?.Gameplay(IsFighting);
            ui?.Refresh();
        }

        public void Persist()
        {
            if (save == null)
                return;
            if (state != GameState.Waiting && enemy != null)
            {
                save.activeRun = true;
                save.seed = seed;
                save.level = level;
                save.defeated = defeated;
                save.lastReward = 0;
                save.lastTimeReward = lastTimeReward; save.lastTimeAwarded = lastTimeAwarded;
                save.lastGift = miniReward;
                save.player = player.Copy();
                save.shop = shop;
                save.reviveUsed = reviveUsed;
                save.rewardDoubled = rewardDoubled;
                save.phase = state == GameState.Shop || state == GameState.Stats || state == GameState.Transition ? "Victory" : state.ToString();
                save.enemyTime = enemy.time;
                save.enemyTimer = enemy.timer;
                save.elapsed = enemy.elapsed;
                save.enemyAttacks = enemy.attackCount;
                save.physicalHits = enemy.physicalHits;
                save.bossPhase2 = enemy.bossPhase2;
                save.bossStage = enemy.bossStage; save.bossProgress = enemy.bossProgress; save.bossWindow = enemy.bossWindow;
                save.bossRulesVersion = BossRules.Version; save.bossIdle = enemy.bossIdle; save.bossStoredDamage = enemy.bossStoredDamage;
            }

            SaveManager.Write(save);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                Persist();
        }

        void OnApplicationQuit() => Persist();
    }
}
