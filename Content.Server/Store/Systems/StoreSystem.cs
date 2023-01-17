using Content.Server.Stack;
using Content.Server.Store.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Store;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server.UserInterface;
using Content.Shared.Stacks;

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
        SubscribeLocalEvent<StoreComponent, BeforeActivatableUIOpenEvent>((_,c,a) => UpdateUserInterface(a.User, c));

        SubscribeLocalEvent<StoreComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoreComponent, ComponentShutdown>(OnShutdown);

        InitializeUi();
    }

    private void OnStartup(EntityUid uid, StoreComponent component, ComponentStartup args)
    {
        RaiseLocalEvent(uid, new StoreAddedEvent(), true);
    }

    private void OnShutdown(EntityUid uid, StoreComponent component, ComponentShutdown args)
    {
        RaiseLocalEvent(uid, new StoreRemovedEvent(), true);
    }

    private void OnAfterInteract(EntityUid uid, CurrencyComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target == null || !TryComp<StoreComponent>(args.Target, out var store))
            return;

        //if you somehow are inserting cash before the store initializes.
        if (!store.Opened)
        {
            RefreshAllListings(store);
            InitializeFromPreset(store.Preset, store);
            store.Opened = true;
        }

        args.Handled = TryAddCurrency(GetCurrencyValue(component), store);

        if (args.Handled)
        {
            var msg = Loc.GetString("store-currency-inserted", ("used", args.Used), ("target", args.Target));
            _popup.PopupEntity(msg, args.Target.Value);
            QueueDel(args.Used);
        }
    }

    /// <summary>
    /// Gets the value from an entity's currency component.
    /// Scales with stacks.
    /// </summary>
    /// <param name="component"></param>
    /// <returns>The value of the currency</returns>
    public Dictionary<string, FixedPoint2> GetCurrencyValue(CurrencyComponent component)
    {
        TryComp<StackComponent>(component.Owner, out var stack);
        var amount = stack?.Count ?? 1;

        return component.Price.ToDictionary(v => v.Key, p => p.Value * amount);
    }

    /// <summary>
    /// Tries to add a currency to a store's balance.
    /// </summary>
    /// <param name="component">The currency to add</param>
    /// <param name="store">The store to add it to</param>
    /// <returns>Whether or not the currency was succesfully added</returns>
    public bool TryAddCurrency(CurrencyComponent component, StoreComponent store)
    {
        return TryAddCurrency(GetCurrencyValue(component), store);
    }

    /// <summary>
    /// Tries to add a currency to a store's balance
    /// </summary>
    /// <param name="currency">The value to add to the store</param>
    /// <param name="store">The store to add it to</param>
    /// <returns>Whether or not the currency was succesfully added</returns>
    public bool TryAddCurrency(Dictionary<string, FixedPoint2> currency, StoreComponent store)
    {
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

        UpdateUserInterface(null, store);
        return true;
    }

    /// <summary>
    /// Initializes a store based on a preset ID
    /// </summary>
    /// <param name="preset">The ID of a store preset prototype</param>
    /// <param name="component">The store being initialized</param>
    public void InitializeFromPreset(string? preset, StoreComponent component)
    {
        if (preset == null)
            return;

        if (!_proto.TryIndex<StorePresetPrototype>(preset, out var proto))
            return;

        InitializeFromPreset(proto, component);
    }

    /// <summary>
    /// Initializes a store based on a given preset
    /// </summary>
    /// <param name="preset">The StorePresetPrototype</param>
    /// <param name="component">The store being initialized</param>
    public void InitializeFromPreset(StorePresetPrototype preset, StoreComponent component)
    {
        component.Preset = preset.ID;
        component.CurrencyWhitelist.UnionWith(preset.CurrencyWhitelist);
        component.Categories.UnionWith(preset.Categories);
        if (component.Balance == new Dictionary<string, FixedPoint2>() && preset.InitialBalance != null) //if we don't have a value stored, use the preset
            TryAddCurrency(preset.InitialBalance, component);

        var ui = _ui.GetUiOrNull(component.Owner, StoreUiKey.Key);
        if (ui != null)
            _ui.SetUiState(ui, new StoreInitializeState(preset.StoreName));
    }
}

/// <summary>
/// Raised on an item when it is purchased.
/// An item may need to set it upself up for its purchaser.
/// For example, to make sure it isn't hostile to them or
/// to make sure it fits their apperance.
/// </summary>
[ByRefEvent]
public readonly record struct ItemPurchasedEvent(EntityUid Purchaser);
