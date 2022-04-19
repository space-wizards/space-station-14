using System;
using Content.Client.Parallax.Managers;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Parallax;

public sealed class ParallaxOverlay : Overlay
{
    [Dependency] private readonly IParallaxManager _parallaxManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;
    private readonly ShaderInstance _shader;

    public ParallaxOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
        {
            return;
        }

        var screenHandle = args.WorldHandle;
        screenHandle.UseShader(_shader);

        foreach (var layer in _parallaxManager.ParallaxLayers)
        {
            var tex = layer.Texture;

            // Size of the texture in world units.
            var size = (tex.Size / (float) EyeManager.PixelsPerMeter) * layer.Config.Scale;

            // Origin
            var originBL = args.Viewport.Eye.Position.Position * layer.Config.Slowness;

            // Centre around (WorldHomePosition + ParallaxAnchor).
            // The ParallaxAnchor adapts the parallax for station positioning and possibly map-specific tweaks.
            originBL += (layer.Config.WorldHomePosition + _parallaxManager.ParallaxAnchor) - (size / 2);

            if (layer.Config.Tiled)
            {
                // Remove offset so we can floor.
                var flooredBL = args.WorldAABB.BottomLeft - originBL;

                // Floor to background size.
                flooredBL = (flooredBL / size).Floored() * size;

                // Re-offset.
                flooredBL += originBL;

                for (var x = flooredBL.X; x < args.WorldAABB.Right; x += size.X)
                {
                    for (var y = flooredBL.Y; y < args.WorldAABB.Top; y += size.Y)
                    {
                        screenHandle.DrawTextureRect(tex, Box2.FromDimensions((x, y), size));
                    }
                }
            }
            else
            {
                screenHandle.DrawTextureRect(tex, Box2.FromDimensions(originBL, size));
            }
        }
    }
}

