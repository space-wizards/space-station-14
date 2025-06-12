using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client.Light;

public sealed class AmbientOcclusionOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private IRenderTexture? _aoTarget;

    public AmbientOcclusionOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = AfterLightTargetOverlay.ContentZIndex + 1;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.Viewport;
        var mapId = args.MapId;
        var worldBounds = args.WorldBounds;
        var worldHandle = args.WorldHandle;
        var color = Color.FromHex("#04080FAA");
        //var color = Color.Red;
        var target = viewport.RenderTarget;
        var lightScale = target.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);

        if (_aoTarget?.Texture.Size != target.Size)
        {
            _aoTarget?.Dispose();
            _aoTarget = _clyde.CreateRenderTarget(target.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "ambient-occlusion-target");
        }

        args.WorldHandle.RenderInRenderTarget(_aoTarget,
            () =>
            {
                var invMatrix = _aoTarget.GetWorldToLocalMatrix(viewport.Eye!, scale);

                var query = _entManager.System<OccluderSystem>();
                var xformSystem = _entManager.System<SharedTransformSystem>();

                foreach (var entry in query.QueryAabb(mapId, worldBounds))
                {
                    DebugTools.Assert(entry.Component.Enabled);
                    var matrix = xformSystem.GetWorldMatrix(entry.Transform);
                    var localMatrix = Matrix3x2.Multiply(matrix, invMatrix);

                    worldHandle.SetTransform(localMatrix);
                    // 4 pixels
                    worldHandle.DrawRect(Box2.UnitCentered.Enlarged(4f / EyeManager.PixelsPerMeter), Color.White);
                }
            }, Color.Transparent);

        _clyde.BlurRenderTarget(viewport, _aoTarget, _aoTarget, viewport.Eye!, 14f);

        args.WorldHandle.RenderInRenderTarget(target,
            () =>
            {
                var localMatrix = target.GetWorldToLocalMatrix(viewport.Eye!, viewport.RenderScale);
                worldHandle.SetTransform(localMatrix);

                worldHandle.DrawTextureRect(_aoTarget.Texture, worldBounds, color);
            }, null);

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
