using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Content.IntegrationTests;
using NUnit.Framework;

namespace Content.Benchmarks
{
    public class ContentBenchmark : ContentIntegrationTest
    {
        private string _robustResourceDest =
            Path.Combine(Directory.GetCurrentDirectory(), "../../RobustToolbox/Resources/");
        private string _resourceDest = Path.Combine(Directory.GetCurrentDirectory(), "../../Resources/");
        private string _serverDest = Path.Combine(Directory.GetCurrentDirectory(), "../../bin/Content.Server/");

        [GlobalSetup]
        public void Setup()
        {
            DirectoryCopy(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../../RobustToolbox/Resources"),
                _robustResourceDest);
            DirectoryCopy(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../Content.Server"),
                _serverDest);
            DirectoryCopy(Path.Combine(Directory.GetCurrentDirectory(), "../../../../../../Resources"),
                _resourceDest);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Directory.Delete(_robustResourceDest, true);
            Directory.Delete(_serverDest, true);
            Directory.Delete(_resourceDest, true);
        }

        protected override ServerIntegrationInstance StartServer(ServerIntegrationOptions options = null)
        {
            options ??= new ServerContentIntegrationOption();
            options.NotThreaded = true;
            return base.StartServer(options);
        }

        // I'm lazy and grabbed this method from here:
        // https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            var dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (var subdir in dirs)
                {
                    var tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}
