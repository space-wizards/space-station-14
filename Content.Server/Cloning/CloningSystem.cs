using Content.Server.Humanoid;
using Content.Shared.Administration.Logs;
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.NameModifier.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Whitelist;
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
    public void CopyEquipment(EntityUid original, EntityUid clone, SlotFlags slotFlags, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        if (!TryComp<InventoryComponent>(original, out var originalInventory) || !TryComp<InventoryComponent>(clone, out var cloneInventory))
            return;
        // Iterate over all inventory slots
        var slotEnumerator = _inventory.GetSlotEnumerator((original, originalInventory), slotFlags);
        while (slotEnumerator.NextItem(out var item, out var slot))
        {
            // Spawn a copy of the item using the original prototype.
            // This means any changes done to the item after spawning will be reset, but that should not be a problem for simple items like clothing etc.
            // we use a whitelist and blacklist to be sure to exclude any problematic entities

            if (_whitelist.IsWhitelistFail(whitelist, item) || _whitelist.IsBlacklistPass(blacklist, item))
                continue;

            var prototype = MetaData(item).EntityPrototype;
            if (prototype != null)
                _inventory.SpawnItemInSlot(clone, slot.Name, prototype.ID, silent: true, inventory: cloneInventory);
        }
    }
}
