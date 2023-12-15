using Content.Server.Store.Components;
using Content.Server.UserInterface;
using Content.Shared.FixedPoint;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Store;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Store.Systems;

/// <summary>
/// Manages general interactions with a store and different entities,
/// getting listings for stores, and interfacing with the store UI.
/// </summary>
public sealed partial class StoreSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CurrencyComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<StoreComponent, BeforeActivatableUIOpenEvent>(BeforeActivatableUiOpen);

        SubscribeLocalEvent<StoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StoreComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoreComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StoreComponent, OpenUplinkImplantEvent>(OnImplantActivate);

        InitializeUi();
        InitializeCommand();
    }

    private void OnMapInit(EntityUid uid, StoreComponent component, MapInitEvent args)
    {
        RefreshAllListings(component);
        InitializeFromPreset(component.Preset, uid, component);
    }

    private void OnStartup(EntityUid uid, StoreComponent component, ComponentStartup args)
    {
        // for traitors, because the StoreComponent for the PDA can be added at any time.
        if (MetaData(uid).EntityLifeStage == EntityLifeStage.MapInitialized)
        {
            RefreshAllListings(component);
            InitializeFromPreset(component.Preset, uid, component);
        }

        var ev = new StoreAddedEvent();
        RaiseLocalEvent(uid, ref ev, true);
    }

    private void OnShutdown(EntityUid uid, StoreComponent component, ComponentShutdown args)
    {
        var ev = new StoreRemovedEvent();
        RaiseLocalEvent(uid, ref ev, true);
    }

    private void OnAfterInteract(EntityUid uid, CurrencyComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!TryComp<StoreComponent>(args.Target, out var store))
            return;

        var ev = new CurrencyInsertAttemptEvent(args.User, args.Target.Value, args.Used, store);
        RaiseLocalEvent(args.Target.Value, ev);
        if (ev.Cancelled)
            return;

        args.Handled = TryAddCurrency(GetCurrencyValue(uid, component), args.Target.Value, store);

        if (args.Handled)
        {
            var msg = Loc.GetString("store-currency-inserted", ("used", args.Used), ("target", args.Target));
            _popup.PopupEntity(msg, args.Target.Value);
            QueueDel(args.Used);
        }
    }

    private void OnImplantActivate(EntityUid uid, StoreComponent component, OpenUplinkImplantEvent args)
    {
        ToggleUi(args.Performer, uid, component);
    }

    /// <summary>
    /// Gets the value from an entity's currency component.
    /// Scales with stacks.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns>The value of the currency</returns>
    public Dictionary<string, FixedPoint2> GetCurrencyValue(EntityUid uid, CurrencyComponent component)
    {
        var amount = EntityManager.GetComponentOrNull<StackComponent>(uid)?.Count ?? 1;
        return component.Price.ToDictionary(v => v.Key, p => p.Value * amount);
    }

    /// <summary>
    /// Tries to add a currency to a store's balance.
    /// </summary>
    /// <param name="currencyEnt"></param>
    /// <param name="storeEnt"></param>
    /// <param name="currency">The currency to add</param>
    /// <param name="store">The store to add it to</param>
    /// <returns>Whether or not the currency was succesfully added</returns>
    [PublicAPI]
    public bool TryAddCurrency(EntityUid currencyEnt, EntityUid storeEnt, StoreComponent? store = null, CurrencyComponent? currency = null)
    {
        if (!Resolve(currencyEnt, ref currency) || !Resolve(storeEnt, ref store))
            return false;
        return TryAddCurrency(GetCurrencyValue(currencyEnt, currency), storeEnt, store);
    }

    /// <summary>
    /// Tries to add a currency to a store's balance
    /// </summary>
    /// <param name="currency">The value to add to the store</param>
    /// <param name="uid"></param>
    /// <param name="store">The store to add it to</param>
    /// <returns>Whether or not the currency was succesfully added</returns>
    public bool TryAddCurrency(Dictionary<string, FixedPoint2> currency, EntityUid uid, StoreComponent? store = null)
    {
        if (!Resolve(uid, ref store))
            return false;

        //verify these before values are modified
        foreach (var type in currency)
        {
            if (!store.CurrencyWhitelist.Contains(type.Key))
                return false;
        }

        foreach (var type in currency)
        {
            if (!store.Balance.TryAdd(type.Key, type.Value))
                store.Balance[type.Key] += type.Value;
        }

        UpdateUserInterface(null, uid, store);
        return true;
    }

    /// <summary>
    /// Initializes a store based on a preset ID
    /// </summary>
    /// <param name="preset">The ID of a store preset prototype</param>
    /// <param name="uid"></param>
    /// <param name="component">The store being initialized</param>
    public void InitializeFromPreset(string? preset, EntityUid uid, StoreComponent component)
    {
        if (preset == null)
            return;

        if (!_proto.TryIndex<StorePresetPrototype>(preset, out var proto))
            return;

        InitializeFromPreset(proto, uid, component);
    }

    /// <summary>
    /// Initializes a store based on a given preset
    /// </summary>
    /// <param name="preset">The StorePresetPrototype</param>
    /// <param name="uid"></param>
    /// <param name="component">The store being initialized</param>
    public void InitializeFromPreset(StorePresetPrototype preset, EntityUid uid, StoreComponent component)
    {
        component.Preset = preset.ID;
        component.CurrencyWhitelist.UnionWith(preset.CurrencyWhitelist);
        component.Categories.UnionWith(preset.Categories);
        if (component.Balance == new Dictionary<string, FixedPoint2>() && preset.InitialBalance != null) //if we don't have a value stored, use the preset
            TryAddCurrency(preset.InitialBalance, uid, component);

        var ui = _ui.GetUiOrNull(uid, StoreUiKey.Key);
        if (ui != null)
        {
            _ui.SetUiState(ui, new StoreInitializeState(preset.StoreName));
        }
    }
}

public sealed class CurrencyInsertAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Target;
    public readonly EntityUid Used;
    public readonly StoreComponent Store;

    public CurrencyInsertAttemptEvent(EntityUid user, EntityUid target, EntityUid used, StoreComponent store)
    {
        User = user;
        Target = target;
        Used = used;
        Store = store;
    }
}
