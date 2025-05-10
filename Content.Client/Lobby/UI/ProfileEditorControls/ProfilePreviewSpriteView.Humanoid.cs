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
    /// <summary>
    /// A slim reload that only updates the entity itself and not any of the job entities, etc.
    /// </summary>
    /// <param name="humanoid">Profile to apply to the dummy</param>
    private void ReloadHumanoidEntity(HumanoidCharacterProfile humanoid)
    {
        if (!_entManager.EntityExists(PreviewDummy) ||
            !_entManager.HasComponent<HumanoidAppearanceComponent>(PreviewDummy))
            return;

        _entManager.System<HumanoidAppearanceSystem>().LoadProfile(PreviewDummy, humanoid);
    }

    /// <summary>
    /// Reloads the entire dummy entity for preview.
    /// </summary>
    /// <remarks>
    /// This is expensive so not recommended to run if you have a slider.
    /// </remarks>
    /// <param name="humanoid">Profile to load</param>
    /// <param name="job">Force job clothes override -- don't use job preferences</param>
    /// <param name="showClothes">Add job clothes or just spawn a species doll</param>
    private void LoadHumanoidEntity(HumanoidCharacterProfile humanoid, JobPrototype? job, bool showClothes)
    {
        ProfileName = humanoid.Name;
        JobName = null;
        LoadoutName = null;

        job ??= GetPreferredJob(humanoid);

        RoleLoadout? loadout;

        if(job != null)
        {
            loadout = humanoid.GetLoadoutOrDefault(
                LoadoutSystem.GetJobPrototype(job.ID),
                _playerManager.LocalSession,
                humanoid.Species,
                _entManager,
                _prototypeManager);

            // If the job has a preview specific entity or a job specific entity use that
            var previewEntity = job.JobPreviewEntity ?? (EntProtoId?)job.JobEntity;

            if (previewEntity != null)
            {
                // This is currently for borg and AI
                PreviewDummy = _entManager.SpawnEntity(previewEntity, MapCoordinates.Nullspace);
                JobName = job.LocalizedName;
                // Grab the loadout specific name too!
                LoadoutName = GetLoadoutName(loadout);
                return;
            }
        }

        // No job specific entities, we should spawn a humanoid
        PreviewDummy = _entManager.SpawnEntity(
            _prototypeManager.Index(humanoid.Species).DollPrototype,
            MapCoordinates.Nullspace);

        // Bail now if all we need is the naked doll
        if (!showClothes)
            return;

        // If we don't have an overridden job and the profile has NO job perefences, check for an antag preview
        if (job == null && humanoid.JobPreferences.Count == 0)
        {
            // Search the preferences for an antag with "PreviewStartingGear" defined
            foreach(var antag in humanoid.AntagPreferences)
            {
                if (!_prototypeManager.TryIndex(antag, out var antagProto))
                    continue;
                if (!antagProto.PreviewStartingGear.HasValue)
                    continue;

                // We found an antag to dress as! Set it and return.
                GiveDummyAntagLoadout(antagProto);
                JobName = Loc.GetString(antagProto.Name);
                return;
            }
        }

        if (job == null)
        {
            // We STILL don't have a job, use fallback and don't set "JobName" (we don't want to display Passenger)
            job = _prototypeManager.Index<JobPrototype>(SharedGameTicker.FallbackOverflowJob);
        }
        else
        {
            JobName = job.LocalizedName;
        }
        GiveDummyJobClothes(PreviewDummy, humanoid, job);

        if (!_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID)))
            return;

        loadout = humanoid.GetLoadoutOrDefault(
            LoadoutSystem.GetJobPrototype(job.ID),
            _playerManager.LocalSession,
            humanoid.Species,
            _entManager,
            _prototypeManager);

        LoadoutName = GetLoadoutName(loadout);

        GiveDummyLoadout(PreviewDummy, loadout);

        ReloadHumanoidEntity(humanoid);
    }

    /// <summary>
    /// Gets the highest priority job for the profile.
    /// If there is one job set, always return that.
    /// Otherwise, from the set of enabled jobs on this profile, return "High" priority job, otherwise,
    ///     the first "Medium" priority job found, etc.
    /// </summary>
    /// <param name="profile">Profile to get job for</param>
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

    private string? GetLoadoutName(RoleLoadout loadout)
    {
        if (_prototypeManager.TryIndex(loadout.Role, out var roleLoadoutPrototype) &&
            roleLoadoutPrototype.CanCustomizeName)
            return loadout.EntityName;
        return null;
    }

    /// <summary>
    /// Apply PreviewStartingGear from antag prototype to the dummy.
    /// </summary>
    /// <param name="antag"></param>
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

    /// <summary>
    /// Give player's role loadout to the dummy.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="roleLoadout"></param>
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
