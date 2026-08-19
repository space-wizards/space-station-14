using Content.Shared.Body.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Rootable;
using Content.Shared.Sericulture;
using Content.Shared.Speech.Components;
using Content.Shared.Storage;
using Content.Shared.Store.Components;
using Content.Shared.Wagging;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Cloning;

/// <summary>
/// A context to be used for copying components over to a cloned entity.
/// Custom cloning logic per component should be implemented here!
/// </summary>
public sealed partial class CloningContext :
    ISerializationContext,
    ITypeCopier<BloodstreamComponent>,
    ITypeCopier<HandsComponent>,
    ITypeCopier<InventoryComponent>,
    ITypeCopier<JumpAbilityComponent>,
    ITypeCopier<PullerComponent>,
    ITypeCopier<RootableComponent>,
    ITypeCopier<SericultureComponent>,
    ITypeCopier<StorageComponent>,
    ITypeCopier<StoreComponent>,
    ITypeCopier<VocalComponent>,
    ITypeCopier<WaggingComponent>
{
    /// <inheritdoc />
    public SerializationManager.SerializerProvider SerializerProvider { get; }

    /// <inheritdoc />
    public bool WritingReadingPrototypes { get; set; }

    // Dependencies
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;
    [Dependency] private EntityQuery<HandsComponent> _handsQuery = default!;
    [Dependency] private EntityQuery<InventoryComponent> _inventoryQuery = default!;
    [Dependency] private EntityQuery<JumpAbilityComponent> _jumpAbilityQuery = default!;
    [Dependency] private EntityQuery<PullerComponent> _pullerQuery = default!;
    [Dependency] private EntityQuery<RootableComponent> _rootableQuery = default!;
    [Dependency] private EntityQuery<SericultureComponent> _sericultureQuery = default!;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;
    [Dependency] private EntityQuery<StoreComponent> _storeQuery = default!;
    [Dependency] private EntityQuery<VocalComponent> _vocalQuery = default!;
    [Dependency] private EntityQuery<WaggingComponent> _waggingQuery = default!;

    // Persistence entity
    private EntityUid _target = EntityUid.Invalid;

    /// <summary>
    /// Constructs a serialization context for cloning entities.
    /// Handles special cases of component values, along with CloningSystem.Subscriptions.
    /// </summary>
    public CloningContext(IDependencyCollection dependency, ISerializationManager ser)
    {
        dependency.InjectDependencies(this);

        SerializerProvider = new(ser);
        SerializerProvider.RegisterSerializer(this);
    }

    /// <summary>
    /// Grabs the components from the target object if they exist.
    /// Should be called before CopyTo.
    /// </summary>
    /// <remarks>
    /// This isn't great, though it works for the case of CopyComponents because
    /// the state of the components updates before the components are removed and
    /// added again.
    /// We need to cache the values of the components before they were removed.
    /// </remarks>
    public void CacheTarget(EntityUid target)
    {
        _target = target;
    }

    /// <summary>
    /// Clears the components from the target object if they exist.
    /// Should be called after CopyTo.
    /// </summary>
    public void ClearTarget()
    {
        _target = EntityUid.Invalid;
    }

    // Clone functions below.
    // Keep in alphabetical order by name of component,
    // and keep fields within the component in the order they were defined in.

    #region Bloodstream
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        BloodstreamComponent source,
        ref BloodstreamComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _bloodstreamQuery.TryComp(_target, out var bloodstream);
        target.NextUpdate = TimeSpan.Zero;
        target.BleedAmount = bloodstream?.BleedAmount ?? 0.0f;
        target.BloodReferenceSolution = bloodstream?.BloodReferenceSolution ?? new();
        target.BloodData = bloodstream?.BloodData;
        target.BloodSolution = bloodstream?.BloodSolution;
        target.TemporarySolution = bloodstream?.TemporarySolution;
        target.MetabolitesSolution = bloodstream?.MetabolitesSolution;
    }
    #endregion Bloodstream

    #region Hands
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        HandsComponent source,
        ref HandsComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _handsQuery.TryComp(_target, out var hands);
        target.ActiveHandId = hands?.ActiveHandId;
        target.StartingHands = hands?.StartingHands ?? new();
        target.Hands = hands?.Hands ?? new();
        target.SortedHands = hands?.SortedHands ?? new();
        target.RevealedLayers.Clear();
        if (hands != null)
        {
            target.RevealedLayers.Clear();
            foreach ((var hand, var layers) in hands.RevealedLayers)
            {
                target.RevealedLayers[hand] = new(layers);
            }
        }
        target.NextThrowTime = hands?.NextThrowTime ?? TimeSpan.Zero;
    }
    #endregion Hands

    #region Inventory
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        InventoryComponent source,
        ref InventoryComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _inventoryQuery.TryComp(_target, out var inventory);
        target.Slots = inventory?.Slots ?? [];
        target.Containers = inventory?.Containers ?? [];
    }
    #endregion Inventory

    #region JumpAbility
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        JumpAbilityComponent source,
        ref JumpAbilityComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _jumpAbilityQuery.TryComp(_target, out var jumpAbility);
        target.ActionEntity = jumpAbility?.ActionEntity;
    }
    #endregion JumpAbility

    #region Pulling
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        PullerComponent source,
        ref PullerComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _pullerQuery.TryComp(_target, out var puller);
        target.Pulling = puller?.Pulling;
        target.NextThrow = puller?.NextThrow ?? TimeSpan.Zero;
    }
    #endregion Pulling

    #region Rootable
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        RootableComponent source,
        ref RootableComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _rootableQuery.TryComp(_target, out var rootable);
        target.ActionEntity = rootable?.ActionEntity;
        target.PuddleEntity = rootable?.PuddleEntity;
    }
    #endregion Rootable

    #region Sericulture
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        SericultureComponent source,
        ref SericultureComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _sericultureQuery.TryComp(_target, out var sericulture);
        target.ActionEntity = sericulture?.ActionEntity;
    }
    #endregion Sericulture

    #region Storage
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        StorageComponent source,
        ref StorageComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _storageQuery.TryComp(_target, out var storage);
        target.StoredItems = storage?.StoredItems ?? new();
        target.SavedLocations = storage?.SavedLocations ?? new();
    }
    #endregion Storage

    #region Store
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        StoreComponent source,
        ref StoreComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _storeQuery.TryComp(_target, out var store);
        target.AccountOwner = store?.AccountOwner;
        target.FullListingsCatalog = store?.FullListingsCatalog ?? new();
        target.BoughtEntities = store?.BoughtEntities ?? new();
        target.BalanceSpent = store?.BalanceSpent ?? new();
        target.StartingMap = store?.StartingMap ?? new();
    }
    #endregion Store

    #region Vocal
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        VocalComponent source,
        ref VocalComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _vocalQuery.TryComp(_target, out var vocal);
        target.EmoteActionEntity = vocal?.EmoteActionEntity;
    }
    #endregion Vocal

    #region Wagging
    /// <inheritdoc/>
    public void CopyTo(
        ISerializationManager serializationManager,
        WaggingComponent source,
        ref WaggingComponent target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        // Persistent fields
        _waggingQuery.TryComp(_target, out var wagging);
        target.ActionEntity = wagging?.ActionEntity;
        target.Wagging = wagging?.Wagging ?? false;
    }
    #endregion Wagging
}
