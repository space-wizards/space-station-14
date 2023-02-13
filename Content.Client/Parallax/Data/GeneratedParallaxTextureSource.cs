using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nett;
using Content.Shared.CCVar;
using Content.Client.IoC;
using Robust.Client.Graphics;
using Robust.Shared.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Parallax.Data;

[UsedImplicitly]
[DataDefinition]
public sealed class GeneratedParallaxTextureSource : IParallaxTextureSource
{
    /// <summary>
    /// Parallax config path (the TOML file).
    /// In client resources.
    /// </summary>
    [DataField("configPath")]
    public ResourcePath ParallaxConfigPath { get; } = new("/parallax_config.toml");

    /// <summary>
    /// ID for debugging, caching, and so forth.
    /// The empty string here is reserved for the original parallax.
    /// It is advisible to provide a roughly unique ID for any unique config contents.
    /// </summary>
    [DataField("id")]
    public string Identifier { get; } = "other";

    /// <summary>
    /// Cached path.
    /// In user directory.
    /// </summary>
    private ResourcePath ParallaxCachedImagePath => new($"/parallax_{Identifier}cache.png");

    /// <summary>
    /// Old parallax config path (for checking for parallax updates).
    /// In user directory.
    /// </summary>
    private ResourcePath PreviousParallaxConfigPath => new($"/parallax_{Identifier}config_old");

    async Task<Texture> IParallaxTextureSource.GenerateTexture(CancellationToken cancel)
    {
        var parallaxConfig = GetParallaxConfig();
        if (parallaxConfig == null)
        {
            Logger.ErrorS("parallax", $"Parallax config not found or unreadable: {ParallaxConfigPath}");
            // The show must go on.
            return Texture.Transparent;
        }

        var debugParallax = IoCManager.Resolve<IConfigurationManager>().GetCVar(CCVars.ParallaxDebug);

        if (debugParallax
            || !StaticIoC.ResC.UserData.TryReadAllText(PreviousParallaxConfigPath, out var previousParallaxConfig)
            || previousParallaxConfig != parallaxConfig)
        {
            var table = Toml.ReadString(parallaxConfig);
            await UpdateCachedTexture(table, debugParallax, cancel);

            //Update the previous config
            using var writer = StaticIoC.ResC.UserData.OpenWriteText(PreviousParallaxConfigPath);
            writer.Write(parallaxConfig);
        }

        try
        {
            return GetCachedTexture();
        }
        catch (Exception ex)
        {
            Logger.ErrorS("parallax", $"Couldn't retrieve parallax cached texture: {ex}");

            try
            {
                // Also try to at least sort of fix this if we've been fooled by a config backup
                StaticIoC.ResC.UserData.Delete(PreviousParallaxConfigPath);
            }
            catch (Exception)
            {
                // The show must go on.
            }
            return Texture.Transparent;
        }
    }

    private async Task UpdateCachedTexture(TomlTable config, bool saveDebugLayers, CancellationToken cancel = default)
    {
        var debugImages = saveDebugLayers ? new List<Image<Rgba32>>() : null;

        var sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("parallax");

        // Generate the parallax in the thread pool.
        using var newParallexImage = await Task.Run(() =>
            ParallaxGenerator.GenerateParallax(config, new Size(1920, 1080), sawmill, debugImages, cancel), cancel);

        // And load it in the main thread for safety reasons.
        // But before spending time saving it, make sure to exit out early if it's not wanted.
        cancel.ThrowIfCancellationRequested();

        // Store it and CRC so further game starts don't need to regenerate it.
        using var imageStream = StaticIoC.ResC.UserData.OpenWrite(ParallaxCachedImagePath);
        newParallexImage.SaveAsPng(imageStream);

        if (saveDebugLayers)
        {
            for (var i = 0; i < debugImages!.Count; i++)
            {
                var debugImage = debugImages[i];
                using var debugImageStream = StaticIoC.ResC.UserData.OpenWrite(new ResourcePath($"/parallax_{Identifier}debug_{i}.png"));
                debugImage.SaveAsPng(debugImageStream);
            }
        }
    }

    private Texture GetCachedTexture()
    {
        using var imageStream = StaticIoC.ResC.UserData.OpenRead(ParallaxCachedImagePath);
        return Texture.LoadFromPNGStream(imageStream, "Parallax");
    }

    private string? GetParallaxConfig()
    {
        if (!StaticIoC.ResC.TryContentFileRead(ParallaxConfigPath, out var configStream))
        {
            return null;
        }

        using var configReader = new StreamReader(configStream, EncodingHelpers.UTF8);
        return configReader.ReadToEnd().Replace(Environment.NewLine, "\n");
    }
}

