using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using RoyalVault.Unity;
using SlotKit;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RoyalVault.Tests
{
    // Opt-in: set ROYALVAULT_PERF to a file and run this test to record frame cost and memory per game phase
    public sealed class PerformanceProbeTests
    {
        private sealed class Counter
        {
            public readonly string Name;
            public readonly ProfilerRecorder Recorder;
            public double Sum;
            public double Max;

            public Counter(string name, ProfilerRecorder recorder)
            {
                Name = name;
                Recorder = recorder;
            }
        }

        private readonly List<Counter> counters = new List<Counter>();
        private readonly StringBuilder report = new StringBuilder();
        private RenderTexture target;
        private int frames;

        [UnityTest, Timeout(600000)]
        public IEnumerator RecordFrameCostAndMemory()
        {
            string file = System.Environment.GetEnvironmentVariable("ROYALVAULT_PERF");
            Assume.That(file, Is.Not.Null.And.Not.Empty);
            PlayerPrefs.DeleteAll();
            StartCounters();
            yield return Phase("empty editor frame", 1f, null);
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var game = Object.FindFirstObjectByType<GameBootstrap>();
            game.Story.Mute();
            RenderThroughCamera();
            yield return new WaitForSeconds(1f);

            yield return Phase("idle", 3f, null);
            yield return Phase("spinning", 1.2f, () => Spin(game, new[] { 19, 18, 40, 46, 23 }));
            yield return WaitIdle(game);
            yield return Phase("line win", 2.5f, () => Spin(game, new[] { 19, 18, 40, 46, 23 }));
            yield return WaitIdle(game);
            yield return Phase("big win", 6f, () => Spin(game, new[] { 33, 13, 48, 1, 5 }));

            Memory();
            File.WriteAllText(file, report.ToString());
            Debug.Log("[perf]\n" + report);
            StopCounters();
            PlayerPrefs.DeleteAll();
        }

        private static void Spin(GameBootstrap game, int[] stops)
        {
            game.ForceNextStops(stops);
            Assert.IsTrue(game.Flow.RequestSpin());
        }

        private static IEnumerator WaitIdle(GameBootstrap game)
        {
            for (float t = 0f; t < 60f && game.Flow.State != SpinState.Idle; t += Time.deltaTime)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator Phase(string name, float seconds, System.Action start)
        {
            if (start != null)
            {
                start();
            }

            ResetTotals();
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                yield return null;
                if (Camera.main != null && Camera.main.targetTexture != null)
                {
                    Camera.main.Render();
                }

                Sample();
            }

            Write(name);
        }

        // Batch mode has no game view, so the canvases draw through the camera into a texture
        private void RenderThroughCamera()
        {
            target = new RenderTexture(1920, 1080, 24);
            Camera camera = Camera.main;
            camera.targetTexture = target;
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.isRootCanvas)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 5f;
                }
            }
        }

        private void StartCounters()
        {
            Add("main thread ms", ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1));
            Add("ui canvases ms", new ProfilerRecorder("PostLateUpdate.PlayerUpdateCanvases", 1, ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.SumAllSamplesInFrame));
            Add("ui batch build ms", new ProfilerRecorder("Canvas.BuildBatch", 1, ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.SumAllSamplesInFrame));
            Add("gc bytes", ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1));
            Add("batches", ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 1));
            Add("setpass", ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 1));
            Add("draw calls", ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1));
            Add("vertices", ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count", 1));
        }

        private void Add(string name, ProfilerRecorder recorder)
        {
            counters.Add(new Counter(name, recorder));
        }

        private void ResetTotals()
        {
            frames = 0;
            foreach (Counter counter in counters)
            {
                counter.Sum = 0;
                counter.Max = 0;
            }
        }

        private void Sample()
        {
            frames++;
            foreach (Counter counter in counters)
            {
                if (!counter.Recorder.Valid)
                {
                    continue;
                }

                double value = counter.Recorder.LastValue;
                if (counter.Name.EndsWith("ms"))
                {
                    value /= 1e6;
                }

                counter.Sum += value;
                counter.Max = System.Math.Max(counter.Max, value);
            }
        }

        private void Write(string phase)
        {
            report.AppendLine(phase + " (" + frames + " frames)");
            foreach (Counter counter in counters)
            {
                string valid = counter.Recorder.Valid ? "" : " (not recorded)";
                double average = frames > 0 ? counter.Sum / frames : 0;
                report.AppendLine("  " + counter.Name + ": avg " + average.ToString("0.##") + ", max " + counter.Max.ToString("0.##") + valid);
            }
        }

        private void Memory()
        {
            long textures = 0;
            int count = 0;
            var largest = new List<(long size, string line)>();
            foreach (Texture2D texture in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                long size = Profiler.GetRuntimeMemorySizeLong(texture);
                textures += size;
                count++;
                string line = texture.name + " " + texture.width + "x" + texture.height + " " + texture.format + " " + (size / 1048576f).ToString("0.00") + " MB";
                largest.Add((size, line));
            }

            largest.Sort((a, b) => b.size.CompareTo(a.size));

            long audio = 0;
            foreach (AudioClip clip in Resources.FindObjectsOfTypeAll<AudioClip>())
            {
                audio += Profiler.GetRuntimeMemorySizeLong(clip);
            }

            report.AppendLine("memory");
            report.AppendLine("  textures: " + count + " using " + (textures / 1048576f).ToString("0.0") + " MB");
            report.AppendLine("  audio: " + (audio / 1048576f).ToString("0.0") + " MB");
            report.AppendLine("  total allocated: " + (Profiler.GetTotalAllocatedMemoryLong() / 1048576f).ToString("0.0") + " MB");
            report.AppendLine("  managed heap: " + (Profiler.GetMonoUsedSizeLong() / 1048576f).ToString("0.0") + " MB");
            report.AppendLine("largest textures");
            for (int i = 0; i < largest.Count && i < 500; i++)
            {
                report.AppendLine("  " + largest[i].line);
            }
        }

        private void StopCounters()
        {
            foreach (Counter counter in counters)
            {
                counter.Recorder.Dispose();
            }

            counters.Clear();
            if (target != null)
            {
                target.Release();
            }
        }
    }
}
