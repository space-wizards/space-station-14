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

    // Bloodstream persistence fields
    private float _bleedAmount;
    private Solution? _bloodReferenceSolution;
    private List<ReagentData>? _bloodData;
    private Entity<SolutionComponent>? _bloodSolution;
    private Entity<SolutionComponent>? _temporarySolution;
    private Entity<SolutionComponent>? _metabolitesSolution;

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
    /// Takes fields to be maintained on a given object before it's copied.
    /// Should be called before CopyTo.
    /// </summary>
    public void GrabPersistentFields(EntityUid target)
    {
        if (_bloodstreamQuery.TryComp(target, out var bloodstream))
        {
            _bleedAmount = bloodstream.BleedAmount;
            _bloodReferenceSolution = bloodstream.BloodReferenceSolution;
            _bloodData = bloodstream.BloodData;
            _bloodSolution = bloodstream.BloodSolution;
            _temporarySolution = bloodstream.TemporarySolution;
            _metabolitesSolution = bloodstream.MetabolitesSolution;
        }
        else
        {
            _bleedAmount = 0.0f;
            _bloodReferenceSolution = null;
            _bloodData = null;
            _bloodSolution = null;
            _temporarySolution = null;
            _metabolitesSolution = null;
        }
    }

    public void ClearPersistentFields()
    {
        _bleedAmount = 0.0f;
        _bloodReferenceSolution = null;
        _bloodData = null;
        _bloodSolution = null;
        _temporarySolution = null;
        _metabolitesSolution = null;
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

        target.NextUpdate = TimeSpan.Zero;

        // Persistent fields
        target.BleedAmount = _bleedAmount;
        if (_bloodReferenceSolution != null)
            target.BloodReferenceSolution = _bloodReferenceSolution;
        target.BloodData = _bloodData;
        target.BloodSolution = _bloodSolution;
        target.TemporarySolution = _temporarySolution;
        target.MetabolitesSolution = _metabolitesSolution;
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

        target.Slots = Array.Empty<SlotDefinition>();
        target.Containers = Array.Empty<ContainerSlot>();
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

        target.ActionEntity = null;
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

        target.Pulling = null;
        target.NextThrow = TimeSpan.Zero;
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

        target.ActionEntity = null;
        target.PuddleEntity = null;
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

        target.ActionEntity = null;
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

        target.StoredItems.Clear();
        target.SavedLocations.Clear();
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

        target.AccountOwner = null;
        target.FullListingsCatalog.Clear();
        target.BoughtEntities.Clear();
        target.BalanceSpent.Clear();
        target.StartingMap = null;
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

        target.EmoteActionEntity = null;
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

        target.ActionEntity = null;
        target.Wagging = false;
    }
    #endregion Wagging
}
