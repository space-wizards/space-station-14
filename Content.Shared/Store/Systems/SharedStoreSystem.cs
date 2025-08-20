using Content.Shared.UserInterface;
using Content.Shared.FixedPoint;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Store.Systems;

/// <summary>
/// Manages general interactions with a store and different entities,
/// getting listings for stores, and interfacing with the store UI.
/// </summary>
public abstract partial class SharedStoreSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] protected readonly SharedHandsSystem Hands = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem Ui = default!;
    [Dependency] private   readonly IGameTiming _timing = default!;
    [Dependency] private   readonly INetManager _net = default!;
    [Dependency] private   readonly ISharedAdminLogManager _admin = default!;
    [Dependency] private   readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private   readonly ActionUpgradeSystem _actionUpgrade = default!;
    [Dependency] private   readonly SharedActionsSystem _actions = default!;
    [Dependency] private   readonly SharedAudioSystem _audio = default!;
    [Dependency] private   readonly SharedMindSystem _mind = default!;
    [Dependency] private   readonly SharedStackSystem _stack = default!;
    [Dependency] private   readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreComponent, BeforeActivatableUIOpenEvent>(BeforeActivatableUiOpen);
        SubscribeLocalEvent<StoreComponent, ActivatableUIOpenAttemptEvent>(OnStoreOpenAttempt);
        SubscribeLocalEvent<CurrencyComponent, AfterInteractEvent>(OnAfterInteract);

        SubscribeLocalEvent<StoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StoreComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoreComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StoreComponent, OpenUplinkImplantEvent>(OnImplantActivate);
        SubscribeLocalEvent<StoreComponent, ImplantRelayEvent<AfterInteractUsingEvent>>(OnStoreRelay);

        InitializeUi();
        InitializeRefund();
    }

    private void OnMapInit(Entity<StoreComponent> ent, ref MapInitEvent args)
    {
        RefreshAllListings(ent);
        ent.Comp.StartingMap = Transform(ent).MapUid;
        DirtyField(ent, ent.Comp, nameof(StoreComponent.StartingMap));
        UpdateUi(ent);
    }

    private void OnStartup(Entity<StoreComponent> ent, ref ComponentStartup args)
    {
        var uid = ent.Owner;

        // for traitors, because the StoreComponent for the PDA can be added at any time.
        if (MetaData(uid).EntityLifeStage == EntityLifeStage.MapInitialized)
        {
            RefreshAllListings(ent);
        }

        var ev = new StoreAddedEvent();
        RaiseLocalEvent(uid, ref ev, true);
    }

    private void OnShutdown(Entity<StoreComponent> ent, ref ComponentShutdown args)
    {
        var ev = new StoreRemovedEvent();
        RaiseLocalEvent(ent, ref ev, true);
    }

    private void BeforeActivatableUiOpen(Entity<StoreComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateAvailableListings(args.User, (ent.Owner, ent.Comp));
    }

    private void OnStoreOpenAttempt(Entity<StoreComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        var (uid, component) = ent;

        if (!component.OwnerOnly)
            return;

        if (!_mind.TryGetMind(args.User, out var mind, out _))
            return;

        component.AccountOwner ??= mind;
        DebugTools.Assert(component.AccountOwner != null);

        if (component.AccountOwner == mind)
            return;

        _popup.PopupPredicted(Loc.GetString("store-not-account-owner", ("store", uid)), uid, args.User);
        args.Cancel();
    }

    private void OnAfterInteract(Entity<CurrencyComponent> ent, ref AfterInteractEvent args)
    {
        var (uid, component) = ent;

        if (args.Handled || !args.CanReach)
            return;

        if (!TryComp<StoreComponent>(args.Target, out var store))
            return;

        var ev = new CurrencyInsertAttemptEvent(args.User, args.Target.Value, args.Used, store);
        RaiseLocalEvent(args.Target.Value, ev);
        if (ev.Cancelled)
            return;

        if (!TryAddCurrency((uid, component), (args.Target.Value, store)))
            return;

        args.Handled = true;
        var msg = Loc.GetString("store-currency-inserted", ("used", args.Used), ("target", args.Target));
        _popup.PopupPredicted(msg, args.Target.Value, args.User);
    }

    private void OnImplantActivate(Entity<StoreComponent> ent, ref OpenUplinkImplantEvent args)
    {
        ToggleUi(args.Performer, ent, ent.Comp);
    }

    private void OnStoreRelay(EntityUid uid, StoreComponent store, ImplantRelayEvent<AfterInteractUsingEvent> implantRelay)
    {
        var args = implantRelay.Event;

        if (args.Handled)
            return;

        // can only insert into yourself to prevent uplink checking with renault
        if (args.Target != args.User)
            return;

        if (!TryComp<CurrencyComponent>(args.Used, out var currency))
            return;

        // same as store code, but message is only shown to yourself
        if (!TryAddCurrency((args.Used, currency), (uid, store)))
            return;

        args.Handled = true;
        var msg = Loc.GetString("store-currency-inserted-implant", ("used", args.Used));
        _popup.PopupEntity(msg, args.User, args.User);
    }

    /// <summary>
    /// Gets the value from an entity's currency component.
    /// Scales with stacks.
    /// </summary>
    /// <remarks>
    /// If this result is intended to be used with <see cref="TryAddCurrency(Robust.Shared.GameObjects.Entity{Content.Shared.Store.Components.CurrencyComponent?},Robust.Shared.GameObjects.Entity{Content.Shared.Store.Components.StoreComponent?})"/>,
    /// consider using <see cref="TryAddCurrency(Robust.Shared.GameObjects.Entity{Content.Shared.Store.Components.CurrencyComponent?},Robust.Shared.GameObjects.Entity{Content.Shared.Store.Components.StoreComponent?})"/> instead to ensure that the currency is consumed in the process.
    /// </remarks>
    /// <returns>The value of the currency</returns>
    public Dictionary<string, FixedPoint2> GetCurrencyValue(Entity<CurrencyComponent> ent)
    {
        var amount = CompOrNull<StackComponent>(ent)?.Count ?? 1;
        return ent.Comp.Price.ToDictionary(v => v.Key, p => p.Value * amount);
    }

    /// <summary>
    /// Tries to add a currency to a store's balance. Note that if successful, this will consume the currency in the process.
    /// </summary>
    public bool TryAddCurrency(Entity<CurrencyComponent?> currency, Entity<StoreComponent?> store)
    {
        if (!Resolve(currency.Owner, ref currency.Comp))
            return false;

        if (!Resolve(store.Owner, ref store.Comp))
            return false;

        var value = currency.Comp.Price;
        if (TryComp(currency.Owner, out StackComponent? stack) && stack.Count != 1)
        {
            value = currency.Comp.Price
                .ToDictionary(v => v.Key, p => p.Value * stack.Count);
        }

        if (!TryAddCurrency(value, store, store.Comp))
            return false;

        // Avoid having the currency accidentally be re-used. E.g., if multiple clients try to use the currency in the
        // same tick
        currency.Comp.Price.Clear();

        if (stack != null)
            _stack.SetCount(currency.Owner, 0, stack);

        PredictedQueueDel(currency.Owner);
        return true;
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

        DirtyField(uid, store, nameof(StoreComponent.Balance));
        UpdateUi((uid, store));
        return true;
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
