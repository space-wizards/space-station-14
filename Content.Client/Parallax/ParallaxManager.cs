using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Content.Client.Interfaces.Parallax;
using Content.Shared;
using Nett;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Parallax
{
    internal sealed class ParallaxManager : IParallaxManager
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private static readonly ResourcePath ParallaxConfigPath = new("/parallax_config.toml");

        // Both of these below are in the user directory.
        private static readonly ResourcePath ParallaxPath = new("/parallax_cache.png");
        private static readonly ResourcePath ParallaxConfigOld = new("/parallax_config_old");

        public event Action<Texture> OnTextureLoaded;
        public Texture ParallaxTexture { get; private set; }

        public async void LoadParallax()
        {
            if (!_configurationManager.GetCVar(CCVars.ParallaxEnabled))
            {
                return;
            }

            var debugParallax = _configurationManager.GetCVar(CCVars.ParallaxDebug);
            string contents;
            TomlTable table;
            // Load normal config into memory
            if (!_resourceCache.TryContentFileRead(ParallaxConfigPath, out var configStream))
            {
                Logger.ErrorS("parallax", "Parallax config not found.");
                return;
            }

            using (configStream)
            {
                using (var reader = new StreamReader(configStream, EncodingHelpers.UTF8))
                {
                    contents = reader.ReadToEnd();
                }

                if (!debugParallax && _resourceCache.UserData.Exists(ParallaxConfigOld))
                {
                    var match = _resourceCache.UserData.ReadAllText(ParallaxConfigOld) == contents;

                    if (match)
                    {
                        using (var stream = _resourceCache.UserData.OpenRead(ParallaxPath))
                        {
                            ParallaxTexture = Texture.LoadFromPNGStream(stream, "Parallax");
                        }

                        OnTextureLoaded?.Invoke(ParallaxTexture);
                        return;
                    }
                }

                table = Toml.ReadString(contents);
            }

            List<Image<Rgba32>> debugImages = null;
            if (debugParallax)
            {
                debugImages = new List<Image<Rgba32>>();
            }

            var sawmill = _logManager.GetSawmill("parallax");
            // Generate the parallax in the thread pool.
            var image = await Task.Run(() =>
                ParallaxGenerator.GenerateParallax(table, new Size(1920, 1080), sawmill, debugImages));
            // And load it in the main thread for safety reasons.
            ParallaxTexture = Texture.LoadFromImage(image, "Parallax");

            // Store it and CRC so further game starts don't need to regenerate it.
            using (var stream = _resourceCache.UserData.Create(ParallaxPath))
            {
                image.SaveAsPng(stream);
            }

            if (debugParallax)
            {
                var i = 0;
                foreach (var debugImage in debugImages)
                {
                    using (var stream = _resourceCache.UserData.Create(new ResourcePath($"/parallax_debug_{i}.png")))
                    {
                        debugImage.SaveAsPng(stream);
                    }

                    i += 1;
                }
            }

            image.Dispose();

            using (var stream = _resourceCache.UserData.Create(ParallaxConfigOld))
            using (var writer = new StreamWriter(stream, EncodingHelpers.UTF8))
            {
                writer.Write(contents);
            }

            OnTextureLoaded?.Invoke(ParallaxTexture);
        }
    }
}
