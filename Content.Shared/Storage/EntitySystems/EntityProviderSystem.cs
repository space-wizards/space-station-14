using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.Events;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// System for managing providing entities from storage.
/// <seealso cref="EntityProviderComponent"/>
/// </summary>
public sealed partial class EntityProviderSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<EntityProviderComponent> _providerQuery;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery;

    private const string ContainerId = "entity-provider";

    /// <summary> Initialize container on component. </summary>
    [SubscribeLocalEvent]
    private void OnInit(Entity<EntityProviderComponent> provider, ref ComponentInit args)
    {
        provider.Comp.Container = _container.EnsureContainer<Container>(provider, ContainerId);

        if (provider.Comp.CanEject && provider.Comp.EntityCounter.Count == 1)
            provider.Comp.SelectedEntityProtoId =  provider.Comp.EntityCounter.Single().Key;
    }

    /// <summary> Adds contents info into examine. </summary>
    [SubscribeLocalEvent]
    private void OnExamined(Entity<EntityProviderComponent> replacer, ref ExaminedEvent args)
    {
        if (!TryGetEntityCounter(replacer.Owner, out var entities))
            return;

        using (args.PushGroup(nameof(EntityProviderComponent)))
        {
            if (entities.Count == 0)
            {
                args.PushMarkup(Loc.GetString("comp-entity-provider-no-stored-entities"));
                return;
            }

            args.PushMarkup(Loc.GetString("comp-entity-provider-has-entities"));

            foreach (var entity in entities)
            {
                if (!ProtoMan.Resolve(entity.Key, out var entityPrototype))
                    continue;

                args.PushMarkup(Loc.GetString("comp-entity-provider-entity-listing", ("amount", entity.Value), ("name", entityPrototype.Name)));
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<EntityProviderComponent> provider, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!provider.Comp.CanEject || provider.Comp.EntityCounter.Count == 0)
            return;

        var user = args.User;
        var verb = new AlternativeVerb()
        {
            Text = Loc.GetString("comp-entity-provider-select-new-active"),
            Act = () => _ui.OpenUi(provider.Owner, EntityProviderUiKey.Key, user),
        };
        args.Verbs.Add(verb);
    }

    /// <summary> Eject one of the currently selected entity. </summary>
    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<EntityProviderComponent> provider, ref UseInHandEvent args)
    {
        if (args.Handled || !provider.Comp.CanEject || provider.Comp.SelectedEntityProtoId == null)
            return;

        TryEjectEntities(provider.AsNullable(), provider.Comp.SelectedEntityProtoId.Value, out _, 1, args.User);
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<EntityProviderComponent> provider, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
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
    private void OnInteractUsing(Entity<EntityProviderComponent> provider, ref InteractUsingEvent args)
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

    [SubscribeLocalEvent]
    private void OnSwitchMessage(Entity<EntityProviderComponent> provider, ref SwitchSelectedEntity args)
    {
        if (provider.Comp.CanEject) // Safeguard against modified clients
            TrySelectEntity(provider.AsNullable(), args.EntityProtoId, args.Actor);
    }

    [SubscribeLocalEvent]
    private void OnEjectMessage(Entity<EntityProviderComponent> provider, ref EjectSelectedEntities args)
    {
        if (provider.Comp.CanEject) // Safeguard against modified clients
            TryEjectEntities(provider.AsNullable(), args.EntityProtoId, out _, user: args.Actor);
    }

    /// <summary> Spawn all entities when reclaimed. </summary>
    [SubscribeLocalEvent]
    private void OnReclaimed(Entity<EntityProviderComponent> provider, ref GotReclaimedEvent args)
    {
        foreach (var entities in provider.Comp.EntityCounter)
        {
            TryEjectEntities(provider.AsNullable(), entities.Key, out _);
        }
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

        if (IsProviderFull(refillTarget, user))
            return false;

        foreach (var (entProtoId, count) in provider.Comp.EntityCounter)
        {
            if (_whitelist.IsWhitelistFailOrNull(refillTarget.Comp.Whitelist, entProtoId))
                continue;

            var amount = count;
            // Get the total count of entities inside the refillTarget.
            var entityCount = refillTarget.Comp.EntityCounter.Values.Sum();
            var isFull = false;

            if (refillTarget.Comp.MaxEntityCount.HasValue && count + entityCount > refillTarget.Comp.MaxEntityCount)
            {
                isFull = true;
                amount = refillTarget.Comp.MaxEntityCount.Value - entityCount;
            }

            if (!refillTarget.Comp.EntityCounter.TryAdd(entProtoId, amount))
                refillTarget.Comp.EntityCounter[entProtoId] += amount;

            // Move all spawned entities over to the new provider.
            var existingEntities = GetEntitiesFromContainer(provider.AsNullable(), entProtoId, amount).ToArray();
            foreach (var spawnedEntity in existingEntities)
            {
                _container.Insert(spawnedEntity, refillTarget.Comp.Container);
            }

            success = true;
            // Don't add it to the remove list if the entity provider wasn't emptied.
            if (isFull)
                break;

            toRemove.Add(entProtoId);
        }

        foreach (var removedEntProtoId in toRemove)
        {
            provider.Comp.EntityCounter.Remove(removedEntProtoId);
        }

        if (provider.Comp is { DeleteIfEmpty: true, EntityCounter.Count: 0 })
            PredictedQueueDel(provider);

        HandleAppearance(provider.AsNullable());
        HandleAppearance(refillTarget.AsNullable());

        Dirty(provider);
        Dirty(refillTarget);

        if (!success)
            return success;
        // The refilling provider dictates the sound happening at the refillTarget.
        _audio.PlayPredicted(provider.Comp.PluralTransferSound, refillTarget, user);

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
        if (!refillTarget.Comp.CanReceive || IsProviderFull(refillTarget, user) || !Resolve(storage, ref storage.Comp))
            return false;

        var storedEntities = storage.Comp.Container.ContainedEntities.ToArray();
        var insertionSuccess = false;

        foreach (var entity in storedEntities)
        {
            if (TryInsertIntoProvider(refillTarget, entity)) // Not passing along the user to avoid the popup.
                insertionSuccess = true;
        }

        // show some message if success
        if (!insertionSuccess || user == null)
            return insertionSuccess;

        _audio.PlayPredicted(refillTarget.Comp.PluralTransferSound, refillTarget, user);

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

    private void HandleAppearance(Entity<EntityProviderComponent?> provider)
    {
        if (!Resolve(provider, ref provider.Comp))
            return;

        var count = provider.Comp.EntityCounter.Count;

        if (count == 0)
            _appearance.SetData(provider.Owner, EntityProviderVisuals.Key, EntityProviderVisuals.Empty);
        else if (count == provider.Comp.MaxEntityCount)
            _appearance.SetData(provider.Owner, EntityProviderVisuals.Key, EntityProviderVisuals.Full);
        else
            _appearance.SetData(provider.Owner, EntityProviderVisuals.Key, EntityProviderVisuals.Opened);
    }

    /// <summary>
    /// Checks if the provider is full.
    /// </summary>
    /// <returns>Returns true if full, otherwise false.</returns>
    /// <remarks>If <see cref="EntityProviderComponent.MaxEntityCount"/> is null, it'll always return false.</remarks>
    private bool IsProviderFull(Entity<EntityProviderComponent> provider, EntityUid? user = null)
    {
        if (provider.Comp.MaxEntityCount == null || provider.Comp.EntityCounter.Count < provider.Comp.MaxEntityCount)
            return false;

        var message = Loc.GetString("comp-entity-provider-full", ("provider", provider));
        // No need to check if the user is null, because it simply won't show a popup if it is.
        _popup.PopupEntity(message, provider, user, PopupType.Medium);
        return true;

    }
}

/// <summary>
/// Used for showing the radial menu for selecting & ejecting entities stored within a provider.
/// </summary>
[Serializable, NetSerializable]
public enum EntityProviderUiKey : byte
{
    Key,
}

/// <summary>
/// Used for appearance visuals based on how whether the provider is full, empty or neither.
/// </summary>
[Serializable, NetSerializable]
public enum EntityProviderVisuals : byte
{
    Key, // This is where the data will be stored.
    Full,
    Opened,
    Empty,
}
