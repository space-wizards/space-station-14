using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Content.Shared;
using Content.Shared.CCVar;
using Nett;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Parallax.Managers
{
    internal sealed class ParallaxManager : IParallaxManager
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private static readonly ResourcePath ParallaxConfigPath = new("/parallax_config.toml");

        // Both of these below are in the user directory.
        private static readonly ResourcePath ParallaxCachedImagePath = new("/parallax_cache.png");
        private static readonly ResourcePath PreviousParallaxConfigPath = new("/parallax_config_old");

        public event Action<Texture>? OnTextureLoaded;
        public Texture? ParallaxTexture { get; private set; }

        public async void LoadParallax()
        {
            if (!_configurationManager.GetCVar(CCVars.ParallaxEnabled))
                return;

            var parallaxConfig = GetParallaxConfig();
            if (parallaxConfig == null)
                return;

            var debugParallax = _configurationManager.GetCVar(CCVars.ParallaxDebug);

            if (debugParallax
                || !_resourceCache.UserData.TryReadAllText(PreviousParallaxConfigPath, out var previousParallaxConfig)
                || previousParallaxConfig != parallaxConfig)
            {
                var table = Toml.ReadString(parallaxConfig);
                await UpdateCachedTexture(table, debugParallax);

                //Update the previous config
                using var writer = _resourceCache.UserData.OpenWriteText(PreviousParallaxConfigPath);
                writer.Write(parallaxConfig);
            }

            ParallaxTexture = GetCachedTexture();
            OnTextureLoaded?.Invoke(ParallaxTexture);
        }

        private async Task UpdateCachedTexture(TomlTable config, bool saveDebugLayers)
        {
            var debugImages = saveDebugLayers ? new List<Image<Rgba32>>() : null;

            var sawmill = _logManager.GetSawmill("parallax");
            // Generate the parallax in the thread pool.
            using var newParallexImage = await Task.Run(() =>
                ParallaxGenerator.GenerateParallax(config, new Size(1920, 1080), sawmill, debugImages));
            // And load it in the main thread for safety reasons.

            // Store it and CRC so further game starts don't need to regenerate it.
            using var imageStream = _resourceCache.UserData.OpenWrite(ParallaxCachedImagePath);
            newParallexImage.SaveAsPng(imageStream);

            if (saveDebugLayers)
            {
                for (var i = 0; i < debugImages!.Count; i++)
                {
                    var debugImage = debugImages[i];
                    using var debugImageStream = _resourceCache.UserData.OpenWrite(new ResourcePath($"/parallax_debug_{i}.png"));
                    debugImage.SaveAsPng(debugImageStream);
                }
            }
        }

        private Texture GetCachedTexture()
        {
            using var imageStream = _resourceCache.UserData.OpenRead(ParallaxCachedImagePath);
            return Texture.LoadFromPNGStream(imageStream, "Parallax");
        }

        private string? GetParallaxConfig()
        {
            if (!_resourceCache.TryContentFileRead(ParallaxConfigPath, out var configStream))
            {
                Logger.ErrorS("parallax", "Parallax config not found.");
                return null;
            }

            using var configReader = new StreamReader(configStream, EncodingHelpers.UTF8);
            return configReader.ReadToEnd().Replace(Environment.NewLine, "\n");
        }
    }
}
