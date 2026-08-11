using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Rootable;
using Content.Shared.Sericulture;
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
/// Should write as-is, and clear values on read.
/// </summary>
public sealed partial class CloningContext :
    ISerializationContext,
    ITypeCopier<Component>
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

    public void CopyTo(
        ISerializationManager serializationManager,
        Component source,
        ref Component target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        switch (target)
        {
            case BloodstreamComponent bloodTarget:
                bloodTarget.NextUpdate = TimeSpan.Zero;
                bloodTarget.BleedAmount = 0.0f;
                bloodTarget.BloodData = null;
                bloodTarget.BloodSolution = null;
                bloodTarget.TemporarySolution = null;
                bloodTarget.MetabolitesSolution = null;
                break;
            case InventoryComponent invTarget:
                invTarget.Slots = Array.Empty<SlotDefinition>();
                invTarget.Containers = Array.Empty<ContainerSlot>();
                break;
            case JumpAbilityComponent jumpTarget:
                jumpTarget.ActionEntity = null;
                break;
            case PullerComponent pullTarget:
                pullTarget.Pulling = null;
                pullTarget.NextThrow = TimeSpan.Zero;
                break;
            case RootableComponent rootTarget:
                rootTarget.ActionEntity = null;
                rootTarget.PuddleEntity = null;
                break;
            case SericultureComponent seriTarget:
                seriTarget.ActionEntity = null;
                break;
            case StorageComponent storageTarget:
                storageTarget.StoredItems.Clear();
                storageTarget.SavedLocations.Clear();
                break;
            case StoreComponent storeTarget: // Keep the balance!
                storeTarget.FullListingsCatalog.Clear();
                storeTarget.AccountOwner = null;
                break;
            case WaggingComponent wagTarget:
                wagTarget.ActionEntity = null;
                wagTarget.Wagging = false;
                break;
        }
    }
}
