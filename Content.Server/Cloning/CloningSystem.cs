using Content.Server.Humanoid;
using Content.Shared.Administration.Logs;
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.NameModifier.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Cloning;

/// <summary>
///     System responsible for making a copy of a humanoid's body.
///     For the cloning machines themselves look at CloningPodSystem, CloningConsoleSystem and MedicalScannerSystem instead.
/// </summary>
public sealed class CloningSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _subdermalImplant = default!;

    /// <summary>
    ///     Spawns a clone of the given humanoid mob at the specified location or in nullspace.
    /// </summary>
    public bool TryCloning(EntityUid original, MapCoordinates? coords, ProtoId<CloningSettingsPrototype> settingsId, [NotNullWhen(true)] out EntityUid? clone)
    {
        clone = null;
        if (!_prototype.TryIndex(settingsId, out var settings))
            return false; // invalid settings

        if (!TryComp<HumanoidAppearanceComponent>(original, out var humanoid))
            return false; // whatever body was to be cloned, was not a humanoid

        if (!_prototype.TryIndex(humanoid.Species, out var speciesPrototype))
            return false; // invalid species

        var attemptEv = new CloningAttemptEvent(settings);
        RaiseLocalEvent(original, ref attemptEv);
        if (attemptEv.Cancelled && !settings.ForceCloning)
            return false; // cannot clone, for example due to the unrevivable trait

        clone = coords == null ? Spawn(speciesPrototype.Prototype) : Spawn(speciesPrototype.Prototype, coords.Value);
        _humanoidSystem.CloneAppearance(original, clone.Value);

        var componentsToCopy = settings.Components;

        // don't make status effects permanent
        if (TryComp<StatusEffectsComponent>(original, out var statusComp))
            componentsToCopy.ExceptWith(statusComp.ActiveEffects.Values.Select(s => s.RelevantComponent).Where(s => s != null)!);

        foreach (var componentName in componentsToCopy)
        {
            if (!_componentFactory.TryGetRegistration(componentName, out var componentRegistration))
            {
                Log.Error($"Tried to use invalid component registration for cloning: {componentName}");
                continue;
            }

            if (EntityManager.TryGetComponent(original, componentRegistration.Type, out var sourceComp)) // Does the original have this component?
            {
                if (HasComp(clone.Value, componentRegistration.Type)) // CopyComp cannot overwrite existing components
                    RemComp(clone.Value, componentRegistration.Type);
                CopyComp(original, clone.Value, sourceComp);
            }
        }

        var cloningEv = new CloningEvent(settings, clone.Value);
        RaiseLocalEvent(original, ref cloningEv); // used for datafields that cannot be directly copied

        // Add equipment first so that SetEntityName also renames the ID card.
        if (settings.CopyEquipment != null)
            CopyEquipment(original, clone.Value, settings.CopyEquipment.Value, settings.Whitelist, settings.Blacklist);

        // Copy storage on the mob itself as well.
        // This is needed for slime storage.
        if (settings.CopyInternalStorage)
            CopyStorage(original, clone.Value, settings.Whitelist, settings.Blacklist);

        // copy implants and their storage contents
        if (settings.CopyImplants)
            CopyImplants(original, clone.Value, settings.CopyInternalStorage, settings.Whitelist, settings.Blacklist);

        var originalName = Name(original);
        if (TryComp<NameModifierComponent>(original, out var nameModComp)) // if the originals name was modified, use the unmodified name
            originalName = nameModComp.BaseName;

        // This will properly set the BaseName and EntityName for the clone.
        // Adding the component first before renaming will make sure RefreshNameModifers is called.
        // Without this the name would get reverted to Urist.
        // If the clone has no name modifiers, NameModifierComponent will be removed again.
        EnsureComp<NameModifierComponent>(clone.Value);
        _metaData.SetEntityName(clone.Value, originalName);

        _adminLogger.Add(LogType.Chat, LogImpact.Medium, $"The body of {original:player} was cloned as {clone.Value:player}");
        return true;
    }

    /// <summary>
    ///     Copies the equipment the original has to the clone.
    ///     This uses the original prototype of the items, so any changes to components that are done after spawning are lost!
    /// </summary>
    public void CopyEquipment(Entity<InventoryComponent?> original, Entity<InventoryComponent?> clone, SlotFlags slotFlags, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        if (!Resolve(original, ref original.Comp) || !Resolve(clone, ref clone.Comp))
            return;

        var coords = Transform(clone).Coordinates;

        // Iterate over all inventory slots
        var slotEnumerator = _inventory.GetSlotEnumerator(original, slotFlags);
        while (slotEnumerator.NextItem(out var item, out var slot))
        {
            var cloneItem = CopyItem(item, coords, whitelist, blacklist);

            if (cloneItem != null && !_inventory.TryEquip(clone, cloneItem.Value, slot.Name, silent: true, inventory: clone.Comp))
                Del(cloneItem); // delete it again if the clone cannot equip it
        }
    }

    /// <summary>
    ///     Copies an item and its storage recursively, placing all items at the same position in grid storage.
    ///     This uses the original prototype of the items, so any changes to components that are done after spawning are lost!
    /// </summary>
    /// <remarks>
    ///     This is not perfect and only considers item in storage containers.
    ///     Some components have their own additional spawn logic on map init, so we cannot just copy all containers.
    /// </remarks>
    public EntityUid? CopyItem(EntityUid original, EntityCoordinates coords, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        // we use a whitelist and blacklist to be sure to exclude any problematic entities
        if (!_whitelist.CheckBoth(original, blacklist, whitelist))
            return null;

        var prototype = MetaData(original).EntityPrototype?.ID;
        if (prototype == null)
            return null;

        var spawned = EntityManager.SpawnAtPosition(prototype, coords);

        // if the original is a stack, adjust the count of the copy
        if (TryComp<StackComponent>(original, out var originalStack) && TryComp<StackComponent>(spawned, out var spawnedStack))
            _stack.SetCount(spawned, originalStack.Count, spawnedStack);

        // if the original has items inside its storage, copy those as well
        if (TryComp<StorageComponent>(original, out var originalStorage) && TryComp<StorageComponent>(spawned, out var spawnedStorage))
        {
            // remove all items that spawned with the entity inside its storage
            // this ignores other containers, but this should be good enough for our purposes
            _container.CleanContainer(spawnedStorage.Container);

            // recursively replace them
            // surely no one will ever create two items that contain each other causing an infinite loop, right?
            foreach ((var itemUid, var itemLocation) in originalStorage.StoredItems)
            {
                var copy = CopyItem(itemUid, coords, whitelist, blacklist);
                if (copy != null)
                    _storage.InsertAt((spawned, spawnedStorage), copy.Value, itemLocation, out _, playSound: false);
            }
        }

        return spawned;
    }

    /// <summary>
    ///     Copies an item's storage recursively to another storage.
    ///     The storage grids should have the same shape or it will drop on the floor.
    ///     Basically the same as CopyItem, but we don't copy the outermost container.
    /// </summary>
    public void CopyStorage(Entity<StorageComponent?> original, Entity<StorageComponent?> target, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        if (!Resolve(original, ref original.Comp, false) || !Resolve(target, ref target.Comp, false))
            return;

        var coords = Transform(target).Coordinates;

        // delete all items in the target storage
        _container.CleanContainer(target.Comp.Container);

        // recursively replace them
        foreach ((var itemUid, var itemLocation) in original.Comp.StoredItems)
        {
            var copy = CopyItem(itemUid, coords, whitelist, blacklist);
            if (copy != null)
                _storage.InsertAt(target, copy.Value, itemLocation, out _, playSound: false);
        }
    }

    /// <summary>
    ///     Copies all implants from one mob to another.
    ///     Might result in duplicates if the target already has them.
    ///     Can copy the storage inside a storage implant according to a whitelist and blacklist.
    /// </summary>
    /// <param name="original">Entity to copy implants from.</param>
    /// <param name="target">Entity to copy implants to.</param>
    /// <param name="copyStorage">If true will copy storage of the implants (E.g storage implant)</param>
    /// <param name="whitelist">Whitelist for the storage copy (If copyStorage is true)</param>
    /// <param name="blacklist">Blacklist for the storage copy (If copyStorage is true)</param>
    public void CopyImplants(Entity<ImplantedComponent?> original, EntityUid target, bool copyStorage = false, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        if (!Resolve(original, ref original.Comp, false))
            return; // they don't have any implants to copy!

        foreach (var originalImplant in original.Comp.ImplantContainer.ContainedEntities)
        {
            if (!HasComp<SubdermalImplantComponent>(originalImplant))
                continue; // not an implant (should only happen with admin shenanigans)

            var implantId = MetaData(originalImplant).EntityPrototype?.ID;

            if (implantId == null)
                continue;

            var targetImplant = _subdermalImplant.AddImplant(target, implantId);

            if (copyStorage && targetImplant != null)
                CopyStorage(originalImplant, targetImplant.Value, whitelist, blacklist); // only needed for storage implants
        }

    }
}
