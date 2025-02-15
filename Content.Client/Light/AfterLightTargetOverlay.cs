using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Light;

/// <summary>
/// This exists just to copy <see cref="BeforeLightTargetOverlay"/> to the light render target
/// </summary>
public sealed class AfterLightTargetOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    [Dependency] private readonly IOverlayManager _overlay = default!;

    public const int ContentZIndex = LightBlurOverlay.ContentZIndex + 1;

    public AfterLightTargetOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = ContentZIndex;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.Viewport;
        var worldHandle = args.WorldHandle;

        if (viewport.Eye == null)
            return;

        var lightOverlay = _overlay.GetOverlay<BeforeLightTargetOverlay>();
        var bounds = args.WorldBounds;

        /*
         *  TODO:
         * - Need to make sure RoofOverlay drawn properly
         * - Need to make sure TileEmissionOverlay drawn properly
         * - LightBlurOverlay needs fixing for the enlarged bounds (whole reason we're doing this)
         * - Need to fix the below
         */

        // at 1-1 render scale it's mostly fine but at 4x4 it's way too fkn big
        var oldScale = viewport.RenderScale / 2f;
        var newScale = viewport.RenderScale / 2f;

        var oldMatrix =
            lightOverlay.EnlargedLightTarget.GetWorldToLocalMatrix(viewport.Eye, oldScale);

        args.WorldHandle.RenderInRenderTarget(lightOverlay.EnlargedLightTarget,
            () =>
            {
                worldHandle.SetTransform(oldMatrix);
                worldHandle.DrawRect(new Box2(Vector2.Zero, Vector2.One), Color.Green);

            }, null);

        var localMatrix =
            viewport.LightRenderTarget.GetWorldToLocalMatrix(viewport.Eye, newScale);
        var diff = (lightOverlay.EnlargedLightTarget.Size - viewport.LightRenderTarget.Size);
        var halfDiff = diff / 2;

        // Pixels -> Metres -> Half distance.
        // If we're zoomed in need to enlarge the bounds further.
        args.WorldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
            () =>
            {
                // We essentially need to draw the cropped version onto the lightrendertarget.
                var subRegion = new UIBox2i(halfDiff.X,
                    halfDiff.Y,
                    viewport.LightRenderTarget.Size.X + halfDiff.X,
                    viewport.LightRenderTarget.Size.Y + halfDiff.Y);

                worldHandle.SetTransform(localMatrix);
                worldHandle.DrawTextureRectRegion(lightOverlay.EnlargedLightTarget.Texture, bounds, subRegion: subRegion);
            }, null);
    }
}
