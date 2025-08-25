using Content.Server.Store.Components;
using Content.Shared.UserInterface;
using Content.Shared.FixedPoint;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
using Content.Server.PDA.Ringer;
using Content.Shared.DoAfter;
using Robust.Shared.Timing;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;

namespace Content.Server.Store.Systems;

/// <summary>
/// Manages general interactions with a store and different entities,
/// getting listings for stores, and interfacing with the store UI.
/// </summary>
public sealed partial class StoreSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreComponent, ActivatableUIOpenAttemptEvent>(OnStoreOpenAttempt);
        SubscribeLocalEvent<CurrencyComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<StoreComponent, BeforeActivatableUIOpenEvent>(BeforeActivatableUiOpen);

        SubscribeLocalEvent<StoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StoreComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StoreComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StoreComponent, OpenUplinkImplantEvent>(OnImplantActivate);

        SubscribeLocalEvent<StealableStoreComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<StealableStoreComponent, StealableStoreDoAfterEvent>(OnStealableDoAfter);

        InitializeUi();
        InitializeCommand();
        InitializeRefund();
    }

    private void OnMapInit(EntityUid uid, StoreComponent component, MapInitEvent args)
    {
        RefreshAllListings(component);
        component.StartingMap = Transform(uid).MapUid;
    }

    private void OnStartup(EntityUid uid, StoreComponent component, ComponentStartup args)
    {
        // for traitors, because the StoreComponent for the PDA can be added at any time.
        if (MetaData(uid).EntityLifeStage == EntityLifeStage.MapInitialized)
        {
            RefreshAllListings(component);
        }

        var ev = new StoreAddedEvent();
        RaiseLocalEvent(uid, ref ev, true);
    }

    private void OnShutdown(EntityUid uid, StoreComponent component, ComponentShutdown args)
    {
        var ev = new StoreRemovedEvent();
        RaiseLocalEvent(uid, ref ev, true);
    }

    private void OnStoreOpenAttempt(EntityUid uid, StoreComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (!component.OwnerOnly)
            return;

        if (!_mind.TryGetMind(args.User, out var mind, out _))
            return;

        component.AccountOwner ??= mind;
        DebugTools.Assert(component.AccountOwner != null);

        if (component.AccountOwner == mind)
            return;

        _popup.PopupEntity(Loc.GetString("store-not-account-owner", ("store", uid)), uid, args.User);
        args.Cancel();
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

        if (!TryAddCurrency((uid, component), (args.Target.Value, store)))
            return;

        args.Handled = true;
        var msg = Loc.GetString("store-currency-inserted", ("used", args.Used), ("target", args.Target));
        _popup.PopupEntity(msg, args.Target.Value, args.User);
    }

    private void OnImplantActivate(EntityUid uid, StoreComponent component, OpenUplinkImplantEvent args)
    {
        ToggleUi(args.Performer, uid, component);
    }

    private void OnInteractUsing(Entity<StealableStoreComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !ent.Comp.CanBeStolenFrom)
            return;

        // fuck you
        if (HasComp<MindShieldComponent>(args.User))
            return;

        if (ent.Comp.RequireUserUnlocked && TryComp<RingerUplinkComponent>(args.Used, out var usedUplink) && !usedUplink.Unlocked)
            return;

        if (ent.Comp.RequireTargetUnlocked && TryComp<RingerUplinkComponent>(ent, out var targetUplink) && !targetUplink.Unlocked)
            return;

        if (!TryComp<StoreComponent>(ent, out var targetStore) || !TryComp<StoreComponent>(args.Used, out var usedStore))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterDuration, new StealableStoreDoAfterEvent(), ent, target: ent, used: args.Used)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true
        };

        if (ent.Comp.SelfStealPopup != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.SelfStealPopup, ("target", args.Target)), args.User, args.User);

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnStealableDoAfter(Entity<StealableStoreComponent> ent, ref StealableStoreDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.RequireUserUnlocked && TryComp<RingerUplinkComponent>(args.Used, out var usedUplink) && !usedUplink.Unlocked)
            return;

        if (ent.Comp.RequireTargetUnlocked && TryComp<RingerUplinkComponent>(ent, out var targetUplink) && !targetUplink.Unlocked)
            return;

        if (!TryComp<StoreComponent>(ent, out var targetStore) || !TryComp<StoreComponent>(args.Used, out var usedStore))
            return;

        foreach (var currency in targetStore.Balance.ToList())
        {
            TryAddCurrency(new Dictionary<string, FixedPoint2> { { currency.Key, currency.Value } }, args.Used.Value, usedStore);
        }

        targetStore.Balance.Clear();

        _audio.PlayPvs(ent.Comp.FinishStealingSound, ent);

        if (ent.Comp.SuccessfulStealPopup != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.SuccessfulStealPopup), ent, PopupType.MediumCaution);
    }

    /// <summary>
    /// Gets the value from an entity's currency component.
    /// Scales with stacks.
    /// </summary>
    /// <remarks>
    /// If this result is intended to be used with <see cref="TryAddCurrency(Robust.Shared.GameObjects.Entity{Content.Server.Store.Components.CurrencyComponent?},Robust.Shared.GameObjects.Entity{Content.Shared.Store.Components.StoreComponent?})"/>,
    /// consider using <see cref="TryAddCurrency(Robust.Shared.GameObjects.Entity{Content.Server.Store.Components.CurrencyComponent?},Robust.Shared.GameObjects.Entity{Content.Shared.Store.Components.StoreComponent?})"/> instead to ensure that the currency is consumed in the process.
    /// </remarks>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns>The value of the currency</returns>
    public Dictionary<string, FixedPoint2> GetCurrencyValue(EntityUid uid, CurrencyComponent component)
    {
        var amount = EntityManager.GetComponentOrNull<StackComponent>(uid)?.Count ?? 1;
        return component.Price.ToDictionary(v => v.Key, p => p.Value * amount);
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

        QueueDel(currency);
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

        UpdateUserInterface(null, uid, store);
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
