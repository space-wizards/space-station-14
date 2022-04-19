using Content.Client.Parallax.Managers;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;

namespace Content.Client.Parallax;

/// <summary>
///     Renders the parallax background as a UI control.
/// </summary>
public sealed class ParallaxControl : Control
{
    [Dependency] private readonly IParallaxManager _parallaxManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [ViewVariables(VVAccess.ReadWrite)] public Vector2i Offset { get; set; }

    public ParallaxControl()
    {
        IoCManager.InjectDependencies(this);

        Offset = (_random.Next(0, 1000), _random.Next(0, 1000));
        RectClipContent = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        foreach (var layer in _parallaxManager.ParallaxLayers)
        {
            var tex = layer.Texture;
            var ourSize = PixelSize;

            if (layer.Config.Tiled)
            {
                var scaledOffset = (Offset * layer.Config.Slowness).Floored();
                var size = tex.Size;

                for (var x = -size.X + scaledOffset.X; x < ourSize.X; x += size.X)
                {
                    for (var y = -size.Y + scaledOffset.Y; y < ourSize.Y; y += size.Y)
                    {
                        handle.DrawTexture(tex, (x, y));
                    }
                }
            }
            else
            {
                handle.DrawTexture(tex, (ourSize / 2) + layer.Config.ControlHomePosition);
            }
        }
    }
}

