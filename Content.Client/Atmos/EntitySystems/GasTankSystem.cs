using Content.Client.Atmos.UI;
using Content.Client.Items;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class GasTankSystem : SharedGasTankSystem
{
    public static readonly ProtoId<ItemStatusPrototype> GasTankItemStatus = "GasTank";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasTankComponent, AfterAutoHandleStateEvent>(OnGasTankState);

        Subs.ItemStatus<GasTankComponent>(entity => new TankPressureStatusControl(entity), GasTankItemStatus);
    }

    protected override void DeviceUpdated(Entity<GasTankComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        // Atmos not predicted :(
        throw new NotImplementedException();
    }

    private void OnGasTankState(Entity<GasTankComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (UI.TryGetOpenUi(ent.Owner, SharedGasTankUiKey.Key, out var bui))
        {
            bui.Update<GasTankBoundUserInterfaceState>();
        }
    }

    public override void UpdateUserInterface(Entity<GasTankComponent> ent)
    {
        if (UI.TryGetOpenUi(ent.Owner, SharedGasTankUiKey.Key, out var bui))
        {
            bui.Update<GasTankBoundUserInterfaceState>();
        }
    }
}
