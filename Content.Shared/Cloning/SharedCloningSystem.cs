using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Cloning;

/// <summary>
/// System responsible for making a copy of a humanoid's body.
/// For the cloning machines themselves look at CloningPodSystem, CloningConsoleSystem and MedicalScannerSystem instead.
/// </summary>
public abstract partial class SharedCloningSystem : EntitySystem
{
    /// <summary>
    /// Spawns a clone of the given humanoid mob at the specified location or in nullspace.
    /// </summary>
    public virtual bool TryCloning(EntityUid original, MapCoordinates? coords, ProtoId<CloningSettingsPrototype> settingsId, [NotNullWhen(true)] out EntityUid? clone)
    {
        clone = null;
        return false;
    }

    /// <summary>
    /// Copies the equipment the original has to the clone.
    /// This uses the original prototype of the items, so any changes to components that are done after spawning are lost!
    /// </summary>
    public virtual void CopyEquipment(Entity<InventoryComponent?> original, Entity<InventoryComponent?> clone, SlotFlags slotFlags, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
    }

    /// <summary>
    /// Copies an item and its storage recursively, placing all items at the same position in grid storage.
    /// This uses the original prototype of the items, so any changes to components that are done after spawning are lost!
    /// </summary>
    /// <remarks>
    /// This is not perfect and only considers item in storage containers.
    /// Some components have their own additional spawn logic on map init, so we cannot just copy all containers.
    /// </remarks>
    public virtual EntityUid? CopyItem(EntityUid original, EntityCoordinates coords, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        return null;
    }

    /// <summary>
    /// Copies an item's storage recursively to another storage.
    /// The storage grids should have the same shape or it will drop on the floor.
    /// Basically the same as CopyItem, but we don't copy the outermost container.
    /// </summary>
    public virtual void CopyStorage(Entity<StorageComponent?> original, Entity<StorageComponent?> target, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
    }

    /// <summary>
    /// Copies all implants from one mob to another.
    /// Might result in duplicates if the target already has them.
    /// Can copy the storage inside a storage implant according to a whitelist and blacklist.
    /// </summary>
    /// <param name="original">Entity to copy implants from.</param>
    /// <param name="target">Entity to copy implants to.</param>
    /// <param name="copyStorage">If true will copy storage of the implants (E.g storage implant)</param>
    /// <param name="whitelist">Whitelist for the storage copy (If copyStorage is true)</param>
    /// <param name="blacklist">Blacklist for the storage copy (If copyStorage is true)</param>
    public virtual void CopyImplants(Entity<ImplantedComponent?> original, EntityUid target, bool copyStorage = false, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
    }

    /// <summary>
    /// Scans all permanent status effects applied to the original entity and transfers them to the clone.
    /// </summary>
    public virtual void CopyStatusEffects(Entity<StatusEffectContainerComponent?> original, Entity<StatusEffectContainerComponent?> target)
    {
    }

    /// <summary>
    /// Copy components from one entity to another based on a CloningSettingsPrototype.
    /// </summary>
    /// <param name="original">The orignal Entity to clone components from.</param>
    /// <param name="clone">The target Entity to clone components to.</param>
    /// <param name="settings">The clone settings prototype containing the list of components to clone.</param>
    public virtual void CloneComponents(EntityUid original, EntityUid clone, CloningSettingsPrototype settings)
    {
    }

    /// <summary>
    /// Copy components from one entity to another based on a CloningSettingsPrototype.
    /// </summary>
    /// <param name="original">The orignal Entity to clone components from.</param>
    /// <param name="clone">The target Entity to clone components to.</param>
    /// <param name="settings">The clone settings prototype id containing the list of components to clone.</param>
    public virtual void CloneComponents(EntityUid original, EntityUid clone, ProtoId<CloningSettingsPrototype> settings)
    {
    }
}
