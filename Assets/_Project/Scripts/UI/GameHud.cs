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
        private readonly RectTransform safeArea;
        private readonly Dictionary<GameScreen, GameObject> panels = new Dictionary<GameScreen, GameObject>();
        private readonly TMP_Text[] levelLabels;
        private readonly Button[] levelButtons;
        private readonly TMP_Text status;
        private readonly TMP_Text objectives;
        private readonly TMP_Text results;
        private readonly TMP_Text soundLabel;
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
        /// <summary>Creates UI once using the bundled TMP default font.</summary>
        public GameHud(LevelData[] levels)
        {
            root = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            // Keep the full portrait layout visible even in a wide Editor Game view.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            safeArea = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>(); safeArea.SetParent(root.transform, false);
            UpdateSafeArea();
            foreach (GameScreen screen in Enum.GetValues(typeof(GameScreen)))
            {
                var panel = new GameObject(screen.ToString(), typeof(RectTransform)); panel.transform.SetParent(safeArea, false);
                Stretch(panel.GetComponent<RectTransform>());
                if (screen != GameScreen.Gameplay)
                { var bg = panel.AddComponent<Image>(); bg.color = ColorTokens.Background; }
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
            Label(menu, "CORE PROTOTYPE  /  3 TRAINING DELIVERIES", 18, -470, 50, ColorTokens.Muted);
            var select = panels[GameScreen.LevelSelect].transform;
            Label(select, "TRAINING DECK", 40, 420, 70, ColorTokens.Text);
            Label(select, "Learn the drift. Deliver the cargo.", 24, 345, 60, ColorTokens.Muted);
            levelLabels = new TMP_Text[levels.Length]; levelButtons = new Button[levels.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                int index = i;
                levelButtons[i] = Button(select, "", 180 - i * 110, () => OnStartLevel?.Invoke(index + 1), false);
                levelLabels[i] = levelButtons[i].GetComponentInChildren<TMP_Text>();
            }
            Button(select, "Main menu", -300, () => OnNavigate?.Invoke(GameScreen.MainMenu), false);
            var game = panels[GameScreen.Gameplay].transform;
            objectives = Label(game, "", 25, 490, 110, ColorTokens.Text);
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
            Label(win, "CARGO SECURED", 44, 300, 70, ColorTokens.Text);
            results = Label(win, "", 28, 140, 180, ColorTokens.Accent);
            next = Button(win, "Next delivery", -70, () => OnAdvance?.Invoke(true));
            Button(win, "Level select", -175, () => OnNavigate?.Invoke(GameScreen.LevelSelect), false);
            var settings = panels[GameScreen.Settings].transform;
            Label(settings, "SETTINGS", 44, 300, 70, ColorTokens.Text);
            var sound = Button(settings, "Sound: on", 120, () => OnToggleSound?.Invoke(), false);
            soundLabel = sound.GetComponentInChildren<TMP_Text>();
            Label(settings, "Music and haptics arrive in a later milestone.\nThis prototype is fully offline. No ads or telemetry.", 23, -30, 160, ColorTokens.Muted);
            Button(settings, "Back", -240, () => OnNavigate?.Invoke(GameScreen.Settings), false);
            var privacy = panels[GameScreen.Privacy].transform;
            Label(privacy, "PRIVACY", 44, 350, 70, ColorTokens.Text);
            Label(privacy, "This development prototype stores progress and sound preferences on this device only.\n\nIt contains no ads, analytics, accounts or network services.\n\nUninstalling the app removes local data. Before release, the policy will be updated for any SDKs added.", 25, 40, 460, ColorTokens.Muted);
            Button(privacy, "Back", -300, () => OnNavigate?.Invoke(GameScreen.Privacy), false);
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
        private static TMP_Text Label(Transform parent, string value, int size, float y, float height, Color color)
        {
            var obj = new GameObject("Text", typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0.05f, 0.5f); rect.anchorMax = new Vector2(0.95f, 0.5f);
            rect.sizeDelta = new Vector2(0, height); rect.anchoredPosition = new Vector2(0, y);
            var text = obj.AddComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center; text.color = color; text.raycastTarget = false; return text;
        }
        private static Button Button(Transform parent, string text, float y, UnityAction action, bool primary = true)
        {
            var obj = new GameObject("Button " + text, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(520, 72); rect.anchoredPosition = new Vector2(0, y);
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
        { toast.text = ""; foreach (var entry in panels) entry.Value.SetActive(entry.Key == screen); }
        /// <summary>Refreshes progress and locks only on navigation.</summary>
        public void Refresh(SaveData save, LevelData[] levels)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                bool unlocked = i < save.highestUnlockedLevel; levelButtons[i].interactable = unlocked;
                levelLabels[i].text = unlocked ? (i + 1) + "  /  " + levels[i].Title + "   " + new string('*', save.starsPerLevel[i]) : (i + 1) + "  /  LOCKED";
            }
            soundLabel.text = "Sound: " + (save.settings.soundOn ? "on" : "off");
        }
        /// <summary>Sets level instructions without per-frame string creation.</summary>
        public void SetLevel(LevelData level) { objectives.text = "DELIVERY " + level.LevelId.ToString("00") + "  /  " + level.Title + "\n1 CARGO  /  " + level.Cargo.ToString().ToUpperInvariant(); SetStatus("Drag from the shuttle to the matching bay.\nA short route? Pick it up and continue."); }
        /// <summary>Updates tutorial feedback at state changes.</summary>
        public void SetStatus(string message) { status.text = message; }
        /// <summary>Shows earned stars and the replayable end-of-prototype state.</summary>
        public void SetResults(int stars, int attempts, bool hasNext, bool saved)
        {
            results.text = new string('*', stars) + "\n" + attempts + " collisions\n" + (saved ? "Progress saved on this device" : "Progress in memory; local save unavailable");
            next.gameObject.SetActive(hasNext);
        }
        /// <summary>Shows a brief navigation hint.</summary>
        public void Toast(string message) { toast.text = message; }
        /// <summary>Releases the persistent UI root.</summary>
        public void Dispose() => UnityEngine.Object.Destroy(root);
    }
}
