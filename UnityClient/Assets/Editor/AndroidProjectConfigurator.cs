#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TransparentEarth.Editor
{
    [InitializeOnLoad]
    public static class AndroidProjectConfigurator
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        static AndroidProjectConfigurator() => EditorApplication.delayCall += EnsureProject;

        [MenuItem("OverHorizon/Prepare Android Project")]
        public static void EnsureProject()
        {
            PlayerSettings.productName = "OverHorizon";
            PlayerSettings.companyName = "OverHorizon";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.transparentearth.unity");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PreserveRuntimeShaders();
            ApplyAppIcon();

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private const string AppIconPath = "Assets/Branding/AppIcon.png";

        private static void ApplyAppIcon()
        {
            if (AssetImporter.GetAtPath(AppIconPath) is TextureImporter importer &&
                (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled))
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null)
            {
                Debug.LogWarning($"App icon not found at {AppIconPath}; keeping the default icon.");
                return;
            }

            var target = NamedBuildTarget.Android;

            // Legacy square icon slot.
            try
            {
                var count = PlayerSettings.GetIconSizes(target, IconKind.Application).Length;
                if (count > 0)
                    PlayerSettings.SetIcons(target, Enumerable.Repeat(icon, count).ToArray(), IconKind.Application);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Could not set application icons: {exception.Message}");
            }

            // Modern Android platform icons (legacy, round and adaptive layers).
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(target))
            {
                var icons = PlayerSettings.GetPlatformIcons(target, kind);
                foreach (var platformIcon in icons)
                {
                    var layers = Mathf.Max(1, platformIcon.maxLayerCount);
                    platformIcon.SetTextures(Enumerable.Repeat(icon, layers).ToArray());
                }
                PlayerSettings.SetPlatformIcons(target, kind, icons);
            }
        }

        private static void PreserveRuntimeShaders()
        {
            var required = new[]
            {
                Shader.Find("TransparentEarth/AtmosphericGrid"),
                Shader.Find("TransparentEarth/GoldenInterior"),
                Shader.Find("TransparentEarth/HorizonHaze"),
                Shader.Find("TransparentEarth/CameraBackground"),
                Shader.Find("Sprites/Default")
            }.Where(shader => shader != null).ToArray();
            var settingsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
            if (settingsAsset == null) return;
            var serializedSettings = new SerializedObject(settingsAsset);
            var shaders = serializedSettings.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null) return;
            foreach (var shader in required)
            {
                var alreadyIncluded = Enumerable.Range(0, shaders.arraySize)
                    .Any(index => shaders.GetArrayElementAtIndex(index).objectReferenceValue == shader);
                if (alreadyIncluded) continue;
                shaders.InsertArrayElementAtIndex(shaders.arraySize);
                shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader;
            }
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void BuildAndroid()
        {
            EnsureProject();
            Directory.CreateDirectory("Builds/Android");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Android/OverHorizon.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android build failed: {report.summary.result}");
            Debug.Log($"Android APK built: {report.summary.outputPath} ({report.summary.totalSize} bytes)");
        }
    }
}
#endif
