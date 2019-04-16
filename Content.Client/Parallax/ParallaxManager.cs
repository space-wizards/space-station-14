using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Content.Client.Interfaces.Parallax;
using ICSharpCode.SharpZipLib.Checksum;
using Nett;
using SixLabors.ImageSharp;
using SixLabors.Primitives;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Client.Parallax
{
    public class ParallaxManager : IParallaxManager
    {
#pragma warning disable 649
        [Dependency] private readonly IResourceCache _resourceCache;
#pragma warning restore 649

        private static readonly ResourcePath ParallaxConfigPath = new ResourcePath("/parallax_config.toml");

        // Both of these below are in the user directory.
        private static readonly ResourcePath ParallaxPath = new ResourcePath("/parallax_cache.png");
        private static readonly ResourcePath ParallaxConfigOld = new ResourcePath("/parallax_config_old");

        public event Action<Texture> OnTextureLoaded;
        public Texture ParallaxTexture { get; private set; }

        public async void LoadParallax()
        {
            MemoryStream configStream = null;
            string contents;
            TomlTable table;
            try
            {
                // Load normal config into memory
                if (!_resourceCache.TryContentFileRead(ParallaxConfigPath, out configStream))
                {
                    Logger.ErrorS("parallax", "Parallax config not found.");
                    return;
                }

                using (var reader = new StreamReader(configStream, EncodingHelpers.UTF8))
                {
                    contents = reader.ReadToEnd();
                }

                if (_resourceCache.UserData.Exists(ParallaxConfigOld))
                {
                    bool match;
                    using (var data = _resourceCache.UserData.Open(ParallaxConfigOld, FileMode.Open))
                    using (var reader = new StreamReader(data, EncodingHelpers.UTF8))
                    {
                        match = reader.ReadToEnd() == contents;
                    }

                    if (match)
                    {
                        using (var stream = _resourceCache.UserData.Open(ParallaxPath, FileMode.Open))
                        {
                            ParallaxTexture = Texture.LoadFromPNGStream(stream, "Parallax");
                        }

                        OnTextureLoaded?.Invoke(ParallaxTexture);
                        return;
                    }
                }

                table = Toml.ReadString(contents);
            }
            finally
            {
                configStream?.Dispose();
            }

            // Generate the parallax in the thread pool.
            var image = await Task.Run(() => ParallaxGenerator.GenerateParallax(table, new Size(1920, 1080)));
            // And load it in the main thread for safety reasons.
            ParallaxTexture = Texture.LoadFromImage(image, "Parallax");

            // Store it and CRC so further game starts don't need to regenerate it.
            using (var stream = _resourceCache.UserData.Open(ParallaxPath, FileMode.Create))
            {
                image.SaveAsPng(stream);
            }

            using (var stream = _resourceCache.UserData.Open(ParallaxConfigOld, FileMode.Create))
            using (var writer = new StreamWriter(stream, EncodingHelpers.UTF8))
            {
                writer.Write(contents);
            }

            OnTextureLoaded?.Invoke(ParallaxTexture);
        }
    }
}
