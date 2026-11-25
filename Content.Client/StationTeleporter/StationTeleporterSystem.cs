using Content.Shared.StationTeleporter;
using Content.Shared.StationTeleporter.Components;
using Robust.Client.GameObjects;

namespace Content.Client.StationTeleporter;

/// <inheritdoc/>
public sealed partial class StationTeleporterSystem : SharedStationTeleporterSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChanged(Entity<StationTeleporterComponent> ent, ref AppearanceChangeEvent args)
    {
        if (ent.Comp.PortalLayerMap is null)
            return;

        if (!args.TryGetData<Color>(TeleporterPortalVisuals.Color, out var newColor))
            return;

        if (!_sprite.LayerMapTryGet(ent.Owner, ent.Comp.PortalLayerMap, out var index, false))
            return;

        _sprite.LayerSetColor(ent.Owner, index, newColor);
    }
}
