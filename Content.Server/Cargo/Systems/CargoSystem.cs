using Content.Server.Cargo.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Popups;
using Content.Server.Shuttles.Systems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem : SharedCargoSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly DeviceLinkSystem _linker = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<CargoSellBlacklistComponent> _blacklistQuery;
    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<TradeStationComponent> _tradeQuery;

    private HashSet<EntityUid> _setEnts = new();
    private List<EntityUid> _listEnts = new();
    private List<(EntityUid, CargoPalletComponent, TransformComponent)> _pads = new();

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        _blacklistQuery = GetEntityQuery<CargoSellBlacklistComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
        _tradeQuery = GetEntityQuery<TradeStationComponent>();

        InitializeConsole();
        InitializeShuttle();
        InitializeTelepad();
        InitializeBounty();
        InitializeFunds();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateConsole();
        UpdateTelepad(frameTime);
        UpdateBounty();
    }

    public void UpdateBankAccount(
        Entity<StationBankAccountComponent?> ent,
        int balanceAdded,
        ProtoId<CargoAccountPrototype> account,
        bool dirty = true)
    {
        UpdateBankAccount(
            ent,
            balanceAdded,
            new Dictionary<ProtoId<CargoAccountPrototype>, double> { {account, 1} },
            dirty: dirty);
    }

    /// <summary>
    /// Adds or removes funds from the <see cref="StationBankAccountComponent"/>.
    /// </summary>
    /// <param name="ent">The station.</param>
    /// <param name="balanceAdded">The amount of funds to add or remove.</param>
    /// <param name="accountDistribution">The distribution between individual <see cref="CargoAccountPrototype"/>.</param>
    /// <param name="dirty">Whether to mark the bank account component as dirty.</param>
    [PublicAPI]
    public void UpdateBankAccount(
        Entity<StationBankAccountComponent?> ent,
        int balanceAdded,
        Dictionary<ProtoId<CargoAccountPrototype>, double> accountDistribution,
        bool dirty = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        foreach (var (account, percent) in accountDistribution)
        {
            var accountBalancedAdded = (int) Math.Round(percent * balanceAdded);
            ent.Comp.Accounts[account] += accountBalancedAdded;
        }

        var ev = new BankBalanceUpdatedEvent(ent, ent.Comp.Accounts);
        RaiseLocalEvent(ent, ref ev, true);

        if (!dirty)
            return;

        Dirty(ent);
    }
}
