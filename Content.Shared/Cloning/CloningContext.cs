using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Cloning;

/// <summary>
/// A context to be used for copying components over to a cloned entity.
/// Custom cloning logic per component should be implemented here!
/// Should write as-is, and clear values on read.
/// </summary>
public sealed partial class CloningContext :
    ISerializationContext,
    ITypeCopier<BloodstreamComponent>,
    ITypeCopier<InventoryComponent>,
    ITypeCopier<PullerComponent>,
    ITypeCopier<StorageComponent>
{
    /// <inheritdoc />
    public SerializationManager.SerializerProvider SerializerProvider { get; }

    /// <inheritdoc />
    public bool WritingReadingPrototypes { get; set; }

    [Dependency] private ILogManager _logMan = default!;

    private ISawmill _log;

    public CloningContext(IDependencyCollection dependency, ISerializationManager ser)
    {
        dependency.InjectDependencies(this);

        _log = _logMan.GetSawmill("cloning_context");

        SerializerProvider = new(ser);
        SerializerProvider.RegisterSerializer(this);
    }

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
        target.BleedAmount = 0.0f;
        target.BloodData = null;
        target.BloodSolution = null;
        target.TemporarySolution = null;
        target.MetabolitesSolution = null;
    }

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
}
