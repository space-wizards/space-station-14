using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Content.Client.Interfaces.Parallax;
using ICSharpCode.SharpZipLib.Checksum;
using Nett;
using SixLabors.ImageSharp;
using SixLabors.Primitives;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Utility;

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
        private static readonly ResourcePath ParallaxConfigCrcPath = new ResourcePath("/parallax_config_crc");

        public event Action<Texture> OnTextureLoaded;
        public Texture ParallaxTexture { get; private set; }

        public async void LoadParallax()
        {
            MemoryStream configStream = null;
            long crcValue;
            TomlTable table;
            try
            {
                // Load normal config into memory
                if (!_resourceCache.TryContentFileRead(ParallaxConfigPath, out configStream))
                {
                    Logger.ErrorS("parallax", "Parallax config not found.");
                    return;
                }

                // Calculate CRC32 of the config file.
                var crc = new Crc32();
                crc.Update(configStream.ToArray());
                crcValue = crc.Value;

                // See if we we have a previous CRC stored.
                if (_resourceCache.UserData.Exists(ParallaxConfigCrcPath))
                {
                    bool match;
                    using (var data = _resourceCache.UserData.Open(ParallaxConfigCrcPath, FileMode.Open))
                    using (var binaryReader = new BinaryReader(data))
                    {
                        match = binaryReader.ReadInt64() == crcValue;
                    }

                    // If the previous CRC matches, just load the old texture.
                    if (match)
                    {
                        using (var stream = _resourceCache.UserData.Open(ParallaxPath, FileMode.Open))
                        {
                            ParallaxTexture = Texture.LoadFromPNGStream(stream);
                        }

                        OnTextureLoaded?.Invoke(ParallaxTexture);
                        return;
                    }
                }

                // Well turns out the CRC does not match so the config changed.
                // Read the new config and get rid of the config memory stream.
                using (var reader = new StreamReader(configStream, Encoding.UTF8))
                {
                    table = Toml.ReadString(reader.ReadToEnd());
                }
            }
            finally
            {
                configStream?.Dispose();
            }

            // Generate the parallax in the thread pool.
            var image = await Task.Run(() => ParallaxGenerator.GenerateParallax(table, new Size(1920, 1080)));
            // And load it in the main thread for safety reasons.
            ParallaxTexture = Texture.LoadFromImage(image);

            // Store it and CRC so further game starts don't need to regenerate it.
            using (var stream = _resourceCache.UserData.Open(ParallaxPath, FileMode.Create))
            {
                image.SaveAsPng(stream);
            }

            using (var stream = _resourceCache.UserData.Open(ParallaxConfigCrcPath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(crcValue);
            }

            OnTextureLoaded?.Invoke(ParallaxTexture);
        }
    }
}
