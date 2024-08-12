using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.StationAi;

public sealed class StationAiOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private IRenderTexture? _staticTexture;

    public IRenderTexture? _blep;

    public StationAiOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_blep != null)
        {
            var worldHandle = args.WorldHandle;

            /*
             * TODO:
             * Wall shadows still fucked
             * Need to draw it over the wall layer or whatever, can't just turn FOV off because that will break stuff
             */

            // Stencil
            //worldHandle.SetTransform(Matrix3x2.Identity);

            var worldAabb = args.WorldAABB;
            var worldBounds = args.WorldBounds;

            // Use the lighting as a mask
            worldHandle.SetTransform(Matrix3x2.Identity);
            worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilMask").Instance());
            worldHandle.DrawTextureRect(_blep!.Texture, worldBounds);

            // Draw the static
            worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilDraw").Instance());
            worldHandle.DrawTextureRect(Texture.White, worldBounds);

            worldHandle.SetTransform(Matrix3x2.Identity);
            worldHandle.UseShader(null);
        }

        // Will be a frame out of sync but makes sure we don't have issues with data not being ready after lighting.
        if (_blep?.Texture.Size != args.Viewport.Size)
        {
            _staticTexture?.Dispose();
            _blep?.Dispose();
            _blep = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "station-ai-stencil");
            _staticTexture = _clyde.CreateRenderTarget(args.Viewport.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: "station-ai-static");
        }
    }
}
