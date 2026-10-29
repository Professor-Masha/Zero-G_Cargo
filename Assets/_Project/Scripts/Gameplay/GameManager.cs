using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ZeroGCargo.Audio;
using ZeroGCargo.Core;
using ZeroGCargo.Data;
using ZeroGCargo.UI;

namespace ZeroGCargo.Gameplay
{
    /// <summary>Offline campaign composition, lifecycle, navigation and persistent progression.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        private GameConfig config;
        private Camera stationCamera;
        private int lastWidth;
        private int lastHeight;
        private LevelDefinition[] levels;
        private SaveSystem save;
        private CampaignSimulation simulation;
        private StationView station;
        private PathDrawer drawer;
        private GameHud hud;
        private AudioFeedback audioFeedback;
        private GameScreen screen;
        private GameScreen settingsOrigin;
        private GameScreen privacyOrigin;
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
            try { Initialize(); }
            catch (Exception e) { Debug.LogException(e); enabled = false; }
        }
        private void Initialize()
        {
            config = Resources.Load<GameConfig>("Config/GameConfig");
            var inertia = Resources.Load<InertiaConfig>("Config/InertiaConfig");
            var source = Resources.Load<TextAsset>("Levels/campaign");
            if (config == null || inertia == null || source == null || source.bytes.Length > 1024*1024) throw new InvalidOperationException("Missing or oversized campaign resources. Run Zero-G Cargo > Prepare game.");
            CampaignData campaign = JsonUtility.FromJson<CampaignData>(source.text);
            if (campaign == null || campaign.version != 1 || campaign.levels == null || campaign.levels.Length != 50) throw new InvalidOperationException("Expected the fifty-level offline campaign.");
            levels = campaign.levels;
            for (int i = 0; i < levels.Length; i++)
            { string error = LevelValidation.Error(levels[i]); if (error != null || levels[i].id != i+1) throw new InvalidOperationException("Level " + (i+1) + ": " + error); }
            Application.targetFrameRate = 60; Time.fixedDeltaTime = 1f/60; Screen.orientation = ScreenOrientation.Portrait;
            var cameraObject = new GameObject("Station camera",typeof(Camera),typeof(AudioListener)); cameraObject.transform.SetParent(transform);
            Camera camera = cameraObject.GetComponent<Camera>(); stationCamera = camera; camera.orthographic = true; camera.orthographicSize = Mathf.Max(config.CameraSize,config.BoundaryX/camera.aspect*1.1f);
            camera.backgroundColor = ColorTokens.Background; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0,10,-3.2f); camera.transform.LookAt(Vector3.zero);
            var lightObject = new GameObject("Station key light",typeof(Light)); lightObject.transform.SetParent(transform);
            var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.3f; light.color = new Color(.75f,.87f,1); light.transform.rotation = Quaternion.Euler(48,-30,0);
            RenderSettings.ambientLight = new Color(.25f,.33f,.45f);
            var eventSystem = new GameObject("Input events",typeof(EventSystem),typeof(StandaloneInputModule)); eventSystem.transform.SetParent(transform);
            save = new SaveSystem(); simulation = new CampaignSimulation(inertia.ToSettings(config.Lookahead));
            station = new StationView(config); drawer = new PathDrawer(camera,config); hud = new GameHud(levels); audioFeedback = new AudioFeedback(gameObject);
            drawer.OnPathComplete += Launch;
            simulation.NearMiss += NearMiss; simulation.Collided += Collided; simulation.Docked += Docked; simulation.Finished += Finished;
            hud.OnNavigate += Navigate; hud.OnStartLevel += StartLevel; hud.OnAdvance += Advance;
            hud.OnToggleSound += ToggleSound; hud.OnToggleMusic += ToggleMusic; hud.OnToggleHaptics += ToggleHaptics; hud.OnResetSave += ResetSave;
            initialized = true; audioFeedback.SetMusic(save.Data.settings.musicOn); Navigate(GameScreen.MainMenu);
        }
        private void Update()
        {
            if (!initialized) return;
            hud.UpdateSafeArea();
            if (lastWidth != Screen.width || lastHeight != Screen.height)
            {
                lastWidth = Screen.width; lastHeight = Screen.height;
                stationCamera.orthographicSize = Mathf.Max(config.CameraSize,config.BoundaryX/stationCamera.aspect*1.1f);
            }
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
            if (screen != GameScreen.Gameplay) return;
            drawer.Tick(simulation); station.Render(simulation,drawer.Path,drawer.Selected,Time.deltaTime); hud.SetTimer(simulation.TimerFraction);
            float speed = 0; for (int i = 0; i < simulation.Level.cargo.Length; i++) speed = Mathf.Max(speed,simulation.Shuttle(i).Flight.Velocity.Magnitude);
            audioFeedback.SetFlight(speed,save.Data.settings.soundOn);
        }
        private void FixedUpdate()
        {
            if (!initialized || screen != GameScreen.Gameplay) return;
            int delivered = simulation.Delivered; simulation.Step(Time.fixedDeltaTime);
            if (delivered != simulation.Delivered && screen == GameScreen.Gameplay) hud.SetProgress(simulation);
        }
        private void Launch(int index, PathData path) { simulation.Launch(index,path); }
        private void StartLevel(int id)
        {
            if (id == 0) id = Mathf.Min(save.Data.highestUnlockedLevel,levels.Length);
            if (id < 1 || id > levels.Length || id > save.Data.highestUnlockedLevel) return;
            levelIndex = id-1; drawer.Cancel(); simulation.Load(levels[levelIndex]); station.Load(levels[levelIndex]); hud.SetLevel(levels[levelIndex]); Navigate(GameScreen.Gameplay);
        }
        private void Navigate(GameScreen target)
        {
            if (target == GameScreen.Settings)
            {
                if (screen == GameScreen.Settings) target = settingsOrigin;
                else if (screen != GameScreen.Reset) settingsOrigin = screen;
            }
            if (target == GameScreen.Privacy)
            { if (screen == GameScreen.Privacy) target = privacyOrigin; else privacyOrigin = screen; }
            screen = target; drawer.Cancel(); hud.Refresh(save.Data,levels); hud.Show(screen);
            station.SetVisible(screen == GameScreen.Gameplay); audioFeedback.SetPaused(screen != GameScreen.Gameplay);
            string sceneName = screen == GameScreen.MainMenu ? "MainMenu" : screen == GameScreen.LevelSelect ? "LevelSelect" : "Gameplay";
            if (SceneManager.GetActiveScene().name != sceneName) SceneManager.LoadScene(sceneName);
        }
        private void Advance(bool next)
        {
            if (next && levelIndex+1 >= levels.Length) { Navigate(GameScreen.LevelSelect); return; }
            StartLevel(levelIndex+(next?2:1));
        }
        private void Collided(int index)
        {
            hud.SetStatus("A little too much momentum. Try a wider route."); audioFeedback.Collision(save.Data.settings.soundOn); Haptic();
        }
        private void NearMiss(int index) { audioFeedback.NearMiss(save.Data.settings.soundOn); }
        private void Docked(int index)
        {
            audioFeedback.Dock(save.Data.settings.soundOn); station.Spark(simulation.Level.bays[index].position.Vector); Haptic();
        }
        private void Finished(int outcome)
        {
            drawer.Cancel();
            if (outcome == 2) { Navigate(GameScreen.Fail); return; }
            save.Data.Complete(levelIndex+1,simulation.Stars,simulation.Delivered); bool saved = save.Save();
            hud.SetResults(simulation.Stars,simulation.Attempts,levelIndex+1<levels.Length,saved); Navigate(GameScreen.Win);
        }
        private void Haptic() { if (save.Data.settings.hapticsOn && !Application.isEditor && Application.isMobilePlatform) Handheld.Vibrate(); }
        private void ToggleSound() { save.Data.settings.soundOn = !save.Data.settings.soundOn; save.Save(); hud.Refresh(save.Data,levels); }
        private void ToggleMusic() { save.Data.settings.musicOn = !save.Data.settings.musicOn; audioFeedback.SetMusic(save.Data.settings.musicOn); save.Save(); hud.Refresh(save.Data,levels); }
        private void ToggleHaptics() { save.Data.settings.hapticsOn = !save.Data.settings.hapticsOn; save.Save(); hud.Refresh(save.Data,levels); }
        private void ResetSave()
        {
            bool reset = save.Reset(); audioFeedback.SetMusic(save.Data.settings.musicOn); Navigate(GameScreen.Settings);
            if (!reset) hud.Toast("Cannot reset a save from a newer game version.");
        }
        private void Back()
        {
            switch(screen)
            {
                case GameScreen.Gameplay: Navigate(GameScreen.Pause); break;
                case GameScreen.Pause: Navigate(GameScreen.Gameplay); break;
                case GameScreen.Settings: Navigate(settingsOrigin); break;
                case GameScreen.Privacy: Navigate(privacyOrigin); break;
                case GameScreen.Reset: Navigate(GameScreen.Settings); break;
                case GameScreen.LevelSelect: Navigate(GameScreen.MainMenu); break;
                case GameScreen.Win: case GameScreen.Fail: Navigate(GameScreen.LevelSelect); break;
                case GameScreen.MainMenu:
                    if (Time.unscaledTime-lastBack < 2) Application.Quit(); else { lastBack = Time.unscaledTime; hud.Toast("Press back again to exit"); }
                    break;
            }
        }
        private void OnApplicationPause(bool paused)
        { if (!initialized || !paused) return; save.Save(); if (screen == GameScreen.Gameplay) Navigate(GameScreen.Pause); }
        private void OnApplicationFocus(bool focused) { if (!Application.isEditor && !focused) OnApplicationPause(true); }
        private void OnDestroy()
        {
            if (instance != this) return;
            if (initialized)
            {
                drawer.OnPathComplete -= Launch; simulation.NearMiss -= NearMiss; simulation.Collided -= Collided; simulation.Docked -= Docked; simulation.Finished -= Finished;
                hud.OnNavigate -= Navigate; hud.OnStartLevel -= StartLevel; hud.OnAdvance -= Advance;
                hud.OnToggleSound -= ToggleSound; hud.OnToggleMusic -= ToggleMusic; hud.OnToggleHaptics -= ToggleHaptics; hud.OnResetSave -= ResetSave;
                save.Save(); hud.Dispose(); station.Dispose(); audioFeedback.Dispose();
            }
            instance = null;
        }
    }
}
