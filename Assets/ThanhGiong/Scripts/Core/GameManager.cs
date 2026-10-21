using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThanhGiong
{
    public enum GameState { Menu, Story, Playing, Upgrade, Paused, GameOver, Victory }

    /// <summary>
    /// Bộ não của game: khởi tạo mọi hệ thống, quản lý trạng thái (máy trạng thái của game),
    /// điểm số, kỷ lục và luồng chơi: Menu → Cốt truyện → Các đợt giặc → Nâng cấp → Chiến thắng / Thất bại.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public GameState State { get; private set; }
        public Player Player { get; private set; }
        public CameraFollow Cam { get; private set; }
        public UIManager UI { get; private set; }
        public WaveManager Waves { get; private set; }
        public FXManager FX { get; private set; }
        public AudioManager Audio { get; private set; }
        public Transform RuntimeRoot { get; private set; }
        public readonly List<Enemy> Enemies = new();

        public int Kills { get; private set; }
        public int Score { get; private set; }
        public float PlayTime { get; private set; }
        public int HighScore { get; private set; }

        /// <summary>Tốc độ thời gian bình thường (chế độ tự kiểm thử chạy nhanh hơn).</summary>
        public float BaseTimeScale => autoTest ? 2.5f : 1f;

        /// <summary>Bất tử — chỉ dùng khi kiểm thử (tham số dòng lệnh -god).</summary>
        public bool GodMode { get; private set; }

        const string HighScoreKey = "ThanhGiong_HighScore";
        static readonly Color SkyColor = new(0.62f, 0.78f, 0.92f);

        int stateFrame;
        bool storySeen, autoTest;
        List<Upgrade> currentUpgrades;
        float autoTimer, nextShotTime = 5f;
        string shotsDir;
        int shotIndex;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;
            var args = System.Environment.GetCommandLineArgs();
            autoTest = System.Array.IndexOf(args, "-autotest") >= 0;
            GodMode = System.Array.IndexOf(args, "-god") >= 0;
            int shotsArg = System.Array.IndexOf(args, "-shots");
            if (shotsArg >= 0 && shotsArg + 1 < args.Length) shotsDir = args[shotsArg + 1];
            Time.timeScale = BaseTimeScale;
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);

            SetupCameraAndLight();
            WorldBuilder.Build(new GameObject("TheGioi").transform);
            RuntimeRoot = new GameObject("Runtime").transform;

            Audio = gameObject.AddComponent<AudioManager>();
            FX = gameObject.AddComponent<FXManager>();
            Waves = gameObject.AddComponent<WaveManager>();
            UI = gameObject.AddComponent<UIManager>();

            Player = new GameObject("ThanhGiong").AddComponent<Player>();
            Player.Build();
            Player.ResetForNewRun();
            Player.AutoPilot = autoTest;
            Cam.target = Player.transform;
            Cam.Snap();
        }

        void Start()
        {
            if (autoTest)
            {
                Log("Chế độ tự kiểm thử bật");
                storySeen = true;
                if (shotsDir != null) GoToMenu(); // để chụp màn hình menu trước
                else StartGame();
            }
            else GoToMenu();
        }

        void SetupCameraAndLight()
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            Cam = camGo.AddComponent<CameraFollow>();

            var sun = new GameObject("MatTroi").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;
            sun.transform.rotation = Quaternion.Euler(50f, 100f, 0f);

            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.52f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.22f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SkyColor;
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 170f;
        }

        void SetState(GameState s)
        {
            State = s;
            stateFrame = Time.frameCount;
        }

        void Update()
        {
            if (autoTest) UpdateAutoTest();
            if (Time.frameCount == stateFrame) return; // tránh một lần bấm kích hoạt 2 hành động

            switch (State)
            {
                case GameState.Menu:
                    if (UI.HelpOpen) { if (InputReader.Help || InputReader.Cancel) UI.ToggleHelp(); }
                    else if (InputReader.Help) UI.ToggleHelp();
                    else if (InputReader.Confirm) OnStartPressed();
                    break;

                case GameState.Story:
                    if (InputReader.Cancel) StartGame();
                    else if (InputReader.Confirm || InputReader.Click)
                    {
                        if (!UI.NextStorySlide()) StartGame();
                    }
                    break;

                case GameState.Playing:
                    PlayTime += Time.unscaledDeltaTime;
                    if (InputReader.Pause) Pause();
                    break;

                case GameState.Paused:
                    if (UI.HelpOpen) { if (InputReader.Help || InputReader.Cancel) UI.ToggleHelp(); }
                    else if (InputReader.Pause) Resume();
                    else if (InputReader.Restart) StartGame();
                    else if (InputReader.MenuKey) GoToMenu();
                    else if (InputReader.Help) UI.ToggleHelp();
                    break;

                case GameState.Upgrade:
                    for (int i = 1; i <= 3; i++)
                        if (InputReader.Number(i)) { ChooseUpgrade(i - 1); break; }
                    break;

                case GameState.GameOver:
                case GameState.Victory:
                    if (!UI.EndPanelShown) break;
                    if (InputReader.Restart || InputReader.Confirm) StartGame();
                    else if (InputReader.MenuKey || InputReader.Cancel) GoToMenu();
                    break;
            }
        }

        // ------------------------------------------------------------------ Điều hướng
        public void GoToMenu()
        {
            StopAllCoroutines();
            ClearRuntime();
            Player.ResetForNewRun();
            Time.timeScale = BaseTimeScale;
            Cam.orbit = true;
            UI.ShowMenu(HighScore);
            Audio.PlayMusic();
            SetState(GameState.Menu);
        }

        public void OnStartPressed()
        {
            if (State != GameState.Menu) return;
            if (!storySeen)
            {
                storySeen = true;
                UI.ShowStory();
                SetState(GameState.Story);
            }
            else StartGame();
        }

        public void StartGame()
        {
            StopAllCoroutines();
            ClearRuntime();
            Player.ResetForNewRun();
            Kills = 0;
            Score = 0;
            PlayTime = 0f;
            Time.timeScale = BaseTimeScale;
            Cam.orbit = false;
            Cam.Snap();
            UI.ShowHud();
            Audio.PlayMusic();
            SetState(GameState.Playing);
            Waves.BeginWave(1);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            UI.ShowPause(true);
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = BaseTimeScale;
            UI.ShowPause(false);
            SetState(GameState.Playing);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ClearRuntime()
        {
            foreach (Transform child in RuntimeRoot) Destroy(child.gameObject);
            Enemies.Clear();
            Waves.StopAll();
            FX.ClearAll();
            foreach (var b in BambooClump.All) b.ResetClump();
            UI.ClearFloating();
        }

        // ------------------------------------------------------------------ Sự kiện trong trận
        public void OnEnemyKilled(Enemy e)
        {
            Kills++;
            AddScore(e.ScoreValue);
            if (Random.value < e.DropChance) RicePickup.Spawn(e.transform.position);
        }

        void AddScore(int amount) => Score += amount;

        public void OnWaveCleared(int wave)
        {
            Log($"Thắng đợt {wave} — điểm {Score}, máu {Player.HP:0}/{Player.MaxHP:0}");
            StartCoroutine(WaveClearedRoutine(wave));
        }

        IEnumerator WaveClearedRoutine(int wave)
        {
            AddScore(300 * wave);
            if (wave >= WaveManager.TotalWaves)
            {
                yield return VictoryRoutine();
                yield break;
            }
            UI.ShowBanner($"Đã đánh lui đợt {wave}!", "Gióng vươn vai lớn thêm...", 2.2f);
            yield return new WaitForSeconds(1.2f);
            if (State != GameState.Playing) yield break;
            Player.Grow();
            yield return new WaitForSeconds(1.3f);
            if (State != GameState.Playing) yield break;

            currentUpgrades = UpgradeSystem.Roll(3);
            UI.ShowUpgrades(currentUpgrades);
            Time.timeScale = 0f;
            SetState(GameState.Upgrade);
            autoTimer = 0f;
        }

        public void ChooseUpgrade(int index)
        {
            if (State != GameState.Upgrade || currentUpgrades == null || index >= currentUpgrades.Count) return;
            var up = currentUpgrades[index];
            up.Apply(Player);
            Log($"Chọn nâng cấp: {up.Name}");
            UI.HideUpgrades();
            UI.FloatText(Player.transform.position + Vector3.up * 4f * Player.GrowthScale, up.Name, Palette.Gold, 1.3f);
            Time.timeScale = BaseTimeScale;
            SetState(GameState.Playing);
            Waves.BeginWave(Waves.CurrentWave + 1);
        }

        public void OnPlayerDied()
        {
            if (State != GameState.Playing) return;
            Log($"Gióng ngã xuống ở đợt {Waves.CurrentWave} — điểm {Score}");
            SetState(GameState.GameOver);
            Waves.StopAll();
            StartCoroutine(GameOverRoutine());
        }

        IEnumerator GameOverRoutine()
        {
            Time.timeScale = 0.35f;
            Audio.Play(Sfx.Defeat, 0.9f, 0f);
            yield return new WaitForSecondsRealtime(2f);
            Time.timeScale = BaseTimeScale;
            bool record = SaveHighScore();
            UI.ShowGameOver(Score, Kills, Waves.CurrentWave, HighScore, record);
        }

        IEnumerator VictoryRoutine()
        {
            SetState(GameState.Victory);
            Waves.StopAll();
            foreach (var e in new List<Enemy>(Enemies)) e.ForceKill();
            Audio.Play(Sfx.Victory, 1f, 0f);
            UI.ShowBanner("ĐÁNH TAN GIẶC ÂN!", "Giặc Ân tan vỡ, bỏ chạy tán loạn", 3f);
            yield return new WaitForSecondsRealtime(3f);

            Player.BeginAscend();
            UI.ShowBanner("Gióng phi ngựa lên đỉnh Sóc Sơn...", "rồi cả người lẫn ngựa bay thẳng về trời", 4.5f);
            yield return new WaitForSecondsRealtime(5.5f);

            int bonus = Mathf.RoundToInt(Player.HP * 10f) + Mathf.Max(0, 3000 - Mathf.RoundToInt(PlayTime) * 5);
            AddScore(bonus);
            bool record = SaveHighScore();
            Log($"CHIẾN THẮNG — điểm {Score}, hạ {Kills} giặc, thời gian {PlayTime:0}s");
            UI.ShowVictory(Score, Kills, PlayTime, HighScore, record);
        }

        bool SaveHighScore()
        {
            if (Score <= HighScore || autoTest) return false;
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
            return true;
        }

        // ------------------------------------------------------------------ Tự kiểm thử (chạy với tham số -autotest)
        public void Log(string message)
        {
            if (autoTest) Debug.Log("[THANHGIONG] " + message);
        }

        void UpdateAutoTest()
        {
            autoTimer += Time.unscaledDeltaTime;
            if (shotsDir != null && shotIndex < 12 && Time.realtimeSinceStartup > nextShotTime)
            {
                nextShotTime += 9f;
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(shotsDir, $"shot_{shotIndex++:00}_{State}.png"));
            }
            if (State == GameState.Menu && autoTimer > 7f) StartGame();
            if (State == GameState.Upgrade && autoTimer > 2.5f) ChooseUpgrade(0);
            if ((State == GameState.GameOver || State == GameState.Victory) && UI.EndPanelShown)
            {
                Log("KẾT THÚC KIỂM THỬ: " + State);
                Application.Quit();
            }
            if (Time.realtimeSinceStartup > 600f)
            {
                Log("HẾT GIỜ KIỂM THỬ ở đợt " + Waves.CurrentWave);
                Application.Quit();
            }
        }
    }
}
