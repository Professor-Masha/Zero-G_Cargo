using System.IO;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZeroGCargo.Editor
{
    /// <summary>Repeatable Unity-only renderer, font and Android project preparation.</summary>
    public static class ProjectSetup
    {
        private const string RendererPath = "Assets/_Project/Resources/Config/Station3DRenderer.asset";
        private const string MaterialPath = "Assets/_Project/Resources/Config/Station3DLineMaterial.mat";
        private const string PipelinePath = "Assets/_Project/Resources/Config/Station3DPipeline.asset";
        private static readonly string[] Scenes = {
            "Assets/_Project/Scenes/Boot/Boot.unity",
            "Assets/_Project/Scenes/MainMenu/MainMenu.unity",
            "Assets/_Project/Scenes/LevelSelect/LevelSelect.unity",
            "Assets/_Project/Scenes/Gameplay/Gameplay.unity"
        };
        [InitializeOnLoadMethod]
        private static void FirstImport()
        {
            EditorApplication.delayCall += () =>
            {
                if (!Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode && (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath) == null || AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) == null || Resources.Load<TMP_Settings>("TMP Settings") == null || Resources.Load<TMP_FontAsset>("Fonts/Inter") == null || Resources.Load<TMP_FontAsset>("Fonts/Orbitron") == null)) Prepare();
            };
        }
        /// <summary>Creates missing URP assets and imports TMP essentials; safe to rerun.</summary>
        [MenuItem("Zero-G Cargo/Prepare game")]
        [MenuItem("Zero-G Cargo/Prepare prototype")]
        public static void Prepare()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) throw new System.InvalidOperationException("URP unlit shader is unavailable. Wait for Package Manager to finish.");
                var line = new Material(shader);
                line.SetFloat("_Surface",1); line.SetFloat("_Blend",0);
                line.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); line.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                line.SetFloat("_ZWrite",0); line.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                line.SetOverrideTag("RenderType","Transparent"); line.renderQueue = (int)RenderQueue.Transparent;
                AssetDatabase.CreateAsset(line,MaterialPath);
            }
            const string surfacePath = "Assets/_Project/Resources/Config/StationSurfaceMaterial.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(surfacePath) == null)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) throw new System.InvalidOperationException("URP Lit shader unavailable");
                AssetDatabase.CreateAsset(new Material(lit),surfacePath);
            }
            PlayerSettings.Android.forceInternetPermission = false;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                // Unity 6 ships TMP inside uGUI; use TMP's own package locator.
                string package = Path.Combine(TMP_EditorUtility.packageFullPath, "Package Resources/TMP Essential Resources.unitypackage");
                if (!File.Exists(package)) throw new FileNotFoundException("TextMeshPro essentials package not found in the installed uGUI package", package);
                UnityEditor.AssetPackage.Package.Import(package, false);
            }
            PrepareFont("Orbitron"); PrepareFont("Inter");
            var sceneSettings = new EditorBuildSettingsScene[Scenes.Length];
            for (int i = 0; i < Scenes.Length; i++) sceneSettings[i] = new EditorBuildSettingsScene(Scenes[i], true);
            EditorBuildSettings.scenes = sceneSettings;
            PlayerSettings.companyName = "Professor Masha"; PlayerSettings.productName = "Zero-G Cargo";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.professormasha.zerogcargo");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            ConfigureLegacyInput();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("Game prepared. Open Boot and press Play. Android requires Hub's Android Build Support, SDK/NDK and OpenJDK.");
        }
        private static void PrepareFont(string name)
        {
            string path = "Assets/_Project/Resources/Fonts/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null) return;
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Resources/Fonts/" + name + ".ttf");
            if (font == null) throw new System.InvalidOperationException("Missing font: " + name);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font);
            if (asset == null) { Debug.LogWarning("Could not create TMP font " + name + "; the UI will use the bundled default font."); return; }
            asset.name = name; AssetDatabase.CreateAsset(asset,path);
            if (asset.material != null) AssetDatabase.AddObjectToAsset(asset.material,asset);
            foreach (Texture2D texture in asset.atlasTextures) if (texture != null) AssetDatabase.AddObjectToAsset(texture,asset);
        }
        private static void ConfigureLegacyInput()
        {
            // PathDrawer and StandaloneInputModule use the legacy Input Manager.
            // Unity's Player Settings inspector stores this as 0=legacy, 1=new, 2=both.
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0) throw new System.InvalidOperationException("Unity Player Settings are unavailable; reopen the project and run preparation again.");
            var settings = new SerializedObject(assets[0]);
            SerializedProperty input = settings.FindProperty("activeInputHandler");
            if (input == null) throw new System.InvalidOperationException("Cannot configure Active Input Handling. Set it to Input Manager (Old) in Player Settings.");
            if (input.intValue == 0) return;
            input.intValue = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Debug.LogWarning("Active Input Handling changed to Input Manager (Old). Restart Unity before pressing Play or building.");
        }
        private static void EnsureBuildReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play mode before building the APK.");
            if (BuildPipeline.isBuildingPlayer)
                throw new System.InvalidOperationException("A player build is already running. Wait for it to finish.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new System.InvalidOperationException("Switch to Android in File > Build Profiles, then wait for imports and compilation to finish.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || ShaderUtil.anythingCompiling)
                throw new System.InvalidOperationException("Unity is still importing assets or compiling scripts/shaders. Wait for Background Tasks to finish before building. If the Console reports Out of memory, close other applications and restart Unity first.");
            if (EditorUtility.scriptCompilationFailed)
                throw new System.InvalidOperationException("Unity reports script compilation errors. Resolve the first compiler error in the Console before building the APK.");
        }
        /// <summary>Builds an installable test APK without development/debug-player features.</summary>
        [MenuItem("Zero-G Cargo/Build phone test APK")]
        public static void BuildPhoneTest()
        {
            EnsureBuildReady();
            if (Resources.Load<TMP_Settings>("TMP Settings") == null) throw new System.InvalidOperationException("Open Unity and run Prepare game once before using the APK build command.");
            Prepare(); Directory.CreateDirectory("Builds"); EditorUserBuildSettings.buildAppBundle = false;
            EnsureBuildReady();
            PlayerSettings.Android.useCustomKeystore = false;
            BuildReport report = BuildPipeline.BuildPlayer(Scenes,"Builds/ZeroGCargo-phone-test.apk",BuildTarget.Android,BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Phone APK build failed: " + report.summary.result);
            Debug.Log("APK ready: " + Path.GetFullPath("Builds/ZeroGCargo-phone-test.apk"));
        }
        /// <summary>Builds a development APK using Android debug signing for a local device smoke test.</summary>
        [MenuItem("Zero-G Cargo/Build Android development APK")]
        public static void BuildAndroid()
        {
            EnsureBuildReady();
            Prepare(); Directory.CreateDirectory("Builds"); EditorUserBuildSettings.buildAppBundle = false;
            EnsureBuildReady();
            BuildReport report = BuildPipeline.BuildPlayer(Scenes, "Builds/ZeroGCargo-dev.apk", BuildTarget.Android, BuildOptions.Development);
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Android development build failed: " + report.summary.result);
        }
    }
}
