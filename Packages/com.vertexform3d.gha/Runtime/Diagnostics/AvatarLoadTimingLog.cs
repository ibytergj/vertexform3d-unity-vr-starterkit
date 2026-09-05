using System;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GHA.AvatarSuite
{
    /// <summary>
    /// Correlated load-timing diagnostics for UMA construction and its prerequisite gates.
    /// Filter the console for Marker to isolate these entries from general scene loading.
    /// </summary>
    public static class AvatarLoadTimingLog
    {
        public const string Marker = "[GHA LOAD TIMING]";

        private static int _nextTraceId;

        public static double Now => Time.realtimeSinceStartupAsDouble;

        public static string NewTraceId(string scope)
        {
            return $"{scope}-{Interlocked.Increment(ref _nextTraceId):D4}";
        }

        public static void Write(
            string traceId,
            string category,
            string phase,
            string host,
            double startedAt = -1d,
            string details = null)
        {
            double now = Now;
            string elapsed = startedAt >= 0d
                ? $" elapsedMs={(now - startedAt) * 1000d:F1}"
                : string.Empty;
            string extra = string.IsNullOrEmpty(details) ? string.Empty : $" {details}";

            Debug.Log(
                $"{Marker} utc={DateTime.UtcNow:O} realtime={now:F3} frame={Time.frameCount} " +
                $"trace={traceId} category={category} phase={phase} host={host}" +
                $"{elapsed} {SceneContext()}{extra}");
        }

        private static string SceneContext()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            var loadedScenes = new StringBuilder();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (i > 0)
                    loadedScenes.Append(',');
                loadedScenes.Append(scene.name);
            }

            return $"activeScene='{activeScene.name}' loadedScenes='{loadedScenes}'";
        }
    }
}
