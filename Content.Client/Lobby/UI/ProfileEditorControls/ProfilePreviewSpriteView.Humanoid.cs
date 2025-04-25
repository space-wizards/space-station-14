using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Station;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{

    private void ReloadHumanoidEntity(HumanoidCharacterProfile humanoid)
    {
        if (!_entManager.EntityExists(PreviewDummy) ||
            !_entManager.HasComponent<HumanoidAppearanceComponent>(PreviewDummy))
            return;

        _entManager.System<HumanoidAppearanceSystem>().LoadProfile(PreviewDummy, humanoid);
    }

    private void LoadHumanoidEntity(HumanoidCharacterProfile humanoid, JobPrototype? job, bool showClothes)
    {
        JobName = null;

        if (job == null && humanoid.JobPreferences.Count == 0)
        {
            foreach(var antag in humanoid.AntagPreferences)
            {
                if (!_prototypeManager.TryIndex(antag, out var antagProto))
                    continue;
                if (!antagProto.PreviewStartingGear.HasValue)
                    continue;

                GiveDummyAntagLoadout(antagProto);
                JobName = Loc.GetString(antagProto.Name);
                return;
            }
        }
        job ??= GetPreferredJob(humanoid);

        EntProtoId? previewEntity = null;
        if(job != null)
        {
            previewEntity = job.JobPreviewEntity ?? (EntProtoId?)job.JobEntity;

            if (previewEntity != null)
            {
                PreviewDummy = _entManager.SpawnEntity(previewEntity, MapCoordinates.Nullspace);
                JobName = job.LocalizedName;
                return;
            }
        }

        PreviewDummy = _entManager.SpawnEntity(
            _prototypeManager.Index(humanoid.Species).DollPrototype,
            MapCoordinates.Nullspace);

        if (!showClothes)
            return;

        if (job == null)
        {
            job = _prototypeManager.Index<JobPrototype>(SharedGameTicker.FallbackOverflowJob);
        }
        else
        {
            JobName = job.LocalizedName;
        }
        GiveDummyJobClothes(PreviewDummy, humanoid, job);

        if (!_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID)))
            return;

        var loadout = humanoid.GetLoadoutOrDefault(
            LoadoutSystem.GetJobPrototype(job.ID),
            _playerManager.LocalSession,
            humanoid.Species,
            _entManager,
            _prototypeManager);

        GiveDummyLoadout(PreviewDummy, loadout);

        ReloadHumanoidEntity(humanoid);
    }

    /// <summary>
    /// Gets the highest priority job for the profile.
    /// </summary>
    private JobPrototype? GetPreferredJob(HumanoidCharacterProfile profile)
    {
        ProtoId<JobPrototype> highPriorityJob = default;
        if (profile.JobPreferences.Count == 1)
        {
            highPriorityJob = profile.JobPreferences.First();
        }
        else
        {
            var priorities = _preferencesManager.Preferences?.JobPriorities ?? [];
            foreach (var priority in new List<JobPriority>{JobPriority.High, JobPriority.Medium, JobPriority.Low})
            {
                highPriorityJob = profile.JobPreferences.FirstOrDefault(p => priorities.GetValueOrDefault(p) == priority);
                if (highPriorityJob.Id != null)
                    break;
            }
        }
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract (what is resharper smoking?)
        return highPriorityJob.Id == null ? null : _prototypeManager.Index(highPriorityJob);
    }

    private void GiveDummyAntagLoadout(AntagPrototype antag)
    {
        if (!antag.PreviewStartingGear.HasValue)
            return;

        var spawnSys = _entManager.System<StationSpawningSystem>();

        spawnSys.EquipStartingGear(PreviewDummy, antag.PreviewStartingGear);
    }

    /// <summary>
    /// Applies the specified job's clothes to the dummy.
    /// </summary>
    private void GiveDummyJobClothes(EntityUid dummy, HumanoidCharacterProfile profile, JobPrototype job)
    {
        var inventorySys = _entManager.System<InventorySystem>();
        if (!inventorySys.TryGetSlots(dummy, out var slots))
            return;

        // Apply loadout
        if (profile.Loadouts.TryGetValue(job.ID, out var jobLoadout))
        {
            foreach (var loadouts in jobLoadout.SelectedLoadouts.Values)
            {
                foreach (var loadout in loadouts)
                {
                    if (!_prototypeManager.TryIndex(loadout.Prototype, out var loadoutProto))
                        continue;

                    // TODO: Need some way to apply starting gear to an entity and replace existing stuff coz holy fucking shit dude.
                    foreach (var slot in slots)
                    {
                        // Try startinggear first
                        if (_prototypeManager.TryIndex(loadoutProto.StartingGear, out var loadoutGear))
                        {
                            var itemType = ((IEquipmentLoadout) loadoutGear).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                _entManager.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = _entManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(dummy, item, slot.Name, true, true);
                            }
                        }
                        else
                        {
                            var itemType = ((IEquipmentLoadout) loadoutProto).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                _entManager.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = _entManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(dummy, item, slot.Name, true, true);
                            }
                        }
                    }
                }
            }
        }

        if (!_prototypeManager.TryIndex(job.StartingGear, out var gear))
            return;

        foreach (var slot in slots)
        {
            var itemType = ((IEquipmentLoadout) gear).GetGear(slot.Name);

            if (inventorySys.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
            {
                _entManager.DeleteEntity(unequippedItem.Value);
            }

            if (itemType != string.Empty)
            {
                var item = _entManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                inventorySys.TryEquip(dummy, item, slot.Name, true, true);
            }
        }
    }

    private void GiveDummyLoadout(EntityUid uid, RoleLoadout? roleLoadout)
    {
        if (roleLoadout == null)
            return;

        var spawnSys = _entManager.System<StationSpawningSystem>();

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.TryIndex(loadout.Prototype, out var loadoutProto))
                    continue;

                spawnSys.EquipStartingGear(uid, loadoutProto);
            }
        }
    }
}
