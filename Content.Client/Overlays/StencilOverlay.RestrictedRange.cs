using System.Numerics;
using Content.Shared.Salvage;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.Overlays;

public sealed partial class StencilOverlay
{
    private void DrawRestrictedRange(in OverlayDrawArgs args, RestrictedRangeComponent rangeComp, Matrix3x2 invMatrix)
    {
        var worldHandle = args.WorldHandle;
        var renderScale = args.Viewport.RenderScale.X;
        // TODO: This won't handle non-standard zooms so uhh yeah, not sure how to structure it on the shader side.
        var zoom = args.Viewport.Eye?.Zoom ?? Vector2.One;
        var length = zoom.X;
        var bufferRange = MathF.Min(10f, rangeComp.Range);

        var pixelCenter = Vector2.Transform(rangeComp.Origin, invMatrix);
        // Something something offset?
        var vertical = args.Viewport.Size.Y;

        var pixelMaxRange = rangeComp.Range * renderScale / length * EyeManager.PixelsPerMeter;
        var pixelBufferRange = bufferRange * renderScale / length * EyeManager.PixelsPerMeter;
        var pixelMinRange = pixelMaxRange - pixelBufferRange;

        _shader.SetParameter("position", new Vector2(pixelCenter.X, vertical - pixelCenter.Y));
        _shader.SetParameter("maxRange", pixelMaxRange);
        _shader.SetParameter("minRange", pixelMinRange);
        _shader.SetParameter("bufferRange", pixelBufferRange);
        _shader.SetParameter("gradient", 0.80f);

        var worldAABB = args.WorldAABB;
        var worldBounds = args.WorldBounds;
        var position = args.Viewport.Eye?.Position.Position ?? Vector2.Zero;
        var localAABB = invMatrix.TransformBox(worldAABB);

        // Cut out the irrelevant bits via stencil
        // This is why we don't just use parallax; we might want specific tiles to get drawn over
        // particularly for planet maps or stations.
        worldHandle.RenderInRenderTarget(_blep!, () =>
        {
            worldHandle.UseShader(_shader);
            worldHandle.DrawRect(localAABB, Color.White);
        }, Color.Transparent);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_protoManager.Index<ShaderPrototype>("StencilMask").Instance());
        worldHandle.DrawTextureRect(_blep!.Texture, worldBounds);
        var curTime = _timing.RealTime;
        var sprite = _sprite.GetFrame(new SpriteSpecifier.Texture(new ResPath("/Textures/Parallaxes/noise.png")), curTime);

        // Draw the rain
        worldHandle.UseShader(_protoManager.Index<ShaderPrototype>("StencilDraw").Instance());
        _parallax.DrawParallax(worldHandle, worldAABB, sprite, curTime, position, new Vector2(0.5f, 0f));
    }
}
