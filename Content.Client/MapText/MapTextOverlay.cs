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
    private readonly IResourceCache _resourceCache;
    private readonly IPrototypeManager _prototypeManager;
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
        _resourceCache = resourceCache;
        _prototypeManager = prototypeManager;
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

        while(query.MoveNext(out var uid, out var mapText))
        {
            var mapPos = _transform.GetMapCoordinates(uid);

            if (mapPos.MapId != args.MapId)
                continue;

            if (!args.WorldBounds.Contains(mapPos.Position))
                continue;

            var fontPrototype = _prototypeManager.Index<FontPrototype>(mapText.FontId);
            var fontResource = _resourceCache.GetResource<FontResource>(fontPrototype.Path);
            var font = new VectorFont(fontResource, mapText.FontSize);

            var pos = Vector2.Transform(mapPos.Position, matrix) + mapText.Offset;
            var dimensions = handle.GetDimensions(font, mapText.Text, scale);
            handle.DrawString(font, pos - dimensions / 2f, mapText.Text, scale, mapText.Color);
        }
    }
}
