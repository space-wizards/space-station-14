using Content.Shared.GatewayStation;
using Robust.Client.GameObjects;

namespace Content.Client.GatewayStation;

public sealed class ClientStationGatewaySystem : SharedStationGatewaySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationGatewayComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<StationGatewayComponent> ent, ref AppearanceChangeEvent args)
    {
        if (ent.Comp.PortalLayerMap is null)
            return;

        if (!_appearance.TryGetData<Color>(ent, GatewayPortalVisual.Color, out var newColor))
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(ent.Comp.PortalLayerMap, out var index))
            return;

        sprite.LayerSetColor(index, newColor);
    }
}
