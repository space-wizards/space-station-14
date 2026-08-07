using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Rootable;
using Content.Shared.Sericulture;
using Content.Shared.Storage;
using Content.Shared.Wagging;
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
    ITypeCopier<JumpAbilityComponent>,
    ITypeCopier<PullerComponent>,
    ITypeCopier<RootableComponent>,
    ITypeCopier<SericultureComponent>,
    ITypeCopier<StorageComponent>,
    ITypeCopier<WaggingComponent>
{
    /// <inheritdoc />
    public SerializationManager.SerializerProvider SerializerProvider { get; }

    /// <inheritdoc />
    public bool WritingReadingPrototypes { get; set; }

    public CloningContext(IDependencyCollection dependency, ISerializationManager ser)
    {
        dependency.InjectDependencies(this);

        SerializerProvider = new(ser);
        SerializerProvider.RegisterSerializer(this);
    }

    #region Bloodstream
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
    #endregion Bloodstream

    #region Inventory
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

    #region Puller
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
    #endregion Puller

    #region Rootable
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

    #region Wagging
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
