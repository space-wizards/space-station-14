using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Storage.EntitySystems;

public sealed partial class EntityProviderSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<EntityProviderComponent> _providerQuery = default!;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;

    private const string ContainerId = "entity-provider";

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

    private bool TryFillOtherProvider(Entity<EntityProviderComponent> provider, Entity<EntityProviderComponent> target, EntityUid? user = null)
    {
        bool success = false;
        List<EntProtoId> toRemove = [];

        foreach (var providedEntities in provider.Comp.EntityCounter)
        {
            if (_whitelist.IsWhitelistFail(target.Comp.Whitelist, providedEntities.Key))
                continue;

            if (!target.Comp.EntityCounter.TryAdd(providedEntities.Key, providedEntities.Value))
                target.Comp.EntityCounter[providedEntities.Key] += providedEntities.Value;

            success = true;
            toRemove.Add(providedEntities.Key);
        }

        foreach (var removedEntProtoId in toRemove)
        {
            provider.Comp.EntityCounter.Remove(removedEntProtoId);
        }

        if (provider.Comp.DeleteIfEmpty && provider.Comp.EntityCounter.Count == 0)
            PredictedQueueDel(provider);
        else
            Dirty(provider);

        Dirty(target);

        if (!success)
            return success;

        var message = Loc.GetString("comp-entity-provider-refill-from-storage", ("provider", provider));
        _popup.PopupEntity(message, provider, user);

        return success;
    }

    private bool TryFillFromStorage(Entity<EntityProviderComponent> provider, Entity<StorageComponent?> storage, EntityUid? user = null)
    {
        if (!Resolve(storage, ref storage.Comp))
            return false;

        var storedEntities = storage.Comp.Container.ContainedEntities.ToArray();
        var insertionSuccess = false;

        foreach (var ent in storedEntities)
        {
            if (TryInsertIntoProvider(provider, ent)) // Not passing along the user to avoid the popup.
                insertionSuccess = true;
        }

        // show some message if success
        if (!insertionSuccess || user == null)
            return insertionSuccess;

        var message= Loc.GetString("comp-entity-provider-refill-from-storage", ("provider", provider));
        _popup.PopupEntity(message, provider, user);

        return insertionSuccess;
    }
}
