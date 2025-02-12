using System.Numerics;
using Content.Client.Parallax.Managers;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Parallax;

/// <summary>
///     Renders the parallax background as a UI control.
/// </summary>
public sealed class ParallaxControl : Control
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IParallaxManager _parallaxManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [ViewVariables(VVAccess.ReadWrite)] public Vector2 Offset { get; set; }

    public ParallaxControl()
    {
        IoCManager.InjectDependencies(this);

        Offset = new Vector2(_random.Next(0, 1000), _random.Next(0, 1000));
        RectClipContent = true;
        _parallaxManager.LoadParallaxByName("FastSpace");
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        foreach (var layer in _parallaxManager.GetParallaxLayers("FastSpace"))
        {
            var tex = layer.Texture;
            var texSize = (tex.Size.X * (int) Size.X, tex.Size.Y * (int) Size.X) * layer.Config.Scale.Floored() / 1920;
            var ourSize = PixelSize;

            var currentTime = (float) _timing.RealTime.TotalSeconds;
            var offset = Offset + new Vector2(currentTime * 100f, currentTime * 0f);

            if (layer.Config.Tiled)
            {
                // Multiply offset by slowness to match normal parallax
                var scaledOffset = (offset * layer.Config.Slowness).Floored();

                // Then modulo the scaled offset by the size to prevent drawing a bunch of offscreen tiles for really small images.
                scaledOffset.X %= texSize.X;
                scaledOffset.Y %= texSize.Y;

                // Note: scaledOffset must never be below 0 or there will be visual issues.
                // It could be allowed to be >= texSize on a given axis but that would be wasteful.

                for (var x = -scaledOffset.X; x < ourSize.X; x += texSize.X)
                {
                    for (var y = -scaledOffset.Y; y < ourSize.Y; y += texSize.Y)
                    {
                        handle.DrawTextureRect(tex, UIBox2.FromDimensions(new Vector2(x, y), texSize));
                    }
                }
            }
            else
            {
                var origin = ((ourSize - texSize) / 2) + layer.Config.ControlHomePosition;
                handle.DrawTextureRect(tex, UIBox2.FromDimensions(origin, texSize));
            }
        }
    }
}

