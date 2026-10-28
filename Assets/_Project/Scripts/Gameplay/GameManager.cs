using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ZeroGCargo.Audio;
using ZeroGCargo.Data;
using ZeroGCargo.UI;

namespace ZeroGCargo.Gameplay
{
    /// <summary>Persistent prototype composition root and scene navigation owner.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        private GameConfig config;
        private InertiaConfig inertia;
        private LevelData[] levels;
        private SaveSystem save;
        private StationView station;
        private LevelManager level;
        private PathDrawer drawer;
        private GameHud hud;
        private AudioFeedback audioFeedback;
        private GameScreen screen;
        private GameScreen returnScreen;
        private int levelIndex;
        private float lastBack = -10;
        private bool initialized;
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            if (instance != this) return;
            config = Resources.Load<GameConfig>("Config/GameConfig");
            inertia = Resources.Load<InertiaConfig>("Config/InertiaConfig");
            levels = Resources.LoadAll<LevelData>("Levels");
            if (config == null || inertia == null || levels.Length == 0)
            { Debug.LogError("Project assets missing. Run Zero-G Cargo > Prepare prototype in the Unity Editor."); enabled = false; return; }
            Array.Sort(levels, (a, b) => a.LevelId.CompareTo(b.LevelId));
            for (int i = 0; i < levels.Length; i++)
                if (!levels[i].IsValid || levels[i].LevelId != i + 1)
                { Debug.LogError("Tutorial layouts must be valid and sequential."); enabled = false; return; }
            Application.targetFrameRate = 60; Time.fixedDeltaTime = 1f / 60;
            Screen.orientation = ScreenOrientation.Portrait;
            var cameraObject = new GameObject("Station camera", typeof(Camera), typeof(AudioListener)); cameraObject.transform.SetParent(transform);
            Camera camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = Mathf.Max(config.CameraSize, config.BoundaryX / camera.aspect * 1.06f);
            camera.backgroundColor = ColorTokens.Background; camera.clearFlags = CameraClearFlags.SolidColor; camera.transform.position = new Vector3(0, 0, -10);
            var eventSystem = new GameObject("Input events", typeof(EventSystem), typeof(StandaloneInputModule)); eventSystem.transform.SetParent(transform);
            save = new SaveSystem(); station = new StationView(config); level = new LevelManager(config, inertia, station);
            drawer = new PathDrawer(camera, config, inertia); hud = new GameHud(levels); audioFeedback = new AudioFeedback(gameObject);
            drawer.OnPathComplete += level.Follow;
            level.OnShuttleCollided += Collided; level.OnShuttleDocked += Docked; level.OnLevelComplete += Completed;
            hud.OnNavigate += Navigate; hud.OnStartLevel += StartLevel; hud.OnAdvance += Advance; hud.OnToggleSound += ToggleSound;
            initialized = true; Navigate(GameScreen.MainMenu);
        }
        private void Update()
        {
            if (!initialized) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
            if (screen != GameScreen.Gameplay) return;
            drawer.Tick(level.Position, level.CanDraw); level.DrawPath(drawer.Path);
        }
        private void FixedUpdate() { if (initialized && screen == GameScreen.Gameplay) level.Step(Time.fixedDeltaTime); }
        private void StartLevel(int id)
        {
            if (id == 0) id = Mathf.Min(save.Data.highestUnlockedLevel, levels.Length);
            if (id < 1 || id > levels.Length || id > save.Data.highestUnlockedLevel) return;
            levelIndex = id - 1; drawer.Cancel(); drawer.Path.Clear(); level.Load(levels[levelIndex]);
            hud.SetLevel(levels[levelIndex]); Navigate(GameScreen.Gameplay);
        }
        private void Navigate(GameScreen target)
        {
            if ((target == GameScreen.Settings || target == GameScreen.Privacy) && screen == target) target = returnScreen;
            else if (target == GameScreen.Settings || target == GameScreen.Privacy) returnScreen = screen;
            screen = target; drawer.Cancel(); hud.Refresh(save.Data, levels); hud.Show(screen);
            station.SetVisible(screen == GameScreen.Gameplay);
            string sceneName = screen == GameScreen.MainMenu ? "MainMenu" : screen == GameScreen.LevelSelect ? "LevelSelect" : "Gameplay";
            if (SceneManager.GetActiveScene().name != sceneName) SceneManager.LoadScene(sceneName);
        }
        private void Advance(bool next)
        {
            if (next && levelIndex + 1 >= levels.Length) { Navigate(GameScreen.LevelSelect); return; }
            StartLevel(levelIndex + (next ? 2 : 1));
        }
        private void Collided()
        {
            drawer.Path.Clear(); hud.SetStatus("A little too much momentum. Let's try again.\nAfter two collisions, a ghost route will help.");
            audioFeedback.Collision(save.Data.settings.soundOn);
        }
        private void Docked() { drawer.Path.Clear(); audioFeedback.Dock(save.Data.settings.soundOn); }
        private void Completed(int stars)
        {
            save.Data.Complete(levelIndex + 1, stars, 1); bool saved = save.Save();
            hud.SetResults(stars, level.Attempts, levelIndex + 1 < levels.Length, saved); Navigate(GameScreen.Win);
        }
        private void ToggleSound() { save.Data.settings.soundOn = !save.Data.settings.soundOn; save.Save(); hud.Refresh(save.Data, levels); }
        private void Back()
        {
            switch (screen)
            {
                case GameScreen.Gameplay: Navigate(GameScreen.Pause); break;
                case GameScreen.Pause: Navigate(GameScreen.Gameplay); break;
                case GameScreen.Settings: case GameScreen.Privacy: Navigate(returnScreen); break;
                case GameScreen.LevelSelect: Navigate(GameScreen.MainMenu); break;
                case GameScreen.Win: Navigate(GameScreen.LevelSelect); break;
                case GameScreen.MainMenu:
                    if (Time.unscaledTime - lastBack < 2) Application.Quit();
                    else { lastBack = Time.unscaledTime; hud.Toast("Press back again to exit"); }
                    break;
            }
        }
        private void OnApplicationPause(bool paused)
        {
            if (!initialized || !paused) return;
            save.Save(); if (screen == GameScreen.Gameplay) Navigate(GameScreen.Pause);
        }
        private void OnApplicationFocus(bool focused)
        { if (!focused) OnApplicationPause(true); }
        private void OnDestroy()
        {
            if (instance != this) return;
            if (initialized)
            {
                drawer.OnPathComplete -= level.Follow;
                level.OnShuttleCollided -= Collided; level.OnShuttleDocked -= Docked; level.OnLevelComplete -= Completed;
                hud.OnNavigate -= Navigate; hud.OnStartLevel -= StartLevel; hud.OnAdvance -= Advance; hud.OnToggleSound -= ToggleSound;
                save.Save(); hud.Dispose(); station.Dispose(); audioFeedback.Dispose();
            }
            instance = null;
        }
    }
}
