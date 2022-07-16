using Content.Client.Parallax.Managers;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Parallax;

public sealed class ParallaxOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IParallaxManager _manager = default!;
    private readonly ParallaxSystem _parallax;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;
    private readonly ShaderInstance _shader;

    public ParallaxOverlay()
    {
        IoCManager.InjectDependencies(this);
        _parallax = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ParallaxSystem>();
        _shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        if (!_configurationManager.GetCVar(CCVars.ParallaxEnabled))
            return;

        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;
        var screenHandle = args.WorldHandle;
        screenHandle.UseShader(_shader);

        var layers = _parallax.GetParallaxLayers(args.MapId);

        foreach (var layer in layers)
        {
            var tex = layer.Texture;

            // Size of the texture in world units.
            var size = (tex.Size / (float) EyeManager.PixelsPerMeter) * layer.Config.Scale;

            // The "home" position is the effective origin of this layer.
            // Parallax shifting is relative to the home, and shifts away from the home and towards the Eye centre.
            // The effects of this are such that a slowness of 1 anchors the layer to the centre of the screen, while a slowness of 0 anchors the layer to the world.
            // (For values 0.0 to 1.0 this is in effect a lerp, but it's deliberately unclamped.)
            // The ParallaxAnchor adapts the parallax for station positioning and possibly map-specific tweaks.
            var home = layer.Config.WorldHomePosition + _manager.ParallaxAnchor;

            // Origin - start with the parallax shift itself.
            var originBL = (position - home) * layer.Config.Slowness;

            // Place at the home.
            originBL += home;

            // Adjust.
            originBL += layer.Config.WorldAdjustPosition;

            // Centre the image.
            originBL -= size / 2;

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

