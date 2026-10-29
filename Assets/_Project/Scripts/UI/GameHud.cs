using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ZeroGCargo.Core;
using ZeroGCargo.Data;

namespace ZeroGCargo.UI
{
    /// <summary>Prebuilt TextMeshPro screens, reused without allocations during play.</summary>
    public sealed class GameHud
    {
        private readonly GameObject root;
        private readonly GameObject backdrop;
        private readonly RectTransform safeArea;
        private readonly Dictionary<GameScreen, GameObject> panels = new Dictionary<GameScreen, GameObject>();
        private readonly TMP_Text[] levelLabels;
        private readonly Button[] levelButtons;
        private readonly TMP_Text status;
        private readonly TMP_Text objectives;
        private readonly TMP_Text results;
        private readonly TMP_Text winTitle;
        private readonly TMP_Text campaignProgress;
        private readonly TMP_Text soundLabel;
        private readonly TMP_Text musicLabel;
        private readonly TMP_Text hapticsLabel;
        private readonly TimerArc timer;
        private static Sprite roundedSprite;
        private static TMP_FontAsset bodyFont;
        private static TMP_FontAsset headingFont;
        private readonly Button next;
        private readonly TMP_Text toast;
        private Rect lastSafeArea;
        private int lastScreenWidth;
        private int lastScreenHeight;
        /// <summary>Navigation requested by a visible button.</summary>
        public event Action<GameScreen> OnNavigate;
        /// <summary>Level launch requested by a visible unlocked tile.</summary>
        public event Action<int> OnStartLevel;
        /// <summary>Retry or next-level action.</summary>
        public event Action<bool> OnAdvance;
        /// <summary>Sound preference toggle.</summary>
        public event Action OnToggleSound;
        /// <summary>Music preference toggle.</summary>
        public event Action OnToggleMusic;
        /// <summary>Haptic preference toggle.</summary>
        public event Action OnToggleHaptics;
        /// <summary>Explicitly confirmed local progress reset.</summary>
        public event Action OnResetSave;
        /// <summary>Creates UI once using the bundled TMP default font.</summary>
        public GameHud(LevelDefinition[] levels)
        {
            bodyFont = Resources.Load<TMP_FontAsset>("Fonts/Inter"); headingFont = Resources.Load<TMP_FontAsset>("Fonts/Orbitron");
            root = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            // Keep the full portrait layout visible even in a wide Editor Game view.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            backdrop = new GameObject("Station backdrop", typeof(RectTransform), typeof(RawImage));
            backdrop.transform.SetParent(root.transform, false); Stretch(backdrop.GetComponent<RectTransform>());
            backdrop.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("Art/StationBackdrop");
            backdrop.GetComponent<RawImage>().raycastTarget = false;
            safeArea = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>(); safeArea.SetParent(root.transform, false);
            UpdateSafeArea();
            foreach (GameScreen screen in Enum.GetValues(typeof(GameScreen)))
            {
                var panel = new GameObject(screen.ToString(), typeof(RectTransform)); panel.transform.SetParent(safeArea, false);
                Stretch(panel.GetComponent<RectTransform>());
                if (screen != GameScreen.Gameplay)
                { var bg = panel.AddComponent<Image>(); bg.color = new Color(10/255f,14/255f,26/255f,.94f); }
                panels.Add(screen, panel);
            }
            toast = Label(safeArea, "", 22, -540, 60, ColorTokens.Muted);
            toast.transform.SetAsLastSibling();
            var menu = panels[GameScreen.MainMenu].transform;
            Label(menu, "ZERO-G\nCARGO", 60, 220, 180, ColorTokens.Text);
            Label(menu, "A QUIET SHIFT. A LITTLE MOMENTUM.", 20, 70, 80, ColorTokens.Muted);
            Button(menu, "Begin shift", -70, () => OnStartLevel?.Invoke(0));
            Button(menu, "Level select", -155, () => OnNavigate?.Invoke(GameScreen.LevelSelect), false);
            Button(menu, "Settings", -240, () => OnNavigate?.Invoke(GameScreen.Settings), false);
            Button(menu, "Privacy", -325, () => OnNavigate?.Invoke(GameScreen.Privacy), false);
            campaignProgress = Label(menu, "50 SECTORS  /  ONE QUIET STATION", 18, -470, 90, ColorTokens.Muted);
            var select = panels[GameScreen.LevelSelect].transform;
            Label(select, "SECTOR MAP", 40, 420, 70, ColorTokens.Text);
            Label(select, "Master momentum. Earn every star.", 24, 345, 60, ColorTokens.Muted);
            levelLabels = new TMP_Text[levels.Length]; levelButtons = new Button[levels.Length];
            var scrollObject = new GameObject("Scrollable sector map", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            scrollObject.transform.SetParent(select, false);
            var scrollRect = scrollObject.GetComponent<RectTransform>(); scrollRect.sizeDelta = new Vector2(570,660); scrollRect.anchoredPosition = new Vector2(0,-30);
            scrollObject.GetComponent<Image>().color = ColorTokens.Panel; scrollObject.GetComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Sector tiles", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(scrollObject.transform,false);
            content.anchorMin = new Vector2(0,1); content.anchorMax = new Vector2(1,1); content.pivot = new Vector2(.5f,1);
            content.sizeDelta = new Vector2(0,Mathf.CeilToInt(levels.Length/5f)*112+20); content.anchoredPosition = Vector2.zero;
            var scroll = scrollObject.GetComponent<ScrollRect>(); scroll.viewport = scrollRect; scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            for (int i = 0; i < levels.Length; i++)
            {
                int index = i; levelButtons[i] = Button(content,"",0,()=>OnStartLevel?.Invoke(index+1),false);
                var rect = levelButtons[i].GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.5f,1); rect.anchorMax = new Vector2(.5f,1);
                rect.anchoredPosition = new Vector2((i%5-2)*108,-65-(i/5)*112); rect.sizeDelta = new Vector2(96,96);
                levelLabels[i] = levelButtons[i].GetComponentInChildren<TMP_Text>();
            }
            Button(select,"Main menu",-470,()=>OnNavigate?.Invoke(GameScreen.MainMenu),false);
            var game = panels[GameScreen.Gameplay].transform;
            var arcObject = new GameObject("Elapsed time arc",typeof(RectTransform),typeof(TimerArc)); arcObject.transform.SetParent(game,false);
            Stretch(arcObject.GetComponent<RectTransform>()); timer = arcObject.GetComponent<TimerArc>(); timer.color = ColorTokens.Accent; timer.raycastTarget = false;
            objectives = Label(game, "", 23, 490, 110, ColorTokens.Text);
            var pauseButton = Button(game, "II", 0, () => OnNavigate?.Invoke(GameScreen.Pause), false);
            var pauseRect = pauseButton.GetComponent<RectTransform>();
            pauseRect.anchorMin = Vector2.one; pauseRect.anchorMax = Vector2.one; pauseRect.pivot = Vector2.one;
            pauseRect.anchoredPosition = Vector2.zero; pauseRect.sizeDelta = new Vector2(72, 72);
            pauseButton.GetComponentInChildren<TMP_Text>().GetComponent<RectTransform>().sizeDelta = new Vector2(0, 68);
            status = Label(game, "Drag from the shuttle to its matching bay.\nRelease to launch; momentum will carry it.", 23, -360, 100, ColorTokens.Muted);
            var pause = panels[GameScreen.Pause].transform;
            Label(pause, "HOLDING PATTERN", 40, 340, 70, ColorTokens.Text);
            Button(pause, "Resume", 170, () => OnNavigate?.Invoke(GameScreen.Gameplay));
            Button(pause, "Restart delivery", 75, () => OnAdvance?.Invoke(false), false);
            Button(pause, "Settings", -20, () => OnNavigate?.Invoke(GameScreen.Settings), false);
            Button(pause, "Privacy", -115, () => OnNavigate?.Invoke(GameScreen.Privacy), false);
            Button(pause, "Level select", -210, () => OnNavigate?.Invoke(GameScreen.LevelSelect), false);
            Button(pause, "Main menu", -305, () => OnNavigate?.Invoke(GameScreen.MainMenu), false);
            var win = panels[GameScreen.Win].transform;
            winTitle = Label(win, "CARGO SECURED", 40, 300, 90, ColorTokens.Text);
            results = Label(win, "", 28, 140, 180, ColorTokens.Accent);
            next = Button(win, "Next delivery", -70, () => OnAdvance?.Invoke(true));
            Button(win, "Level select", -175, () => OnNavigate?.Invoke(GameScreen.LevelSelect), false);
            var settings = panels[GameScreen.Settings].transform;
            Label(settings, "SETTINGS", 44, 300, 70, ColorTokens.Text);
            var sound = Button(settings, "Sound: on", 150, () => OnToggleSound?.Invoke(), false);
            soundLabel = sound.GetComponentInChildren<TMP_Text>();
            musicLabel = Button(settings,"Music: on",50,()=>OnToggleMusic?.Invoke(),false).GetComponentInChildren<TMP_Text>();
            hapticsLabel = Button(settings,"Haptics: on",-50,()=>OnToggleHaptics?.Invoke(),false).GetComponentInChildren<TMP_Text>();
            Button(settings,"Reset progress",-170,()=>OnNavigate?.Invoke(GameScreen.Reset),false);
            Button(settings,"Back",-350,()=>OnNavigate?.Invoke(GameScreen.Settings),false);
            var reset = panels[GameScreen.Reset].transform;
            Label(reset,"RESET PROGRESS?",36,240,100,ColorTokens.Text);
            Label(reset,"This removes local stars and unlocks.\nIt cannot be undone.",25,70,120,ColorTokens.Muted);
            Button(reset,"Keep progress",-100,()=>OnNavigate?.Invoke(GameScreen.Settings));
            Button(reset,"Reset local save",-210,()=>OnResetSave?.Invoke(),false);
            var fail = panels[GameScreen.Fail].transform;
            Label(fail,"LOST IN ORBIT",42,240,90,ColorTokens.Text);
            Label(fail,"The station can wait. Take another shot.",24,100,100,ColorTokens.Muted);
            Button(fail,"Try again",-70,()=>OnAdvance?.Invoke(false));
            Button(fail,"Level select",-180,()=>OnNavigate?.Invoke(GameScreen.LevelSelect),false);
            var privacy = panels[GameScreen.Privacy].transform;
            Label(privacy, "PRIVACY", 44, 350, 70, ColorTokens.Text);
            Label(privacy, "Zero-G Cargo stores progress and preferences on this device only.\n\nIt contains no ads, analytics, accounts or network services.\n\nUninstalling the app removes local data. No information is sent to a server. Local files are validated and backed up; offline progress is not tamper-proof.", 25, 40, 460, ColorTokens.Muted);
            Button(privacy, "Back", -300, () => OnNavigate?.Invoke(GameScreen.Privacy), false);
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
        private static TMP_Text Label(Transform parent, string value, int size, float y, float height, Color color)
        {
            var obj = new GameObject("Text", typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0.05f, 0.5f); rect.anchorMax = new Vector2(0.95f, 0.5f);
            rect.sizeDelta = new Vector2(0, height); rect.anchoredPosition = new Vector2(0, y);
            var text = obj.AddComponent<TextMeshProUGUI>(); text.font = (size >= 32 ? headingFont : bodyFont) ?? TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center; text.color = color; text.raycastTarget = false; return text;
        }
        private static Button Button(Transform parent, string text, float y, UnityAction action, bool primary = true)
        {
            var obj = new GameObject("Button " + text, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(520, 72); rect.anchoredPosition = new Vector2(0, y);
            obj.GetComponent<Image>().sprite = Rounded(); obj.GetComponent<Image>().type = Image.Type.Sliced;
            obj.GetComponent<Image>().color = primary ? (Color)ColorTokens.Accent : ColorTokens.Panel;
            var button = obj.GetComponent<Button>(); button.onClick.AddListener(action);
            Label(obj.transform, text, 25, 0, 68, primary ? (Color)ColorTokens.Background : ColorTokens.Accent);
            return button;
        }
        /// <summary>Applies native screen cutouts plus design-system padding.</summary>
        public void UpdateSafeArea()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect area = Screen.safeArea;
            if (area == lastSafeArea && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight) return;
            lastSafeArea = area; lastScreenWidth = Screen.width; lastScreenHeight = Screen.height;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            safeArea.offsetMin = new Vector2(24, 24); safeArea.offsetMax = new Vector2(-24, -24);
        }
        /// <summary>Shows exactly one prebuilt screen.</summary>
        public void Show(GameScreen screen)
        { backdrop.SetActive(screen != GameScreen.Gameplay); toast.text = ""; foreach (var entry in panels) entry.Value.SetActive(entry.Key == screen); }
        /// <summary>Refreshes progress and locks only on navigation.</summary>
        public void Refresh(SaveData save, LevelDefinition[] levels)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                bool unlocked = i < save.highestUnlockedLevel; levelButtons[i].interactable = unlocked;
                levelLabels[i].text = unlocked ? (i + 1).ToString("00") + "\n" + new string('*', save.starsPerLevel[i]) : (i + 1).ToString("00") + "\nLOCK";
            }
            int stars = 0, cleared = 0;
            for (int i = 0; i < save.starsPerLevel.Length; i++) { stars += save.starsPerLevel[i]; if (save.starsPerLevel[i] > 0) cleared++; }
            campaignProgress.text = cleared + " / 50 SECTORS CLEARED\n" + stars + " / 150 STARS";
            soundLabel.text = "Sound: " + (save.settings.soundOn ? "on" : "off");
            musicLabel.text = "Music: " + (save.settings.musicOn ? "on" : "off");
            hapticsLabel.text = "Haptics: " + (save.settings.hapticsOn ? "on" : "off");
        }
        /// <summary>Sets level instructions without per-frame string creation.</summary>
        public void SetLevel(LevelDefinition level)
        {
            objectives.text = "SECTOR " + level.id.ToString("00") + "  /  " + level.title + "\n0 / " + level.cargo.Length + " CARGO DELIVERED";
            SetStatus(level.id <= 2 ? "Drag from a shuttle to its matching bay.\nRelease to launch. Leave room for momentum." : level.id >= 35 ? "Deliver in order to unlock the smaller bays.\nCounter the fields; use wide arcs." : level.id >= 31 ? "Gravity bends your route. Draw a counter-arc." : level.id >= 11 ? "Red barriers repel. Match every cargo before time runs out." : "Route around the meteorite. Short routes can be continued.");
            timer.gameObject.SetActive(level.timeLimit > 0); timer.Fill = 0;
        }
        /// <summary>Refreshes delivery objectives at state changes.</summary>
        public void SetProgress(CampaignSimulation simulation)
        { objectives.text = "SECTOR " + simulation.Level.id.ToString("00") + "  /  " + simulation.Level.title + "\n" + simulation.Delivered + " / " + simulation.Level.cargo.Length + " CARGO DELIVERED"; }
        /// <summary>Updates the non-numeric timer arc.</summary>
        public void SetTimer(float fraction) { timer.Fill = fraction; }
        /// <summary>Updates tutorial feedback at state changes.</summary>
        public void SetStatus(string message) { status.text = message; }
        /// <summary>Shows earned stars and the replayable end-of-prototype state.</summary>
        public void SetResults(int stars, int attempts, bool hasNext, bool saved)
        {
            winTitle.text = hasNext ? "CARGO SECURED" : "STATION RESTORED";
            results.text = new string('*', stars) + "\n" + attempts + " collisions\n" + (saved ? "Progress saved on this device" : "Progress in memory; local save unavailable");
            next.gameObject.SetActive(hasNext);
        }
        private static Sprite Rounded()
        {
            if (roundedSprite != null) return roundedSprite;
            const int size = 32; var texture = new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels = new Color32[size*size];
            for (int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float dx = Mathf.Max(8-x,Mathf.Max(0,x-23)), dy = Mathf.Max(8-y,Mathf.Max(0,y-23));
                pixels[y*size+x] = dx*dx+dy*dy<=64 ? new Color32(255,255,255,255) : new Color32(255,255,255,0);
            }
            texture.SetPixels32(pixels); texture.Apply(); roundedSprite = Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(9,9,9,9)); return roundedSprite;
        }
        /// <summary>Shows a brief navigation hint.</summary>
        public void Toast(string message) { toast.text = message; }
        /// <summary>Releases the persistent UI root.</summary>
        public void Dispose()
        {
            UnityEngine.Object.Destroy(root);
            if (roundedSprite != null) { UnityEngine.Object.Destroy(roundedSprite.texture); UnityEngine.Object.Destroy(roundedSprite); roundedSprite = null; }
        }
    }
}
