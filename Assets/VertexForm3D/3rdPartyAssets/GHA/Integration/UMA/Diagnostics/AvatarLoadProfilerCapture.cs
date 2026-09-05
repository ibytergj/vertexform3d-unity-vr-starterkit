#if VERTEXFORM_GHA_HOST && VERTEXFORM_GHA_UMA
using System.Collections;

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
#endif

namespace GHA.AvatarSuite
{
    /// <summary>
    /// Temporary, event-driven profiler capture for the deferred Home avatar load.
    ///
    /// Removal is intentionally simple:
    /// 1. Delete this file.
    /// 2. Remove the Begin and CompleteAfterFrames calls from UmaAvatarCustomizer.
    ///
    /// The implementation is Editor-only. Player builds retain two no-op entry points,
    /// so this diagnostic cannot enable profiling or write files in production.
    /// </summary>
    internal static class AvatarLoadProfilerCapture
    {
        public const string Marker = "[GHA PROFILE CAPTURE]";

        public static void Begin(string traceId)
        {
#if UNITY_EDITOR
            BeginEditorCapture(traceId);
#endif
        }

        public static IEnumerator CompleteAfterFrames(string traceId, int framesToRetain)
        {
#if UNITY_EDITOR
            if (!_active || !string.Equals(_traceId, traceId, StringComparison.Ordinal))
                yield break;

            int remaining = Math.Max(0, framesToRetain);
            while (remaining-- > 0)
                yield return new WaitForEndOfFrame();

            FinishEditorCapture(traceId, "SUCCESS");
#else
            yield break;
#endif
        }

#if UNITY_EDITOR
        private const int RecorderCapacity = 512;
        private const int CaptureBufferBytes = 512 * 1024 * 1024;

        // These are resolved by name across all categories. Invalid/unavailable markers
        // are reported once at capture start and otherwise ignored.
        private static readonly string[] CandidateMetrics =
        {
            "Main Thread",
            "Render Thread",
            "EditorLoop",
            "PlayerLoop",
            "Application.Tick",
            "GC.Collect",
            "GC Reserved Memory",
            "GC Used Memory",
            "System Used Memory",
            "Total Used Memory",
            "File Bytes Read",
            "File Bytes Written",
            "File Reads Started",
            "File Reads Finished",
            "File Seeks",
            "Files Opened",
            "Files Closed",
            "Reads in Flight",
            "Mesh Reads",
            "Texture Reads",
            "Scripting Reads",
            "Other Reads",
            "WaitForJobGroupID",
            "JobHandle.Complete",
            "Shader.Parse",
            "Shader.CreateGPUProgram",
            "Gfx.WaitForPresentOnGfxThread",
            "XR.WaitForGPU",
            "OpenXRLoader.ProcessOpenXRMessageLoop",
            "AssetDatabase.Refresh",
            "AssetDatabase.ImportAsset"
        };

        private sealed class MetricCapture
        {
            public string Name;
            public ProfilerRecorder Recorder;
        }

        private static readonly List<MetricCapture> Recorders = new List<MetricCapture>();

        private static bool _active;
        private static string _traceId;
        private static string _rawPath;
        private static double _startedAt;

        private static bool _previousProfilerEnabled;
        private static bool _previousBinaryLogEnabled;
        private static bool _previousAllocationCallstacksEnabled;
        private static string _previousLogFile;
        private static int _previousMaxUsedMemory;

        private static void BeginEditorCapture(string traceId)
        {
            if (string.IsNullOrWhiteSpace(traceId))
                traceId = "unidentified";

            if (_active)
                FinishEditorCapture(_traceId, "SUPERSEDED");

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError($"{Marker} phase=START_FAILED trace={traceId} reason=PROJECT_ROOT_UNAVAILABLE");
                return;
            }

            // Library is outside the Asset Database and ignored by normal Unity source control.
            // Captures therefore cannot trigger imports or pollute the working tree.
            string outputDirectory = Path.Combine(projectRoot, "Library", "GHAProfiles");
            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Marker} phase=START_FAILED trace={traceId} reason=CREATE_DIRECTORY " +
                    $"exception='{exception.GetType().Name}: {exception.Message}'");
                return;
            }

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            _rawPath = Path.Combine(outputDirectory, $"{timestamp}-{SanitizeFileName(traceId)}.raw");
            _traceId = traceId;
            _startedAt = Time.realtimeSinceStartupAsDouble;

            _previousProfilerEnabled = Profiler.enabled;
            _previousBinaryLogEnabled = Profiler.enableBinaryLog;
            _previousAllocationCallstacksEnabled = Profiler.enableAllocationCallstacks;
            _previousLogFile = Profiler.logFile;
            _previousMaxUsedMemory = Profiler.maxUsedMemory;

            try
            {
                Profiler.enabled = false;
                Profiler.logFile = _rawPath;
                Profiler.maxUsedMemory = Math.Max(_previousMaxUsedMemory, CaptureBufferBytes);
                Profiler.enableAllocationCallstacks = false;
                Profiler.enableBinaryLog = true;
                Profiler.enabled = true;

                StartRecorders();
                _active = true;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

                Debug.Log(
                    $"{Marker} phase=START trace={_traceId} realtime={_startedAt:F3} " +
                    $"frame={Time.frameCount} raw='{_rawPath}' validRecorders={Recorders.Count} " +
                    $"deepProfiling=false bufferMB={Profiler.maxUsedMemory / (1024 * 1024)}");
            }
            catch (Exception exception)
            {
                DisposeRecorders();
                RestoreProfilerState();
                ResetCaptureState();
                Debug.LogError(
                    $"{Marker} phase=START_FAILED trace={traceId} reason=PROFILER_CONFIGURATION " +
                    $"exception='{exception.GetType().Name}: {exception.Message}'");
            }
        }

        private static void StartRecorders()
        {
            DisposeRecorders();

            var unavailable = new List<string>();
            foreach (string metricName in CandidateMetrics)
            {
                try
                {
                    var recorder = new ProfilerRecorder(
                        metricName,
                        RecorderCapacity,
                        ProfilerRecorderOptions.Default | ProfilerRecorderOptions.StartImmediately);
                    if (!recorder.Valid)
                    {
                        recorder.Dispose();
                        unavailable.Add(metricName);
                        continue;
                    }

                    Recorders.Add(new MetricCapture
                    {
                        Name = metricName,
                        Recorder = recorder
                    });
                }
                catch (Exception)
                {
                    unavailable.Add(metricName);
                }
            }

            if (unavailable.Count > 0)
            {
                Debug.Log(
                    $"{Marker} phase=METRICS_UNAVAILABLE trace={_traceId} " +
                    $"metrics='{string.Join(",", unavailable)}'");
            }
        }

        private static void FinishEditorCapture(string traceId, string result)
        {
            if (!_active || !string.Equals(_traceId, traceId, StringComparison.Ordinal))
                return;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            double finishedAt = Time.realtimeSinceStartupAsDouble;
            string[] metricSummaries = BuildMetricSummaries();
            DisposeRecorders();

            // Disable and clear logFile before emitting summaries so Unity closes the raw
            // stream and the diagnostic logs do not contaminate the measured interval.
            RestoreProfilerState();

            Debug.Log(
                $"{Marker} phase=FINISH trace={_traceId} result={result} " +
                $"elapsedMs={(finishedAt - _startedAt) * 1000d:F1} frame={Time.frameCount} " +
                $"raw='{_rawPath}' metricCount={metricSummaries.Length}");

            foreach (string summary in metricSummaries)
                Debug.Log($"{Marker} phase=METRIC trace={_traceId} {summary}");

            ResetCaptureState();
        }

        private static string[] BuildMetricSummaries()
        {
            var summaries = new List<string>(Recorders.Count);
            foreach (MetricCapture metric in Recorders)
            {
                ProfilerRecorder recorder = metric.Recorder;
                if (!recorder.Valid)
                    continue;

                int count = recorder.Count;
                long total = 0L;
                long maximum = 0L;
                int maximumSample = -1;
                for (int i = 0; i < count; i++)
                {
                    long value = recorder.GetSample(i).Value;
                    total += value;
                    if (maximumSample < 0 || value > maximum)
                    {
                        maximum = value;
                        maximumSample = i;
                    }
                }

                string unit = recorder.UnitType.ToString();
                summaries.Add(
                    $"metric='{metric.Name}' unit={unit} samples={count} " +
                    $"totalRaw={total} maxRaw={maximum} maxSample={maximumSample} " +
                    $"maxFormatted={FormatValue(maximum, unit)} wrapped={recorder.WrappedAround}");
            }

            return summaries.ToArray();
        }

        private static string FormatValue(long value, string unit)
        {
            if (unit.IndexOf("Nanosecond", StringComparison.OrdinalIgnoreCase) >= 0)
                return $"{value / 1_000_000d:F3}ms";
            if (unit.IndexOf("Byte", StringComparison.OrdinalIgnoreCase) >= 0)
                return $"{value / (1024d * 1024d):F3}MiB";
            return value.ToString();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!_active)
                return;

            if (state == PlayModeStateChange.ExitingPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                FinishEditorCapture(_traceId, $"PLAY_MODE_{state.ToString().ToUpperInvariant()}");
            }
        }

        private static void RestoreProfilerState()
        {
            try
            {
                Profiler.enabled = false;
                Profiler.logFile = string.Empty;
                Profiler.enableBinaryLog = _previousBinaryLogEnabled;
                Profiler.logFile = _previousLogFile ?? string.Empty;
                Profiler.maxUsedMemory = _previousMaxUsedMemory;
                Profiler.enableAllocationCallstacks = _previousAllocationCallstacksEnabled;
                Profiler.enabled = _previousProfilerEnabled;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Marker} phase=RESTORE_FAILED trace={_traceId} " +
                    $"exception='{exception.GetType().Name}: {exception.Message}'");
            }
        }

        private static void DisposeRecorders()
        {
            foreach (MetricCapture metric in Recorders)
            {
                try
                {
                    metric.Recorder.Dispose();
                }
                catch (Exception)
                {
                    // Best-effort cleanup during Play-mode shutdown.
                }
            }
            Recorders.Clear();
        }

        private static void ResetCaptureState()
        {
            _active = false;
            _traceId = null;
            _rawPath = null;
            _startedAt = -1d;
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }
#endif
    }
}
#endif
