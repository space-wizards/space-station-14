using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Content.Shared.Decals;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.ContentPack;
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
    private readonly IResourceManager _resManager;

    private readonly IPrototypeManager _sPrototypeManager;

    private readonly Dictionary<string, SpriteSpecifier> _decalTextures = new();

    public DecalPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
    {
        _resManager = client.ResolveDependency<IResourceManager>();
        _sPrototypeManager = server.ResolveDependency<IPrototypeManager>();
    }

    public void Run(Image canvas, Span<DecalData> decals, Vector2 customOffset = default)
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
            Run(canvas, decal, customOffset);
        }

        Console.WriteLine($"{nameof(DecalPainter)} painted {decals.Length} decals in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
    }

    private void Run(Image canvas, DecalData data, Vector2 customOffset = default)
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
            stream = _resManager.ContentFileRead(texture.TexturePath);
        }
        else if (sprite is SpriteSpecifier.Rsi rsi)
        {
            var path = $"{rsi.RsiPath}/{rsi.RsiState}.png";
            if (!path.StartsWith("/Textures"))
            {
                path = $"/Textures/{path}";
            }

            stream = _resManager.ContentFileRead(path);
        }
        else
        {
            // Don't support
            return;
        }

        var image = Image.Load<Rgba32>(stream);

        image.Mutate(o => o.Rotate((float) -decal.Angle.Degrees));
        var coloredImage = new Image<Rgba32>(image.Width, image.Height);
        Color color = decal.Color?.WithAlpha(byte.MaxValue).ConvertImgSharp() ?? Color.White; // remove the encoded color alpha here
        var alpha = decal.Color?.A ?? 1; // get the alpha separately so we can use it in DrawImage
        coloredImage.Mutate(o => o.BackgroundColor(color));

        image.Mutate(o => o
            .DrawImage(coloredImage, PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcAtop, 1.0f)
            .Flip(FlipMode.Vertical));

        var pointX = (int) data.X + (int) (customOffset.X * EyeManager.PixelsPerMeter);
        var pointY = (int) data.Y + (int) (customOffset.Y * EyeManager.PixelsPerMeter);

        // Woohoo!
        canvas.Mutate(o => o.DrawImage(image, new Point(pointX, pointY), alpha));
    }
}
