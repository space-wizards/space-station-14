using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.CrewManifest;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CCVar;
using Content.Shared.CrewManifest;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class CargoAppCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private ServerStationSystem _stationSystem = default!;
    [Dependency] private CargoSystem _cargoSystem = default!;

    private static readonly EntProtoId CartridgePrototypeName = "CrewManifestCartridge";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CargoAppCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<CargoAppCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<ProgramInstallationAttempt>(OnInstallationAttempt);
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    /// <remarks>
    /// The cartridge specific ui message event needs to inherit from the CartridgeMessageEvent
    /// </remarks>
    private void OnUiMessage(EntityUid uid, CargoAppCartridgeComponent component, CartridgeMessageEvent args)
    {
        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, CargoAppCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    public void UpdateUiState(EntityUid uid, EntityUid loaderUid, CargoAppCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var station = _stationSystem.GetOwningStation(uid);

        if (station is null)
        {
            // Display "loading failed" message
            var failureMessage = Loc.GetString("crew-manifest-cartridge-loading-failed");
            var failureState = new CargoAppUiState(
                [],
                []
                );
            _cartridgeLoader.UpdateCartridgeUiState(loaderUid, failureState);
            return;
        }

        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase))
            return;

        if (!TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        var orderHistory = orderDatabase
            .DeliveredOrders.Concat(orderDatabase.Orders)
            .OrderBy(order => order.OrderId)
            .ToList();


        var state = new CargoAppUiState(
            _cargoSystem.RelevantOrders((station.Value, orderDatabase), orderDatabase.Orders, bank.PrimaryAccount, approved: false).ToArray(),
            _cargoSystem.RelevantOrders((station.Value, orderDatabase), orderHistory, bank.PrimaryAccount, approved: true).ToArray()
            );
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    private void OnInstallationAttempt(ref ProgramInstallationAttempt args)
    {
        if (args.Prototype == CartridgePrototypeName)
            args.Cancelled = true;
    }
}
