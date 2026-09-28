using Content.Server.Cargo.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.IdentityManagement;
using Content.Shared.Labels.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem : SharedCargoSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private DeviceLinkSystem _linker = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private PaperSystem _paperSystem = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private ServerStationSystem _station = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private MetaDataSystem _metaSystem = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private IdentitySystem _identity = default!;

    [Dependency] private EntityQuery<CashComponent> _cashQuery;
    [Dependency] private EntityQuery<CargoOrderConsoleComponent> _consoleQuery;
    [Dependency] private EntityQuery<CargoSellBlacklistComponent> _cargoSellBlacklistQuery;
    [Dependency] private EntityQuery<CargoSlipComponent> _slipQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery;
    [Dependency] private EntityQuery<PaperComponent> _paperQuery;
    [Dependency] private EntityQuery<PaperLabelComponent> _paperLabelQuery;
    [Dependency] private EntityQuery<StationBankAccountComponent> _bankQuery;
    [Dependency] private EntityQuery<StationCargoOrderDatabaseComponent> _orderQuery;
    [Dependency] private EntityQuery<StationDataComponent> _stationQuery;
    [Dependency] private EntityQuery<TradeStationComponent> _tradeStationQuery;

    private HashSet<EntityUid> _setEnts = new();
    private List<EntityUid> _listEnts = new();
    private List<(EntityUid, CargoPalletComponent, TransformComponent)> _pads = new();

    public override void Initialize()
    {
        base.Initialize();
        InitializeShuttle();
        InitializeBounty();
        InitializeFunds();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateConsole();
        UpdateTelepad();
        UpdateBounty();
    }
}
