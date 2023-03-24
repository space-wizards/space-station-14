using System;
using System.Collections.Generic;
using System.IO;
using Content.Shared.Decals;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static Robust.UnitTesting.RobustIntegrationTest;

namespace Content.MapRenderer.Painters;

public sealed class DecalPainter
{
    private readonly IResourceCache _cResourceCache;

    private readonly IPrototypeManager _sPrototypeManager;

    private readonly Dictionary<string, SpriteSpecifier> _decalTextures = new();

    public DecalPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
    {
        _cResourceCache = client.ResolveDependency<IResourceCache>();
        _sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
    }

    public void Run(Image canvas, List<DecalData> decals)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        decals.Sort(Comparer<DecalData>.Create((x, y) => x.Decal.ZIndex.CompareTo(y.Decal.ZIndex)));

        if (_decalTextures.Count == 0)
        {
            foreach (var proto in _sPrototypeManager.EnumeratePrototypes<DecalPrototype>())
            {
                _decalTextures.Add(proto.ID, proto.Sprite);
            }
        }

        foreach (var decal in decals)
        {
            Run(canvas, decal);
        }

        Console.WriteLine($"{nameof(DecalPainter)} painted {decals.Count} decals in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
    }

    private void Run(Image canvas, DecalData data)
    {
        var decal = data.Decal;
        if (!_decalTextures.TryGetValue(decal.Id, out var sprite))
        {
            Console.WriteLine($"Decal {decal.Id} did not have an associated prototype.");
            return;
        }

        Stream stream;
        if (sprite is SpriteSpecifier.Texture texture)
        {
            stream = _cResourceCache.ContentFileRead(texture.TexturePath);
        }
        else if (sprite is SpriteSpecifier.Rsi rsi)
        {
            stream = _cResourceCache.ContentFileRead($"/Textures/{rsi.RsiPath}/{rsi.RsiState}.png");
        }
        else
        {
            // Don't support
            return;
        }

        var image = Image.Load<Rgba32>(stream);

        image.Mutate(o => o.Rotate((float) -decal.Angle.Degrees));
        var coloredImage = new Image<Rgba32>(image.Width, image.Height);
        Color color = decal.Color?.ConvertImgSharp() ?? Color.White;
        coloredImage.Mutate(o => o.BackgroundColor(color));

        image.Mutate(o => o
            .DrawImage(coloredImage, PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcAtop, 1.0f)
            .Flip(FlipMode.Vertical));

        // Very unsure why the - 1 is needed in the first place but all decals are off by exactly one pixel otherwise
        // Woohoo!
        canvas.Mutate(o => o.DrawImage(image, new Point((int) data.X, (int) data.Y - 1), 1.0f));
    }
}
