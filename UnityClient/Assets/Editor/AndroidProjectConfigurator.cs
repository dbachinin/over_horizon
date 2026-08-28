#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TransparentEarth.App;

namespace TransparentEarth.Editor
{
    [InitializeOnLoad]
    public static class AndroidProjectConfigurator
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const int RequiredTargetApiLevel = 36;

        static AndroidProjectConfigurator() => EditorApplication.delayCall += EnsureProject;

        [MenuItem("OverHorizon/Prepare Android Project")]
        public static void EnsureProject()
        {
            PlayerSettings.productName = AppIdentity.ProductName;
            PlayerSettings.companyName = AppIdentity.ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppIdentity.AndroidPackageName);
            if (PlayerSettings.bundleVersion == "1.0")
                PlayerSettings.bundleVersion = AppIdentity.DefaultVersionName;
            if (PlayerSettings.Android.bundleVersionCode < AppIdentity.DefaultVersionCode)
                PlayerSettings.Android.bundleVersionCode = AppIdentity.DefaultVersionCode;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)RequiredTargetApiLevel;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.X86_64;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PreserveRuntimeShaders();
            ApplyAppIcon();
            ApplyGoogleMobileAdsSettings();

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private const string AppIconPath = "Assets/Branding/AppIcon.png";
        private const string AdMobConfigPath = "Assets/Resources/AdMobConfig.json";

        private static void ApplyGoogleMobileAdsSettings()
        {
            var configAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AdMobConfigPath);
            var config = configAsset == null ? null : JsonUtility.FromJson<AdMobBuildConfig>(configAsset.text);
            if (config == null || string.IsNullOrWhiteSpace(config.androidAppId))
            {
                Debug.LogWarning("AdMobConfig.json is missing an Android app ID; ads will not build.");
                return;
            }

            var settingsType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings"))
                .FirstOrDefault(type => type != null);
            var loadInstance = settingsType?.GetMethod("LoadInstance",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (loadInstance?.Invoke(null, null) is not ScriptableObject settings)
            {
                Debug.LogWarning("Google Mobile Ads settings API is unavailable.");
                return;
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("adMobAndroidAppId").stringValue = config.androidAppId.Trim();
            serialized.FindProperty("adMobIOSAppId").stringValue = config.iosAppId?.Trim() ?? string.Empty;
            serialized.FindProperty("enableGradleBuildPreProcessor").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

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

        [MenuItem("OverHorizon/Build Google Play AAB")]
        public static void BuildGooglePlayAab()
        {
            EnsureProject();
            ValidatePlayReleaseConfiguration();

            var previousBundle = EditorUserBuildSettings.buildAppBundle;
            var previousArchitectures = PlayerSettings.Android.targetArchitectures;
            var previousStripEngineCode = PlayerSettings.stripEngineCode;
            var previousStripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Android);
            var previousUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            var previousKeystoreName = PlayerSettings.Android.keystoreName;
            var previousKeystorePass = PlayerSettings.Android.keystorePass;
            var previousAliasName = PlayerSettings.Android.keyaliasName;
            var previousAliasPass = PlayerSettings.Android.keyaliasPass;

            try
            {
                ApplyReleaseVersion();
                ApplyUploadSigning();
                EditorUserBuildSettings.buildAppBundle = true;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);

                Directory.CreateDirectory("Builds/GooglePlay");
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = "Builds/GooglePlay/OverHorizon.aab",
                    target = BuildTarget.Android,
                    options = BuildOptions.CompressWithLz4HC
                };
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException($"Google Play AAB build failed: {report.summary.result}");
                Debug.Log($"Google Play AAB built: {report.summary.outputPath} " +
                          $"({report.summary.totalSize} bytes)");
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousBundle;
                PlayerSettings.Android.targetArchitectures = previousArchitectures;
                PlayerSettings.stripEngineCode = previousStripEngineCode;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, previousStripping);
                PlayerSettings.Android.useCustomKeystore = previousUseCustomKeystore;
                PlayerSettings.Android.keystoreName = previousKeystoreName;
                PlayerSettings.Android.keystorePass = previousKeystorePass;
                PlayerSettings.Android.keyaliasName = previousAliasName;
                PlayerSettings.Android.keyaliasPass = previousAliasPass;
            }
        }

        [MenuItem("OverHorizon/Validate Google Play Release")]
        public static void ValidatePlayReleaseConfiguration()
        {
            var errors = new System.Collections.Generic.List<string>();
            var configAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(AdMobConfigPath);
            var ads = configAsset == null ? null : JsonUtility.FromJson<AdMobBuildConfig>(configAsset.text);
            if (ads == null)
                errors.Add("Assets/Resources/AdMobConfig.json is missing or invalid.");
            else
            {
                if (ads.useTestAds) errors.Add("AdMob useTestAds must be false for a Play release.");
                if (IsGoogleTestId(ads.androidAppId) || IsGoogleTestId(ads.androidBannerAdUnitId))
                    errors.Add("Replace Google's test AdMob IDs with OverHorizon production IDs.");
            }

            RequireEnvironment("OVERHORIZON_KEYSTORE_PATH", errors);
            RequireEnvironment("OVERHORIZON_KEYSTORE_PASS", errors);
            RequireEnvironment("OVERHORIZON_KEY_ALIAS", errors);
            RequireEnvironment("OVERHORIZON_KEY_ALIAS_PASS", errors);

            var keystorePath = Environment.GetEnvironmentVariable("OVERHORIZON_KEYSTORE_PATH");
            if (!string.IsNullOrWhiteSpace(keystorePath) && !File.Exists(keystorePath))
                errors.Add($"Upload keystore does not exist: {keystorePath}");

            if (PlayerSettings.Android.targetSdkVersion != (AndroidSdkVersions)RequiredTargetApiLevel)
                errors.Add($"Android target API must be {RequiredTargetApiLevel}.");
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) !=
                AppIdentity.AndroidPackageName)
                errors.Add("Android package name does not match AppIdentity.AndroidPackageName.");

            if (errors.Count > 0)
                throw new BuildFailedException("Google Play release is not ready:\n- " +
                                               string.Join("\n- ", errors));

            Debug.Log("Google Play release configuration is valid.");
        }

        private static void ApplyReleaseVersion()
        {
            var versionName = Environment.GetEnvironmentVariable("OVERHORIZON_VERSION_NAME");
            var versionCodeText = Environment.GetEnvironmentVariable("OVERHORIZON_VERSION_CODE");
            if (!string.IsNullOrWhiteSpace(versionName)) PlayerSettings.bundleVersion = versionName.Trim();
            if (!string.IsNullOrWhiteSpace(versionCodeText) &&
                int.TryParse(versionCodeText, out var versionCode) && versionCode > 0)
                PlayerSettings.Android.bundleVersionCode = versionCode;
        }

        private static void ApplyUploadSigning()
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Environment.GetEnvironmentVariable("OVERHORIZON_KEYSTORE_PATH");
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("OVERHORIZON_KEYSTORE_PASS");
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("OVERHORIZON_KEY_ALIAS");
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("OVERHORIZON_KEY_ALIAS_PASS");
        }

        private static void RequireEnvironment(string name,
            System.Collections.Generic.ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
                errors.Add($"Environment variable {name} is required.");
        }

        private static bool IsGoogleTestId(string id) =>
            string.IsNullOrWhiteSpace(id) || id.Contains("3940256099942544");

        [Serializable]
        private sealed class AdMobBuildConfig
        {
            public bool useTestAds;
            public string androidAppId;
            public string iosAppId;
            public string androidBannerAdUnitId;
        }
    }
}
#endif
