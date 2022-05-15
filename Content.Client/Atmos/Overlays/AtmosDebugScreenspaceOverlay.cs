using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.Atmos.Overlays;

public sealed class AtmosDebugScreenspaceOverlay : Overlay
{
    private readonly AtmosDebugOverlaySystem _atmosDebugOverlaySystem;

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    private readonly VectorFont _font;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public AtmosDebugScreenspaceOverlay()
    {
        IoCManager.InjectDependencies(this);

        _atmosDebugOverlaySystem = EntitySystem.Get<AtmosDebugOverlaySystem>();
        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_atmosDebugOverlaySystem.CfgMode != AtmosDebugOverlayMode.Everything)
            return;

        var mapId = args.Viewport.Eye!.Position.MapId;
        var worldBounds = args.WorldBounds;

        foreach (var mapGrid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
        {
            if (!_atmosDebugOverlaySystem.HasData(mapGrid.Index))
                continue;

            foreach (var tile in mapGrid.GetTilesIntersecting(worldBounds))
            {
                var dataMaybeNull = _atmosDebugOverlaySystem.GetData(mapGrid.Index, tile.GridIndices);

                if (!dataMaybeNull.HasValue) continue;
                var data = dataMaybeNull.Value;

                var tileCenterScreenCoords = _eyeManager.MapToScreen(mapGrid.GridTileToWorld(tile.GridIndices));
                var drawToScreenCoords = tileCenterScreenCoords.Position + new Vector2(-55f, 30f);

                var totalMoles = 0f;
                var overScale = false;

                var topValueToShow = _atmosDebugOverlaySystem.CfgScale + _atmosDebugOverlaySystem.CfgBase;
                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    totalMoles += data.Moles[i];
                    if (data.Moles[i] > topValueToShow)
                    {
                        topValueToShow = data.Moles[i];
                        overScale = true;
                    }
                }

                const float lineHeight = 11f;
                if (totalMoles != 0f)
                {
                    args.ScreenHandle.DrawString(_font, drawToScreenCoords, totalMoles.ToString("0.## E-0"), overScale ? Color.Red : Color.White);
                }

                var tempColor = data.Temperature switch
                {
                    > Atmospherics.T0C + 50 => Color.Red,
                    < Atmospherics.T0C => Color.Aqua,
                    _ => Color.White
                };
                args.ScreenHandle.DrawString(_font, drawToScreenCoords - new Vector2(0f, lineHeight),
                    (data.Temperature - Atmospherics.T0C).ToString("0.# C"), tempColor);

                if (data.InExcitedGroup || data.IsHotspot)
                {
                    var line = (data.InExcitedGroup ? (data.IsHotspot ? "EXC HOT" : "EXC") : "HOT");
                    var color = data.IsHotspot ? Color.OrangeRed : Color.Yellow;
                    args.ScreenHandle.DrawString(_font, drawToScreenCoords - new Vector2(0f, lineHeight * 2), line, color);
                }

            }
        }
    }
}
