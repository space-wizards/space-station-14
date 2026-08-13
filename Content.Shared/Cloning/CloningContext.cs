using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Rootable;
using Content.Shared.Sericulture;
using Content.Shared.Speech.Components;
using Content.Shared.Storage;
using Content.Shared.Store.Components;
using Content.Shared.Wagging;
using Robust.Shared.Containers;
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
    [Dependency] private EntityQuery<InventoryComponent> _inventoryQuery = default!;
    [Dependency] private EntityQuery<JumpAbilityComponent> _jumpAbilityQuery = default!;
    [Dependency] private EntityQuery<PullerComponent> _pullerQuery = default!;
    [Dependency] private EntityQuery<RootableComponent> _rootableQuery = default!;
    [Dependency] private EntityQuery<SericultureComponent> _sericultureQuery = default!;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;
    [Dependency] private EntityQuery<StoreComponent> _storeQuery = default!;
    [Dependency] private EntityQuery<VocalComponent> _vocalQuery = default!;
    [Dependency] private EntityQuery<WaggingComponent> _waggingQuery = default!;

    // Persistence components
    private BloodstreamComponent? _bloodstream;
    private InventoryComponent? _inventory;
    private JumpAbilityComponent? _jumpAbility;
    private PullerComponent? _puller;
    private RootableComponent? _rootable;
    private SericultureComponent? _sericulture;
    private StorageComponent? _storage;
    private StoreComponent? _store;
    private VocalComponent? _vocal;
    private WaggingComponent? _wagging;

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
    public void CacheTargetComponents(EntityUid target)
    {
        _bloodstreamQuery.TryComp(target, out _bloodstream);
        _inventoryQuery.TryComp(target, out _inventory);
        _jumpAbilityQuery.TryComp(target, out _jumpAbility);
        _pullerQuery.TryComp(target, out _puller);
        _rootableQuery.TryComp(target, out _rootable);
        _sericultureQuery.TryComp(target, out _sericulture);
        _storageQuery.TryComp(target, out _storage);
        _storeQuery.TryComp(target, out _store);
        _vocalQuery.TryComp(target, out _vocal);
        _waggingQuery.TryComp(target, out _wagging);
    }

    /// <summary>
    /// Clears the components from the target object if they exist.
    /// Should be called after CopyTo.
    /// </summary>
    public void ClearTargetComponents()
    {
        _bloodstream = null;
        _inventory = null;
        _jumpAbility = null;
        _puller = null;
        _rootable = null;
        _sericulture = null;
        _storage = null;
        _store = null;
        _vocal = null;
        _wagging = null;
    }

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
        target.NextUpdate = TimeSpan.Zero;
        target.BleedAmount = _bloodstream?.BleedAmount ?? 0.0f;
        target.BloodReferenceSolution = _bloodstream?.BloodReferenceSolution ?? new();
        target.BloodData = _bloodstream?.BloodData;
        target.BloodSolution = _bloodstream?.BloodSolution;
        target.TemporarySolution = _bloodstream?.TemporarySolution;
        target.MetabolitesSolution = _bloodstream?.MetabolitesSolution;
    }
    #endregion Bloodstream

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
        target.Slots = _inventory?.Slots ?? [];
        target.Containers = _inventory?.Containers ?? [];
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
        target.ActionEntity = _jumpAbility?.ActionEntity;
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
        target.Pulling = _puller?.Pulling;
        target.NextThrow = _puller?.NextThrow ?? TimeSpan.Zero;
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
        target.ActionEntity = _rootable?.ActionEntity;
        target.PuddleEntity = _rootable?.PuddleEntity;
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
        target.ActionEntity = _sericulture?.ActionEntity;
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
        target.StoredItems = _storage?.StoredItems ?? new();
        target.SavedLocations = _storage?.SavedLocations ?? new();
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
        target.AccountOwner = _store?.AccountOwner;
        target.FullListingsCatalog = _store?.FullListingsCatalog ?? new();
        target.BoughtEntities = _store?.BoughtEntities ?? new();
        target.BalanceSpent = _store?.BalanceSpent ?? new();
        target.StartingMap = _store?.StartingMap ?? new();
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
        target.EmoteActionEntity = _vocal?.EmoteActionEntity;
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
        target.ActionEntity = _wagging?.ActionEntity;
        target.Wagging = _wagging?.Wagging ?? false;
    }
    #endregion Wagging
}
