// Run with Unity CLI eval_file after entering Play Mode at LoginScene.
// Explicit, one-shot diagnostic: no asset changes or permanent runtime hooks.

if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LoginScene")
    throw new InvalidOperationException("Run only in Play Mode at LoginScene.");
var login = UnityEngine.Object.FindFirstObjectByType<VertexFormCore.LoginManager>();
if (login == null || login.PlayerName_InputName == null)
    throw new InvalidOperationException("Login UI is not ready.");
var capture = typeof(GHA.AvatarSuite.UmaHomeAvatar).Assembly.GetType("GHA.AvatarSuite.AvatarLoadProfilerCapture", true);
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
if ((bool)capture.GetField("_active", flags).GetValue(null) || UnityEngine.Profiling.Profiler.enabled)
    throw new InvalidOperationException("Another profiler capture is active; leave it untouched.");
if (UnityEditorInternal.ProfilerDriver.deepProfiling)
    throw new InvalidOperationException("Disable deep profiling before the baseline capture.");
string trace = "normal-login-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
double deadline = EditorApplication.timeSinceStartup + 90;
int readyFrame = -1;
bool complete = false;
Application.LogCallback onLog = null;
EditorApplication.CallbackFunction update = null;
Action<string> finish = result =>
{
    if (complete) return;
    complete = true;
    Application.logMessageReceived -= onLog;
    EditorApplication.update -= update;
    capture.GetMethod("FinishEditorCapture", flags).Invoke(null, new object[] { trace, result });
};
onLog = (message, stack, type) =>
{
    if (message.StartsWith("[GHA LOAD TIMING]") && message.Contains("phase=FINISH host=home ") && message.Contains("result=SUCCESS"))
        readyFrame = Time.frameCount;
};
update = () =>
{
    if (!EditorApplication.isPlaying) finish("PLAY_MODE_ENDED");
    else if (readyFrame >= 0 && Time.frameCount >= readyFrame + 3) finish("SUCCESS");
    else if (EditorApplication.timeSinceStartup >= deadline) finish("TIMEOUT");
};
Application.logMessageReceived += onLog;
EditorApplication.update += update;
try
{
    capture.GetMethod("Begin", flags).Invoke(null, new object[] { trace });
    if (!(bool)capture.GetField("_active", flags).GetValue(null))
        throw new InvalidOperationException("Capture did not start.");
    login.ConnectAnonymously();
}
catch
{
    finish("FAILED");
    throw;
}
return new { trace, captureStarted = true, timeoutSeconds = 90 };
