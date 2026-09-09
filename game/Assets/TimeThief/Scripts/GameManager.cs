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
                    player.LoseTime(dt);
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
            reviveUsed = save.reviveUsed;
            rewardDoubled = save.rewardDoubled;
            player = save.player.Copy();
            shop = save.shop;
            enemy = new EnemyController(this, EnemyGenerator.Generate(config, level, seed));
            enemy.time = Mathf.Clamp(save.enemyTime, 0, enemy.data.maxTime);
            enemy.timer = Mathf.Clamp(save.enemyTimer, 0, enemy.data.cooldown);
            enemy.elapsed = Mathf.Max(0, save.elapsed);
            enemy.attackCount = save.enemyAttacks;
            enemy.physicalHits = save.physicalHits;
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

            PlayEncounterMusic();
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
            lastReward = 0;
            save.bestLevel = Mathf.Max(save.bestLevel, level);
            PlayEncounterMusic();
            if (enemy.data.type != EncounterType.Normal || level <= 5)
                SetState(GameState.Intro);
            else
                SetState(GameState.Fighting);
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
            lastReward = RewardManager.Shards(enemy.data) * (shop.Has(BuffType.DoubleShards) ? 2 : 1);
            shop.shards += lastReward;
            shop.EndBattle();
            player.Grow(config.growth);
            player.AddTime(.35f);
            music.Sfx(config.victorySound);
            music.PlayNormalMusic();
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
            rewards[i].Apply(player);
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
                    shop.shards += lastReward;
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
                save.lastReward = lastReward;
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
