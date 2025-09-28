#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GW.EditorTools
{
    /// <summary>
    /// Ensures WebGL build settings follow the performance budget defined in the production spec.
    /// </summary>
    [InitializeOnLoad]
    public static class WebGLBuildConfigurator
    {
        private const string AppliedKey = "GW_WebGLBuildConfigurator_LastVersion";
        private const int Version = 1;

        static WebGLBuildConfigurator()
        {
            EditorApplication.delayCall += ApplyIfNeeded;
        }

        [MenuItem("GW/Apply WebGL Build Settings", priority = 0)]
        public static void Apply()
        {
            ApplyInternal(force: true);
        }

        private static void ApplyIfNeeded()
        {
            var storedVersion = EditorPrefs.GetInt(AppliedKey, -1);
            if (storedVersion == Version)
            {
                return;
            }

            ApplyInternal(force: false);
        }

        private static void ApplyInternal(bool force)
        {
            ConfigurePlayerSettings();
            ConfigureEditorBuildSettings();

            EditorPrefs.SetInt(AppliedKey, Version);

            if (force)
            {
                Debug.Log("GW WebGL build settings applied.");
            }
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(
                BuildTargetGroup.WebGL,
#if UNITY_2021_2_OR_NEWER
                ApiCompatibilityLevel.NETStandard
#else
                ApiCompatibilityLevel.NET_4_6
#endif
            );
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.WebGL.memorySize = 384;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
#else
            PlayerSettings.WebGL.debugSymbols = false;
#endif
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.gcIncremental = true;
        }

        private static void ConfigureEditorBuildSettings()
        {
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;
#if UNITY_2021_2_OR_NEWER
            EditorUserBuildSettings.webGLCompressionFormat = WebGLCompressionFormat.Brotli;
            EditorUserBuildSettings.webGLLinkerTarget = WebGLLinkerTarget.Wasm;
#endif
        }
    }
}
#endif
