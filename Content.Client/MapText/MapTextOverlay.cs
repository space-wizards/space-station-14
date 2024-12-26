using System.Numerics;
using Content.Shared.MapText;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.MapText;

/// <summary>
/// Draws map text as an overlay
/// </summary>
public sealed class MapTextOverlay : Overlay
{
    private readonly IConfigurationManager _configManager;
    private readonly IEntityManager _entManager;
    private readonly IUserInterfaceManager _uiManager;
    private readonly SharedTransformSystem _transform;
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public MapTextOverlay(
        IConfigurationManager configManager,
        IEntityManager entManager,
        IUserInterfaceManager uiManager,
        SharedTransformSystem transform,
        IResourceCache resourceCache,
        IPrototypeManager prototypeManager)
    {
        _configManager = configManager;
        _entManager = entManager;
        _uiManager = uiManager;
        _transform = transform;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        args.DrawingHandle.SetTransform(Matrix3x2.Identity);

        var scale = _configManager.GetCVar(CVars.DisplayUIScale);

        if (scale == 0f)
            scale = _uiManager.DefaultUIScale;

        DrawWorld(args.ScreenHandle, args, scale);

        args.DrawingHandle.UseShader(null);
    }

    private void DrawWorld(DrawingHandleScreen handle, OverlayDrawArgs args, float scale)
    {
        if ( args.ViewportControl == null)
            return;

        var matrix = args.ViewportControl.GetWorldToScreenMatrix();
        var query = _entManager.AllEntityQueryEnumerator<MapTextComponent>();

        // Enlarge bounds to try prevent pop-in due to large text.
        var bounds = args.WorldBounds.Enlarged(2);

        while(query.MoveNext(out var uid, out var mapText))
        {
            var mapPos = _transform.GetMapCoordinates(uid);

            if (mapPos.MapId != args.MapId)
                continue;

            if (!bounds.Contains(mapPos.Position))
                continue;

            if (mapText.CachedFont == null)
                continue;

            var pos = Vector2.Transform(mapPos.Position, matrix) + mapText.Offset;
            var dimensions = handle.GetDimensions(mapText.CachedFont, mapText.CachedText, scale);
            handle.DrawString(mapText.CachedFont, pos - dimensions / 2f, mapText.CachedText, scale, mapText.Color);
        }
    }
}
