using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class OrderLogCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private ServerStationSystem _stationSystem = default!;
    [Dependency] private CargoSystem _cargoSystem = default!;

    private static readonly EntProtoId CartridgePrototypeName = "CrewManifestCartridge";

    [SubscribeLocalEvent]
    private void OnInstallationAttempt(ref ProgramInstallationAttempt args)
    {
        if (args.Prototype == CartridgePrototypeName)
            args.Cancelled = true;
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    /// <remarks>
    /// The cartridge specific ui message event needs to inherit from the CartridgeMessageEvent
    /// </remarks>
    [SubscribeLocalEvent]
    private void OnUiMessage(EntityUid uid, OrderLogCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not OrderLogUiMessageEvent message)
            return;

        switch (message.Action)
        {
            case OrderLogUiAction.NotificationSwitch:
                component.NotificationOn = !component.NotificationOn;
                break;
        }

        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUiReady(EntityUid uid, OrderLogCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    [SubscribeLocalEvent]
    private void OnNewOrderAdded(Entity<OrderLogCartridgeComponent> ent, ref NewOrderAddedEvent args)
    {
        if (Comp<CartridgeComponent>(ent).LoaderUid is not { } loaderUid)
            return;

        UpdateUiState(ent, loaderUid, ent.Comp);

        if (!ent.Comp.NotificationOn)
            return;

        _cartridgeLoader.SendNotification(
            loaderUid,
            Loc.GetString("order-log-notification-header"),
            args.Message);
    }

    public void UpdateUiState(EntityUid uid, EntityUid loaderUid, OrderLogCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var station = _stationSystem.GetOwningStation(uid);

        if (station is null)
        {
            _cartridgeLoader.UpdateCartridgeUiState(loaderUid, new OrderLogUiState([], component.NotificationOn));
            return;
        }

        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase)
            || !TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        var state = new OrderLogUiState(
            _cargoSystem.RelevantOrders((station.Value, orderDatabase), bank.PrimaryAccount, approved: false).ToArray(),
            component.NotificationOn
            );
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }
}
