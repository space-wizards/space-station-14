using System.Numerics;
using Content.Shared.Salvage;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Salvage.UI;

public sealed class RestrictedRangeOverlay : Overlay
{
    private readonly IClyde _clyde;
    private readonly IEntityManager _entManager;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _protoManager;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private ShaderInstance _shader;

    public RestrictedRangeOverlay(IClyde clyde, IEntityManager entManager, IMapManager mapManager, IPrototypeManager protoManager)
    {
        _clyde = clyde;
        _entManager = entManager;
        _mapManager = mapManager;
        _protoManager = protoManager;
        _shader = _protoManager.Index<ShaderPrototype>("WorldGradientCircle").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return false;

        var mapUid = _mapManager.GetMapEntityId(args.MapId);

        if (!_entManager.HasComponent<RestrictedRangeComponent>(mapUid))
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mapUid = _mapManager.GetMapEntityId(args.MapId);
        var rangeComp = _entManager.GetComponent<RestrictedRangeComponent>(mapUid);
        var worldHandle = args.WorldHandle;
        var renderScale = args.Viewport.RenderScale.X;
        // TODO: This won't handle non-standard zooms so uhh yeah, not sure how to structure it on the shader side.
        var zoom = args.Viewport.Eye?.Zoom ?? Vector2.One;
        var length = zoom.X;
        var bufferRange = MathF.Min(10f, rangeComp.Range);

        var viewMatrix = args.Viewport.GetWorldToLocalMatrix();
        var pixelCenter = viewMatrix.Transform(Vector2.Zero);
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

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldAABB, Color.Black);

        worldHandle.UseShader(null);
    }
}
