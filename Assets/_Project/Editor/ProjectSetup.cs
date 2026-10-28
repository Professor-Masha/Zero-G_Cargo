using System.IO;
using TMPro;
using UnityEditor;
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
        private const string RendererPath = "Assets/_Project/Resources/Config/StationRenderer.asset";
        private const string MaterialPath = "Assets/_Project/Resources/Config/StationLineMaterial.mat";
        private const string PipelinePath = "Assets/_Project/Resources/Config/StationPipeline.asset";
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
                if (!Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode && (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath) == null || AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) == null || Resources.Load<TMP_Settings>("TMP Settings") == null)) Prepare();
            };
        }
        /// <summary>Creates missing URP assets and imports TMP essentials; safe to rerun.</summary>
        [MenuItem("Zero-G Cargo/Prepare prototype")]
        public static void Prepare()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
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
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) throw new System.InvalidOperationException("URP 2D unlit shader is unavailable. Wait for Package Manager to finish.");
                AssetDatabase.CreateAsset(new Material(shader), MaterialPath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                string package = Path.GetFullPath("Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage");
                // Package paths may be virtual; resolve the installed package's real location.
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
                if (info != null) package = Path.Combine(info.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage");
                if (!File.Exists(package)) throw new FileNotFoundException("TextMeshPro essentials package not found", package);
                AssetDatabase.ImportPackage(package, false);
            }
            var sceneSettings = new EditorBuildSettingsScene[Scenes.Length];
            for (int i = 0; i < Scenes.Length; i++) sceneSettings[i] = new EditorBuildSettingsScene(Scenes[i], true);
            EditorBuildSettings.scenes = sceneSettings;
            PlayerSettings.companyName = "Professor Masha"; PlayerSettings.productName = "Zero-G Cargo";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.professormasha.zerogcargo");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("Prototype prepared. Open Boot and press Play. Android requires Hub's Android Build Support, SDK/NDK and OpenJDK.");
        }
        /// <summary>Builds a development APK using Android debug signing for a local device smoke test.</summary>
        [MenuItem("Zero-G Cargo/Build Android development APK")]
        public static void BuildAndroid()
        {
            Prepare(); Directory.CreateDirectory("Builds"); EditorUserBuildSettings.buildAppBundle = false;
            BuildReport report = BuildPipeline.BuildPlayer(Scenes, "Builds/ZeroGCargo-dev.apk", BuildTarget.Android, BuildOptions.Development);
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Android development build failed: " + report.summary.result);
        }
    }
}
