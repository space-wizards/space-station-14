using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Light;

/// <summary>
/// Handles an enlarged lighting target so content can use large blur radii.
/// </summary>
public sealed class BeforeLightTargetOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    [Dependency] private readonly IClyde _clyde = default!;

    public IRenderTexture EnlargedLightTarget = default!;
    public Box2Rotated EnlargedBounds;

    /// <summary>
    /// In metres
    /// </summary>
    private float _skirting = 1.5f;

    public const int ContentZIndex = -10;

    public BeforeLightTargetOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = ContentZIndex;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // Code is weird but I don't think engine should be enlarging the lighting render target arbitrarily either, maybe via cvar?
        // The problem is the blur has no knowledge of pixels outside the viewport so with a large enough blur radius you get sampling issues.
        var size = args.Viewport.LightRenderTarget.Size + (int) (_skirting * EyeManager.PixelsPerMeter);
        EnlargedBounds = args.WorldBounds.Enlarged(_skirting / 2f);

        // This just exists to copy the lightrendertarget and write back to it.
        if (EnlargedLightTarget?.Size != size)
        {
            EnlargedLightTarget = _clyde
                .CreateRenderTarget(size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "enlarged-light-copy");
        }

        args.WorldHandle.RenderInRenderTarget(EnlargedLightTarget,
            () =>
            {
            }, _clyde.GetClearColor(args.MapUid));
    }
}
