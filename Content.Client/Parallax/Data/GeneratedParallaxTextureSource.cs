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
using Robust.Shared.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client.Parallax.Data;

[UsedImplicitly]
[DataDefinition]
public sealed partial class GeneratedParallaxTextureSource : IParallaxTextureSource
{
    /// <summary>
    /// Parallax config path (the TOML file).
    /// In client resources.
    /// </summary>
    [DataField("configPath")]
    public ResPath ParallaxConfigPath { get; private set; } = new("/parallax_config.toml");

    /// <summary>
    /// ID for debugging, caching, and so forth.
    /// The empty string here is reserved for the original parallax.
    /// It is advisible to provide a roughly unique ID for any unique config contents.
    /// </summary>
    [DataField("id")]
    public string Identifier { get; private set; } = "other";

    /// <summary>
    /// Cached path.
    /// In user directory.
    /// </summary>
    private ResPath ParallaxCachedImagePath => new($"/parallax_{Identifier}cache.png");

    /// <summary>
    /// Old parallax config path (for checking for parallax updates).
    /// In user directory.
    /// </summary>
    private ResPath PreviousParallaxConfigPath => new($"/parallax_{Identifier}config_old");

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
        var resManager = IoCManager.Resolve<IResourceManager>();

        if (debugParallax
            || !resManager.UserData.TryReadAllText(PreviousParallaxConfigPath, out var previousParallaxConfig)
            || previousParallaxConfig != parallaxConfig)
        {
            var table = Toml.ReadString(parallaxConfig);
            await UpdateCachedTexture(table, debugParallax, cancel);

            //Update the previous config
            using var writer = resManager.UserData.OpenWriteText(PreviousParallaxConfigPath);
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
                resManager.UserData.Delete(PreviousParallaxConfigPath);
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
        var resManager = IoCManager.Resolve<IResourceManager>();

        // Store it and CRC so further game starts don't need to regenerate it.
        await using var imageStream = resManager.UserData.OpenWrite(ParallaxCachedImagePath);
        await newParallexImage.SaveAsPngAsync(imageStream, cancel);

        if (saveDebugLayers)
        {
            for (var i = 0; i < debugImages!.Count; i++)
            {
                var debugImage = debugImages[i];
                await using var debugImageStream = resManager.UserData.OpenWrite(new ResPath($"/parallax_{Identifier}debug_{i}.png"));
                await debugImage.SaveAsPngAsync(debugImageStream, cancel);
            }
        }
    }

    private Texture GetCachedTexture()
    {
        var resManager = IoCManager.Resolve<IResourceManager>();
        using var imageStream = resManager.UserData.OpenRead(ParallaxCachedImagePath);
        return Texture.LoadFromPNGStream(imageStream, "Parallax");
    }

    private string? GetParallaxConfig()
    {
        var resManager = IoCManager.Resolve<IResourceManager>();
        if (!resManager.TryContentFileRead(ParallaxConfigPath, out var configStream))
        {
            return null;
        }

        using var configReader = new StreamReader(configStream, EncodingHelpers.UTF8);
        return configReader.ReadToEnd().Replace(Environment.NewLine, "\n");
    }
}

