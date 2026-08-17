using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// System for managing providing entities from storage.
/// <seealso cref="EntityProviderComponent"/>
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

        if (!provider.Comp.CanTransfer)
        {
            _popup.PopupEntity(Loc.GetString("comp-entity-provider-cannot-transfer", ("provider", provider)), provider, user);
            return false;
        }

        if (!refillTarget.Comp.CanReceive)
        {
            _popup.PopupEntity(Loc.GetString("comp-entity-provider-cannot-receive", ("refillTarget", refillTarget)), refillTarget, user);
            return false;
        }

        foreach (var (entProtoId, count) in provider.Comp.EntityCounter)
        {
            if (_whitelist.IsWhitelistFail(refillTarget.Comp.Whitelist, entProtoId))
                continue;

            if (!refillTarget.Comp.EntityCounter.TryAdd(entProtoId, count))
                refillTarget.Comp.EntityCounter[entProtoId] += count;

            // Move all spawned entities over to the new provider.
            foreach (var spawnedEntity in GetEntitiesFromContainer(provider.AsNullable(), entProtoId))
            {
                _container.Insert(spawnedEntity, refillTarget.Comp.Container);
            }

            success = true;
            toRemove.Add(entProtoId);
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

    /// <summary>
    /// Attempts to get only the spawned entities of a certain prototype from the container.
    /// </summary>
    /// <param name="provider">The entityProvider with the container.</param>
    /// <param name="protoId">The entity prototype to check for. </param>
    /// <param name="requestedAmount">The amount of entities to retrieve. If null, it'll retrieve all of them.</param>
    /// <returns>Returns a list of all currently spawned entities of that prototype. It will NOT spawn more to reach <see cref="requestedAmount"/>.</returns>
    private IEnumerable<EntityUid> GetEntitiesFromContainer(Entity<EntityProviderComponent?> provider, EntProtoId protoId, int? requestedAmount = null)
    {
        if (requestedAmount <= 0 || !Resolve(provider, ref provider.Comp))
            yield break;

        var containedEntities = provider.Comp.Container.ContainedEntities;
        var count = 0;
        foreach (var containedEntity in containedEntities)
        {
            var meta = MetaData(containedEntity).EntityPrototype;

            if (meta != null && meta.ID == protoId)
            {
                yield return containedEntity;
                count++;
            }
            // Check if we have enough.
            if (count == requestedAmount)
                yield break;
        }
    }
}
