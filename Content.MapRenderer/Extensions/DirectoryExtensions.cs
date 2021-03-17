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
            var root = RepositoryRoot();

            root.MoveTo("Resources");

            return root;
        }

        public static DirectoryInfo Workflows()
        {
            var root = RepositoryRoot().CreateSubdirectory(".github/workflows");

            return root;
        }

        public static DirectoryInfo Maps()
        {
            var resources = Resources();
            var mapImages = resources.CreateSubdirectory("Maps");

            return mapImages;
        }

        public static DirectoryInfo MapImages()
        {
            var resources = Resources();
            var mapImages = resources.CreateSubdirectory("MapImages");

            return mapImages;
        }
    }
}
