using System.IO;
using System.Reflection;

namespace Content.MapRenderer.Extensions
{
    public static class DirectoryExtensions
    {
        public static DirectoryInfo RepositoryRoot()
        {
            // space-station-14/bin/Content.MapRenderer/Content.MapRenderer.dll
            var currentLocation = Assembly.GetExecutingAssembly().Location;

            // space-station-14
            return Directory.GetParent(currentLocation)!.Parent!.Parent!;
        }

        public static DirectoryInfo Resources()
        {
            return new DirectoryInfo($"{RepositoryRoot()}{Path.DirectorySeparatorChar}Resources");
        }

        public static DirectoryInfo Maps()
        {
            return new DirectoryInfo($"{Resources()}{Path.DirectorySeparatorChar}Maps");
        }

        public static DirectoryInfo MapImages()
        {
            return new DirectoryInfo($"{Resources()}{Path.DirectorySeparatorChar}MapImages");
        }
    }
}
