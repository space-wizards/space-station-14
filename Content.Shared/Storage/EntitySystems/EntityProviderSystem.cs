using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// System for managing providing entities from storage.
/// </summary>
public sealed partial class EntityProviderSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<EntityProviderComponent> _providerQuery;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery;

    private const string ContainerId = "entity-provider";

    /// <summary> Initialize container on component. </summary>
    [SubscribeLocalEvent]
    private void OnInit(Entity<EntityProviderComponent> provider, ref ComponentInit args)
    {
        provider.Comp.Container = _container.EnsureContainer<Container>(provider, ContainerId);
    }

    [SubscribeLocalEvent]
    public void OnAfterInteract(Entity<EntityProviderComponent> provider, ref AfterInteractEvent args)
    {
        if (args.Handled || !provider.Comp.CanTransfer || args.Target == null)
            return;

        if (TryInsertIntoProvider(provider, args.Target.Value, args.User))
        {
            args.Handled = true;
            return;
        }

        if (_providerQuery.TryComp(args.Target, out var targetComp))
            args.Handled = TryFillOtherProvider(provider, (args.Target.Value, targetComp), args.User);
    }

    [SubscribeLocalEvent]
    public void OnInteractUsing(Entity<EntityProviderComponent> provider, ref InteractUsingEvent args)
    {
        if (args.Handled || !provider.Comp.CanReceive)
            return;

        if (TryInsertIntoProvider(provider, args.Used, args.User))
        {
            args.Handled = true;
            return;
        }

        if (_storageQuery.TryComp(args.Used, out var storage))
            args.Handled = TryFillFromStorage(provider, (args.Used, storage), args.User);
    }

    /// <summary>
    /// Attempts to insert an entity back into the entityStorage of the provider.
    /// This deletes entities, and thus data. An empty gun inserted will be spawned back as a loaded gun
    /// </summary>
    /// <param name="provider">The entity providing the entityProvider storage.</param>
    /// <param name="target">The entity attempted to be put into the provider.</param>
    /// <param name="user">The user attempting to insert the entity into the provider. Leave null to avoid popups.</param>
    /// <returns>Returns true if it was inserted successfully, otherwise false.</returns>
    private bool TryInsertIntoProvider(Entity<EntityProviderComponent> provider, EntityUid target, EntityUid? user = null)
    {
        if (_whitelist.IsWhitelistFail(provider.Comp.Whitelist, target))
            return false;

        // This event allows for a deeper check than a whitelist/blacklist.
        var ev = new EntityProviderInsertCheckEvent();
        RaiseLocalEvent(target, ref ev);

        if (ev.FailureMessage != null)
        {
            _popup.PopupEntity(ev.FailureMessage, provider, user, PopupType.Medium);
            return false;
        }

        var meta = MetaData(target);
        if (meta.EntityPrototype == null)
            return false;

        if (!provider.Comp.EntityCounter.TryAdd(meta.EntityPrototype, 1))
            provider.Comp.EntityCounter[meta.EntityPrototype]++;

        var message = Loc.GetString("comp-entity-provider-insert-entity", ("provider", provider), ("entity", target));
        _popup.PopupEntity(message, provider, user);

        PredictedQueueDel(target);
        Dirty(provider);
        return true;
    }

    /// <summary>
    /// Fill the entityStorage of a provider with the entityStorage of another provider.
    /// </summary>
    /// <param name="provider">The provider to refill the target.</param>
    /// <param name="refillTarget">The provider whose entityStorage is to be refilled.</param>
    /// <param name="user">The user who attempts to refill the provider with the other. Leave null to avoid popups.</param>
    /// <returns>Returns true if it was able to insert at least one entity, otherwise false.</returns>
    private bool TryFillOtherProvider(Entity<EntityProviderComponent> provider, Entity<EntityProviderComponent> refillTarget, EntityUid? user = null)
    {
        var success = false;
        List<EntProtoId> toRemove = [];

        if (!provider.Comp.CanTransfer || !refillTarget.Comp.CanReceive)
        {
            _popup.PopupEntity(Loc.GetString("comp-entity-provider-cannot-receive", ("refillTarget", refillTarget)), provider, user);
            return false;
        }

        foreach (var providedEntities in provider.Comp.EntityCounter)
        {
            if (_whitelist.IsWhitelistFail(refillTarget.Comp.Whitelist, providedEntities.Key))
                continue;

            if (!refillTarget.Comp.EntityCounter.TryAdd(providedEntities.Key, providedEntities.Value))
                refillTarget.Comp.EntityCounter[providedEntities.Key] += providedEntities.Value;

            success = true;
            toRemove.Add(providedEntities.Key);
        }

        foreach (var removedEntProtoId in toRemove)
        {
            provider.Comp.EntityCounter.Remove(removedEntProtoId);
        }

        if (provider.Comp.DeleteIfEmpty && provider.Comp.EntityCounter.Count == 0)
            PredictedQueueDel(provider);

        Dirty(provider);
        Dirty(refillTarget);

        if (!success)
            return success;

        var message = Loc.GetString("comp-entity-provider-refill-from-storage", ("refillTarget", refillTarget));
        _popup.PopupEntity(message, provider, user);

        return success;
    }

    /// <summary>
    /// Refill an entityProvider with entities inside a storage.
    /// </summary>
    /// <param name="refillTarget">The provider to refill.</param>
    /// <param name="storage">The storage whose contents will refill the provider.</param>
    /// <param name="user">The user who attempts to refill the provider with the storage. Leave null to avoid popups.</param>
    /// <returns>Returns true if it was able to insert at least one entity, otherwise false.</returns>
    private bool TryFillFromStorage(Entity<EntityProviderComponent> refillTarget, Entity<StorageComponent?> storage, EntityUid? user = null)
    {
        if (!Resolve(storage, ref storage.Comp))
            return false;

        var storedEntities = storage.Comp.Container.ContainedEntities.ToArray();
        var insertionSuccess = false;

        foreach (var ent in storedEntities)
        {
            if (TryInsertIntoProvider(refillTarget, ent)) // Not passing along the user to avoid the popup.
                insertionSuccess = true;
        }

        // show some message if success
        if (!insertionSuccess || user == null)
            return insertionSuccess;

        var message= Loc.GetString("comp-entity-provider-refill-from-storage", ("refillTarget", refillTarget));
        _popup.PopupEntity(message, refillTarget, user);

        return insertionSuccess;
    }
}
