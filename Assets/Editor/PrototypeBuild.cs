using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SpaceRunner.Editor
{
    public static class PrototypeBuild
    {
        [MenuItem("Space Runner/Build Windows Prototype")]
        public static void BuildWindows()
        {
            var output = Environment.GetEnvironmentVariable("SPACE_RUNNER_BUILD_PATH");
            if (string.IsNullOrWhiteSpace(output)) output = "Builds/WatchPrototype/SpaceRunner.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Scenes/WatchPrototype.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Watch prototype build failed: " + report.summary.result);
        }
    }
}
