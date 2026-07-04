using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Station;
using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    /// <summary>
    /// A slim reload that only updates the entity itself and not any of the job entities, etc.
    /// </summary>
    private void ReloadHumanoidEntity(HumanoidCharacterProfile humanoid)
    {
        if (!EntMan.EntityExists(PreviewDummy) ||
            !EntMan.HasComponent<VisualBodyComponent>(PreviewDummy))
            return;

        EntMan.System<SharedVisualBodySystem>().ApplyProfileTo(PreviewDummy, humanoid);
    }

    /// <summary>
    /// Loads the profile onto a dummy entity.
    /// </summary>
    private void LoadHumanoidEntity(HumanoidCharacterProfile? humanoid, JobPrototype? job, bool jobClothes)
    {
        EntProtoId? previewEntity = null;
        if (humanoid != null && jobClothes)
        {
            job ??= GetPreferredJob(humanoid);

            previewEntity = job.JobPreviewEntity ?? (EntProtoId?)job?.JobEntity;
        }

        if (previewEntity != null)
        {
            // Special type like borg or AI, do not spawn a human just spawn the entity.
            PreviewDummy = EntMan.SpawnEntity(previewEntity, MapCoordinates.Nullspace);
        }
        else if (humanoid is not null)
        {
            var dummy = _prototypeManager.Index(humanoid.Species).DollPrototype;
            PreviewDummy = EntMan.SpawnEntity(dummy, MapCoordinates.Nullspace);
            EntMan.System<SharedVisualBodySystem>().ApplyProfileTo(PreviewDummy, humanoid);
        }
        else
        {
            PreviewDummy = EntMan.SpawnEntity(_prototypeManager.Index(HumanoidCharacterProfile.DefaultSpecies).DollPrototype, MapCoordinates.Nullspace);
        }

        if (humanoid != null && jobClothes)
        {
            DebugTools.Assert(job != null);

            GiveDummyJobClothes(humanoid, job);

            if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID)))
            {
                var loadout = humanoid.GetLoadoutOrDefault(LoadoutSystem.GetJobPrototype(job.ID), _playerManager.LocalSession, humanoid.Species, EntMan, _prototypeManager);
                GiveDummyLoadout(loadout);
            }
        }
    }

    /// <summary>
    /// Gets the highest priority job for the profile.
    /// </summary>
    private JobPrototype GetPreferredJob(HumanoidCharacterProfile profile)
    {
        var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract (what is resharper smoking?)
        return _prototypeManager.Index<JobPrototype>(highPriorityJob.Id ?? SharedGameTicker.FallbackOverflowJob);
    }

    private void GiveDummyLoadout(RoleLoadout? roleLoadout)
    {
        if (roleLoadout == null)
            return;

        var spawnSys = EntMan.System<StationSpawningSystem>();

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                    continue;

                spawnSys.EquipStartingGear(PreviewDummy, loadoutProto);
            }
        }
    }

    /// <summary>
    /// Applies the specified job's clothes to the dummy.
    /// </summary>
    private void GiveDummyJobClothes(HumanoidCharacterProfile profile, JobPrototype job)
    {
        var inventorySys = EntMan.System<InventorySystem>();
        if (!inventorySys.TryGetSlots(PreviewDummy, out var slots))
            return;

        // Apply loadout
        if (profile.Loadouts.TryGetValue(job.ID, out var jobLoadout))
        {
            foreach (var loadouts in jobLoadout.SelectedLoadouts.Values)
            {
                foreach (var loadout in loadouts)
                {
                    if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                        continue;

                    // TODO: Need some way to apply starting gear to an entity and replace existing stuff coz holy fucking shit dude.
                    foreach (var slot in slots)
                    {
                        // Try startinggear first
                        if (_prototypeManager.Resolve(loadoutProto.StartingGear, out var loadoutGear))
                        {
                            var itemType = ((IEquipmentLoadout) loadoutGear).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                EntMan.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
                            }
                        }
                        else
                        {
                            var itemType = ((IEquipmentLoadout) loadoutProto).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                EntMan.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
                            }
                        }
                    }
                }
            }
        }

        if (!_prototypeManager.Resolve(job.StartingGear, out var gear))
            return;

        foreach (var slot in slots)
        {
            var itemType = ((IEquipmentLoadout) gear).GetGear(slot.Name);

            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
            {
                EntMan.DeleteEntity(unequippedItem.Value);
            }

            if (itemType != string.Empty)
            {
                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
            }
        }
    }
}
