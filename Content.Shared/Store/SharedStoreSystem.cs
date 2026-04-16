using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.Implants;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Store.Components;
using Content.Shared.Store.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

/// <summary>
/// Manages general interactions with a store and different entities,
/// getting listings for stores, and interfacing with the store UI.
/// </summary>
public abstract partial class SharedStoreSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] protected readonly SharedMindSystem Mind = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly SharedStackSystem Stack = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    [Dependency] protected readonly EntityQuery<StoreComponent> StoreQuery = default!;
    [Dependency] protected readonly EntityQuery<RemoteStoreComponent> RemoteStoreQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CurrencyComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RemoteStoreComponent, GetStoreEvent>(OnGetStore);
        SubscribeLocalEvent<RemoteStoreComponent, ImplantRelayEvent<GetStoreEvent>>((x, ref y) =>
        {
            var ev = y.Event;
            OnGetStore(x, ref ev);
            y.Event = ev;
        });
        SubscribeLocalEvent<RemoteStoreComponent, ImplantRelayEvent<CurrencyInsertAttemptEvent>>(OnImplantInsertAttempt);
        SubscribeLocalEvent<StoreComponent, IntrinsicStoreActionEvent>(OnIntrinsicStoreAction);
    }

    private void OnGetStore(Entity<RemoteStoreComponent> entity, ref GetStoreEvent args)
    {
        if (args.Handled)
            return;

        if (!StoreQuery.TryComp(entity.Comp.Store, out var store))
            return;

        args.Store = (entity.Comp.Store.Value, store);
    }

    private void OnImplantInsertAttempt(Entity<RemoteStoreComponent> implant, ref ImplantRelayEvent<CurrencyInsertAttemptEvent> args)
    {
        var ev = args.Event;

        // Only allow insertion if the person implanted is doing the action.
        if (ev.User == ev.Target)
            ev.TargetOverride = implant;
        else
            ev.Cancel();

        args.Event = ev;
    }

    private void OnAfterInteract(EntityUid uid, CurrencyComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryGetStore(target, out var store))
            return;

        var ev = new CurrencyInsertAttemptEvent(args.User, target, args.Used, store.Value.Comp);
        RaiseLocalEvent(target, ev);
        if (ev.Cancelled)
            return;

        if (!TryAddCurrency((uid, component), (store.Value, store.Value.Comp)))
            return;

        args.Handled = true;
        var msg = Loc.GetString("store-currency-inserted", ("used", args.Used), ("target", ev.TargetOverride ?? target));
        Popup.PopupEntity(msg, target, args.User);
    }

    /// <summary>
    /// Attempts to find a store connected to this entity.
    /// First checking for a <see cref="StoreComponent"/> on this entity,
    /// then checking for a <see cref="RemoteStoreComponent"/> to find a remotely connected store.
    /// </summary>
    /// <param name="entity">Entity we're checking for an attached store on</param>
    /// <param name="store">Store entity we're returning.</param>
    /// <returns>True if a store was found.</returns>
    public bool TryGetStore(EntityUid entity, [NotNullWhen(true)] out Entity<StoreComponent>? store)
    {
        store = GetStore(entity);
        return store != null;
    }

    /// <summary>
    /// Attempts to find a store connected to this entity.
    /// First checking for a <see cref="StoreComponent"/> on this entity,
    /// then checking for a <see cref="RemoteStoreComponent"/> to find a remotely connected store.
    /// </summary>
    /// <param name="entity">Entity we're checking for an attached store on</param>
    /// <returns>The store entity and component if found.</returns>
    public Entity<StoreComponent>? GetStore(EntityUid entity)
    {
        if (StoreQuery.TryComp(entity, out var storeComp))
            return (entity, storeComp);

        var ev = new GetStoreEvent();
        RaiseLocalEvent(entity, ref ev);
        return ev.Store;
    }

    /// <summary>
    /// Attempts to find a remote store connected to this entity.
    /// Checking for a <see cref="RemoteStoreComponent"/> with an attached store entity.
    /// </summary>
    /// <param name="entity">Entity we're checking for an attached store on</param>
    /// <returns>The store entity and component if found.</returns>
    public Entity<StoreComponent>? GetRemoteStore(Entity<RemoteStoreComponent?> entity)
    {
        if (RemoteStoreQuery.Resolve(entity, ref entity.Comp)
            && entity.Comp.Store != null
            && StoreQuery.TryComp(entity.Comp.Store, out var storeComp))
            return (entity.Comp.Store.Value, storeComp);

        return null;
    }

    public void SetRemoteStore(Entity<RemoteStoreComponent?> entity, EntityUid? store)
    {
        if (!RemoteStoreQuery.Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.Store = store;
    }

        /// <summary>
    /// Gets the value from an entity's currency component.
    /// Scales with stacks.
    /// </summary>
    /// <remarks>
    /// If this result is intended to be used with <see cref="TryAddCurrency(Robust.Shared.GameObjects.Entity{Shared.Store.Components.CurrencyComponent?},Robust.Shared.GameObjects.Entity{Content.Shared.Store.Components.StoreComponent?})"/>,
    /// consider using <see cref="TryAddCurrency(Robust.Shared.GameObjects.Entity{Shared.Store.Components.CurrencyComponent?},Robust.Shared.GameObjects.Entity{Content.Shared.Store.Components.StoreComponent?})"/> instead to ensure that the currency is consumed in the process.
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
            Stack.SetCount((currency.Owner, stack), 0);

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

    private void OnIntrinsicStoreAction(Entity<StoreComponent> ent, ref IntrinsicStoreActionEvent args)
    {
        ToggleUi(args.Performer, ent.Owner, ent.Comp);
    }
}

[ByRefEvent]
public record struct GetStoreEvent
{
    public readonly bool Handled => Store != null;
    public Entity<StoreComponent>? Store;
}

public sealed class CurrencyInsertAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Target;
    public readonly EntityUid Used;
    public readonly StoreComponent Store;

    // An optional override for the "Target" of this interaction, used to change the name that pops up!
    public EntityUid? TargetOverride;

    public CurrencyInsertAttemptEvent(EntityUid user, EntityUid target, EntityUid used, StoreComponent store)
    {
        User = user;
        Target = target;
        Used = used;
        Store = store;
    }
}

