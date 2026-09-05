#if UNITY_EDITOR && VERTEXFORM_GHA_HOST && VERTEXFORM_GHA_UMA
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace GHA.AvatarSuite.Editor
{
    /// <summary>
    /// Temporary, removable analyzer for the binary profiler captures written by
    /// AvatarLoadProfilerCapture.
    ///
    /// Removal: delete this file and its .meta file.
    /// </summary>
    internal static class AvatarLoadProfileReportGenerator
    {
        private const string MenuPath = "Tools/GHA/Analyze Latest Avatar Capture";
        private const string Marker = "[GHA PROFILE REPORT]";
        private const int MaximumFramesToReport = 64;
        private const int MaximumThreadsPerFrame = 128;
        private const int MaximumSamplesPerThread = 20000;
        private const int MaximumRankedSamples = 100;
        private const float MinimumRankedTimeMs = 0.01f;

        private static readonly string[] SuspectTerms =
        {
            "uma",
            "asset",
            "resource",
            "addressable",
            "bundle",
            "load",
            "read",
            "file",
            "serialize",
            "deserialize",
            "recipe",
            "mesh",
            "texture",
            "generator",
            "index"
        };

        private sealed class SampleRecord
        {
            public string Path;
            public string Name;
            public float TotalMs;
            public float SelfMs;
            public string Calls;
            public string GcAlloc;
            public int Depth;
        }

        [MenuItem(MenuPath, priority = 2200)]
        private static void AnalyzeLatestCapture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning($"{Marker} phase=SKIPPED reason=PLAY_MODE_ACTIVE");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string captureDirectory = string.IsNullOrEmpty(projectRoot)
                ? null
                : Path.Combine(projectRoot, "Library", "GHAProfiles");

            FileInfo capture = FindLatestCapture(captureDirectory);
            if (capture == null)
            {
                Debug.LogError(
                    $"{Marker} phase=FAILED reason=CAPTURE_NOT_FOUND directory='{captureDirectory}'");
                return;
            }

            Debug.Log($"{Marker} phase=START raw='{capture.FullName}' bytes={capture.Length}");

            try
            {
                LoadProfile(capture.FullName);

                int firstFrame = checked((int)ProfilerDriver.firstFrameIndex);
                int lastFrame = checked((int)ProfilerDriver.lastFrameIndex);
                if (lastFrame < firstFrame)
                    throw new InvalidOperationException(
                        $"Loaded profile contains no frames: [{firstFrame}, {lastFrame}].");

                List<int> frames = SelectFrames(firstFrame, lastFrame);
                var report = new StringBuilder(256 * 1024);
                report.AppendLine("GHA Avatar Load Profiler Report");
                report.AppendLine($"Generated UTC: {DateTime.UtcNow:O}");
                report.AppendLine($"Capture: {capture.FullName}");
                report.AppendLine($"Capture bytes: {capture.Length}");
                report.AppendLine($"Profiler frame range: [{firstFrame}, {lastFrame}]");
                report.AppendLine($"Frames reported: {string.Join(", ", frames)}");
                report.AppendLine();

                foreach (int frame in frames)
                    AppendFrame(report, frame);

                string reportPath = Path.Combine(
                    capture.DirectoryName ?? captureDirectory,
                    Path.GetFileNameWithoutExtension(capture.Name) + "-analysis.txt");
                File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));

                Debug.Log(
                    $"{Marker} phase=FINISH result=SUCCESS raw='{capture.FullName}' " +
                    $"report='{reportPath}' firstFrame={firstFrame} lastFrame={lastFrame} " +
                    $"reportedFrames={frames.Count}");
            }
            catch (Exception exception)
            {
                Exception root = exception is TargetInvocationException invocation &&
                                 invocation.InnerException != null
                    ? invocation.InnerException
                    : exception;
                Debug.LogError(
                    $"{Marker} phase=FAILED raw='{capture.FullName}' " +
                    $"exception='{root.GetType().Name}: {root.Message}'\n{root.StackTrace}");
            }
        }

        private static FileInfo FindLatestCapture(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return null;

            return new DirectoryInfo(directory)
                .GetFiles("*.raw", SearchOption.TopDirectoryOnly)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static void LoadProfile(string capturePath)
        {
            MethodInfo[] candidates = typeof(ProfilerDriver)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(method => method.Name == "LoadProfile")
                .Where(method =>
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length > 0 && parameters[0].ParameterType == typeof(string);
                })
                .OrderBy(method => method.GetParameters().Length)
                .ToArray();

            if (candidates.Length == 0)
                throw new MissingMethodException(
                    typeof(ProfilerDriver).FullName,
                    "LoadProfile(string, ...)");

            Exception lastFailure = null;
            foreach (MethodInfo candidate in candidates)
            {
                try
                {
                    ParameterInfo[] parameters = candidate.GetParameters();
                    object[] arguments = new object[parameters.Length];
                    arguments[0] = capturePath;
                    for (int index = 1; index < parameters.Length; index++)
                    {
                        ParameterInfo parameter = parameters[index];
                        if (parameter.HasDefaultValue)
                            arguments[index] = parameter.DefaultValue;
                        else if (parameter.ParameterType == typeof(bool))
                            arguments[index] = false;
                        else if (parameter.ParameterType.IsValueType)
                            arguments[index] = Activator.CreateInstance(parameter.ParameterType);
                        else
                            arguments[index] = null;
                    }

                    object result = candidate.Invoke(null, arguments);
                    if (candidate.ReturnType == typeof(bool) && result is bool loaded && !loaded)
                        throw new InvalidOperationException(
                            $"ProfilerDriver.{candidate} returned false.");

                    return;
                }
                catch (Exception exception)
                {
                    lastFailure = exception;
                }
            }

            throw new InvalidOperationException(
                "Every available ProfilerDriver.LoadProfile overload failed.",
                lastFailure);
        }

        private static List<int> SelectFrames(int firstFrame, int lastFrame)
        {
            int frameCount = lastFrame - firstFrame + 1;
            if (frameCount <= MaximumFramesToReport)
                return Enumerable.Range(firstFrame, frameCount).ToList();

            int half = MaximumFramesToReport / 2;
            var selected = new List<int>(MaximumFramesToReport);
            selected.AddRange(Enumerable.Range(firstFrame, half));
            selected.AddRange(Enumerable.Range(lastFrame - half + 1, half));
            return selected.Distinct().OrderBy(frame => frame).ToList();
        }

        private static void AppendFrame(StringBuilder report, int frame)
        {
            report.AppendLine(new string('=', 100));
            report.AppendLine($"FRAME {frame}");

            int validThreadCount = 0;
            for (int threadIndex = 0; threadIndex < MaximumThreadsPerFrame; threadIndex++)
            {
                try
                {
                    using (HierarchyFrameDataView frameData = ProfilerDriver.GetHierarchyFrameDataView(
                               frame,
                               threadIndex,
                               HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                               HierarchyFrameDataView.columnTotalTime,
                               false))
                    {
                        if (!frameData.valid)
                        {
                            if (threadIndex > validThreadCount)
                                break;
                            continue;
                        }

                        validThreadCount++;
                        AppendThread(report, frameData, threadIndex);
                    }
                }
                catch (ArgumentException)
                {
                    break;
                }
                catch (IndexOutOfRangeException)
                {
                    break;
                }
            }

            report.AppendLine($"Valid threads reported: {validThreadCount}");
            report.AppendLine();
        }

        private static void AppendThread(
            StringBuilder report,
            HierarchyFrameDataView frameData,
            int threadIndex)
        {
            var samples = new List<SampleRecord>();
            int rootId = frameData.GetRootItemID();
            var rootChildren = new List<int>();
            frameData.GetItemChildren(rootId, rootChildren);
            CollectSamples(frameData, rootChildren, samples, string.Empty, 0);

            report.AppendLine();
            report.AppendLine(
                $"THREAD {threadIndex}: '{frameData.threadName}' " +
                $"frameTimeMs={Format(frameData.frameTimeMs)} " +
                $"frameGpuMs={Format(frameData.frameGpuTimeMs)} " +
                $"sampleCount={frameData.sampleCount} collected={samples.Count}");

            AppendRanking(
                report,
                "TOP BY TOTAL TIME",
                samples.Where(sample => sample.TotalMs >= MinimumRankedTimeMs)
                    .OrderByDescending(sample => sample.TotalMs)
                    .ThenByDescending(sample => sample.SelfMs)
                    .Take(MaximumRankedSamples));

            AppendRanking(
                report,
                "TOP BY SELF TIME",
                samples.Where(sample => sample.SelfMs >= MinimumRankedTimeMs)
                    .OrderByDescending(sample => sample.SelfMs)
                    .ThenByDescending(sample => sample.TotalMs)
                    .Take(MaximumRankedSamples));

            AppendRanking(
                report,
                "LOAD/UMA/IO RELATED PATHS",
                samples.Where(IsSuspect)
                    .Where(sample =>
                        sample.TotalMs >= MinimumRankedTimeMs ||
                        sample.SelfMs >= MinimumRankedTimeMs)
                    .OrderByDescending(sample => sample.TotalMs)
                    .ThenByDescending(sample => sample.SelfMs)
                    .Take(MaximumRankedSamples));
        }

        private static void CollectSamples(
            HierarchyFrameDataView frameData,
            List<int> itemIds,
            List<SampleRecord> output,
            string parentPath,
            int depth)
        {
            if (output.Count >= MaximumSamplesPerThread)
                return;

            foreach (int itemId in itemIds)
            {
                if (output.Count >= MaximumSamplesPerThread)
                    break;

                string name = frameData.GetItemName(itemId);
                string path = string.IsNullOrEmpty(parentPath)
                    ? name
                    : parentPath + " > " + name;

                output.Add(new SampleRecord
                {
                    Path = path,
                    Name = name,
                    TotalMs = frameData.GetItemColumnDataAsFloat(
                        itemId,
                        HierarchyFrameDataView.columnTotalTime),
                    SelfMs = frameData.GetItemColumnDataAsFloat(
                        itemId,
                        HierarchyFrameDataView.columnSelfTime),
                    Calls = frameData.GetItemColumnData(
                        itemId,
                        HierarchyFrameDataView.columnCalls),
                    GcAlloc = frameData.GetItemColumnData(
                        itemId,
                        HierarchyFrameDataView.columnGcMemory),
                    Depth = depth
                });

                if (!frameData.HasItemChildren(itemId))
                    continue;

                var children = new List<int>();
                frameData.GetItemChildren(itemId, children);
                CollectSamples(frameData, children, output, path, depth + 1);
            }
        }

        private static void AppendRanking(
            StringBuilder report,
            string heading,
            IEnumerable<SampleRecord> ranking)
        {
            report.AppendLine($"  {heading}");
            int index = 0;
            foreach (SampleRecord sample in ranking)
            {
                report.AppendLine(
                    $"    {++index,3}. total={Format(sample.TotalMs),12}ms " +
                    $"self={Format(sample.SelfMs),12}ms calls={sample.Calls,-8} " +
                    $"gc={sample.GcAlloc,-10} depth={sample.Depth} path={sample.Path}");
            }

            if (index == 0)
                report.AppendLine("      (none)");
        }

        private static bool IsSuspect(SampleRecord sample)
        {
            string path = sample.Path ?? sample.Name ?? string.Empty;
            return SuspectTerms.Any(
                term => path.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string Format(float value)
        {
            return value.ToString("F3", CultureInfo.InvariantCulture);
        }
    }
}
#endif
