#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Explicit Editor-only diagnostic. Arming survives the Play Mode domain reload.
// Interrupted runs remain armed; only a completed Home capture consumes the request.
// No runtime component or asset changes; subscriptions end after the first Home build.
[InitializeOnLoad]
public static class GhaOneShotAvatarProfile
{
    private const string ArmedKey = "GHA.OneShotAvatarProfile.Armed";
    private const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type capture;
    private static string trace;
    private static double deadline;
    private static int readyFrame = -1;
    private static bool active;
    public static bool IsArmed => SessionState.GetBool(ArmedKey, false);

    static GhaOneShotAvatarProfile()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
    }

    public static string Arm()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Arm while stopped and compilation is complete.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LoginScene")
            throw new InvalidOperationException("Start the capture from LoginScene.");
        if (UnityEngine.Profiling.Profiler.enabled || UnityEditorInternal.ProfilerDriver.deepProfiling)
            throw new InvalidOperationException("Another profiler or deep profiling is enabled.");
        CaptureType();
        SessionState.SetBool(ArmedKey, true);
        return "ARMED: next Play from LoginScene; retries interrupted runs until first Home capture succeeds.";
    }

    private static Type CaptureType()
    {
        return capture ?? (capture = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("GHA.AvatarSuite.AvatarLoadProfilerCapture"))
            .FirstOrDefault(t => t != null)
            ?? throw new InvalidOperationException("GHA profiler capture helper is unavailable."));
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && IsArmed)
        {
            try
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LoginScene")
                    throw new InvalidOperationException("Capture remains armed; start from LoginScene.");
                var helper = CaptureType();
                if (UnityEngine.Profiling.Profiler.enabled ||
                    (bool)helper.GetField("_active", Flags).GetValue(null) ||
                    UnityEditorInternal.ProfilerDriver.deepProfiling)
                    throw new InvalidOperationException("Another capture is active; leaving it untouched.");
                trace = "owner-login-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                helper.GetMethod("Begin", Flags).Invoke(null, new object[] { trace });
                active = (bool)helper.GetField("_active", Flags).GetValue(null);
                if (!active) throw new InvalidOperationException("Profiler capture did not start.");
                deadline = EditorApplication.timeSinceStartup + 180;
                readyFrame = -1;
                Application.logMessageReceived += OnLog;
                EditorApplication.update += Tick;
                Debug.Log("[GHA ONE SHOT] Recording first Home load; raw capture in Library/GHAProfiles.");
            }
            catch (Exception e)
            {
                Finish("FAILED");
                Debug.LogError("[GHA ONE SHOT] " + e);
            }
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
            Finish("PLAY_MODE_ENDED");
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (message.StartsWith("[GHA LOAD TIMING]") && message.Contains("phase=FINISH host=home ") &&
            message.Contains("result=SUCCESS")) readyFrame = Time.frameCount;
    }

    private static void Tick()
    {
        if (readyFrame >= 0 && Time.frameCount >= readyFrame + 3) Finish("SUCCESS");
        else if (EditorApplication.timeSinceStartup >= deadline) Finish("TIMEOUT");
    }

    private static void BeforeReload() => Finish("ASSEMBLY_RELOAD");

    private static void Finish(string result)
    {
        Application.logMessageReceived -= OnLog;
        EditorApplication.update -= Tick;
        if (!active) return;
        active = false;
        CaptureType().GetMethod("FinishEditorCapture", Flags).Invoke(null, new object[] { trace, result });
        if (result == "SUCCESS")
        {
            SessionState.SetBool(ArmedKey, false);
            Debug.Log("[GHA ONE SHOT] Home capture complete; recorder disarmed.");
        }
        else
            Debug.Log("[GHA ONE SHOT] Capture incomplete (" + result + "); still armed for the next Play from LoginScene.");
    }
}
#endif
