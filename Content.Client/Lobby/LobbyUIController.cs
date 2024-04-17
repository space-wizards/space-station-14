using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Client.Lobby.UI;
using Content.Client.Preferences;
using Content.Client.Preferences.UI;
using Content.Client.Station;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Roles;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby;

public sealed class LobbyUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [UISystemDependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [UISystemDependency] private readonly ClientInventorySystem _inventory = default!;
    [UISystemDependency] private readonly StationSpawningSystem _spawn = default!;

    private LobbyCharacterPreviewPanel? _previewPanel;

    private bool _showClothes = true;

    /*
     * Each character profile has its own dummy. There is also a dummy for the lobby screen + character editor
     * that is shared too.
     */

    /// <summary>
    /// Preview dummy for role gear.
    /// </summary>
    private EntityUid? _previewDummy;

    /// <summary>
    /// If we currently have a job prototype selected.
    /// </summary>
    private JobPrototype? _dummyJob;

    // TODO: Load the species directly and don't update entity ever.
    public event Action<EntityUid>? PreviewDummyUpdated;

    private HumanoidCharacterProfile? _profile;

    public override void Initialize()
    {
        base.Initialize();
        _preferencesManager.OnServerDataLoaded += PreferencesDataLoaded;
    }

    private void PreferencesDataLoaded()
    {
        UpdateProfile();
    }

    public void OnStateEntered(LobbyState state)
    {
    }

    public void OnStateExited(LobbyState state)
    {
        EntityManager.DeleteEntity(_previewDummy);
        _previewDummy = null;
    }

    public void SetPreviewPanel(LobbyCharacterPreviewPanel? panel)
    {
        _previewPanel = panel;
        ReloadProfile();
    }

    public void SetClothes(bool value)
    {
        if (_showClothes == value)
            return;

        _showClothes = value;
        ReloadCharacterUI();
    }

    public void SetDummyJob(JobPrototype? job)
    {
        _dummyJob = job;
        ReloadCharacterUI();
    }

    /// <summary>
    /// Updates the character only with the specified profile change.
    /// </summary>
    public void ReloadProfile()
    {
        // Test moment
        if (_profile == null || _stateManager.CurrentState is not LobbyState)
            return;

        // Ignore job clothes and the likes so we don't spam entities out every frame of color changes.
        var previewDummy = EnsurePreviewDummy(_profile);
        _humanoid.LoadProfile(previewDummy, _profile);
    }

    /// <summary>
    /// Updates the currently selected character's preview.
    /// </summary>
    public void ReloadCharacterUI()
    {
        // Test moment
        if (_profile == null || _stateManager.CurrentState is not LobbyState)
            return;

        EntityManager.DeleteEntity(_previewDummy);
        _previewDummy = null;
        _previewDummy = EnsurePreviewDummy(_profile);
        _previewPanel?.SetSprite(_previewDummy.Value);
        _previewPanel?.SetSummaryText(_profile.Summary);
        _humanoid.LoadProfile(_previewDummy.Value, _profile);

        if (_showClothes)
            GiveDummyJobClothesLoadout(_previewDummy.Value, _profile);
    }

    /// <summary>
    /// Updates character profile to the default.
    /// </summary>
    public void UpdateProfile()
    {
        if (!_preferencesManager.ServerDataLoaded)
        {
            _profile = null;
            return;
        }

        if (_preferencesManager.Preferences?.SelectedCharacter is HumanoidCharacterProfile selectedCharacter)
        {
            _profile = selectedCharacter;
            _previewPanel?.SetLoaded(true);
        }
        else
        {
            _previewPanel?.SetSummaryText(string.Empty);
            _previewPanel?.SetLoaded(false);
        }

        ReloadCharacterUI();
    }

    public void UpdateProfile(HumanoidCharacterProfile? profile)
    {
        if (_profile?.Equals(profile) == true)
            return;

        if (_stateManager.CurrentState is not LobbyState)
            return;

        _profile = profile;
    }

    private EntityUid EnsurePreviewDummy(HumanoidCharacterProfile profile)
    {
        if (_previewDummy != null)
            return _previewDummy.Value;

        _previewDummy = EntityManager.SpawnEntity(_prototypeManager.Index<SpeciesPrototype>(profile.Species).DollPrototype, MapCoordinates.Nullspace);
        PreviewDummyUpdated?.Invoke(_previewDummy.Value);
        return _previewDummy.Value;
    }

    /// <summary>
    /// Applies the highest priority job's clothes to the dummy.
    /// </summary>
    public void GiveDummyJobClothesLoadout(EntityUid dummy, HumanoidCharacterProfile profile)
    {
        var job = _dummyJob ?? GetPreferredJob(profile);
        GiveDummyJobClothes(dummy, profile, job);

        if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID)))
        {
            var loadout = profile.GetLoadoutOrDefault(LoadoutSystem.GetJobPrototype(job.ID), EntityManager, _prototypeManager);
            GiveDummyLoadout(dummy, loadout);
        }
    }

    /// <summary>
    /// Gets the highest priority job for the profile.
    /// </summary>
    public JobPrototype GetPreferredJob(HumanoidCharacterProfile profile)
    {
        var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract (what is resharper smoking?)
        return _prototypeManager.Index<JobPrototype>(highPriorityJob ?? SharedGameTicker.FallbackOverflowJob);
    }

    public void GiveDummyLoadout(EntityUid uid, RoleLoadout? roleLoadout)
    {
        if (roleLoadout == null)
            return;

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.TryIndex(loadout.Prototype, out var loadoutProto))
                    continue;

                _spawn.EquipStartingGear(uid, _prototypeManager.Index(loadoutProto.Equipment));
            }
        }
    }

    /// <summary>
    /// Applies the specified job's clothes to the dummy.
    /// </summary>
    public void GiveDummyJobClothes(EntityUid dummy, HumanoidCharacterProfile profile, JobPrototype job)
    {
        if (!_inventory.TryGetSlots(dummy, out var slots))
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

                    // TODO: Need some way to apply starting gear to an entity coz holy fucking shit dude.
                    var loadoutGear = _prototypeManager.Index(loadoutProto.Equipment);

                    foreach (var slot in slots)
                    {
                        var itemType = loadoutGear.GetGear(slot.Name);

                        if (_inventory.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                        {
                            EntityManager.DeleteEntity(unequippedItem.Value);
                        }

                        if (itemType != string.Empty)
                        {
                            var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                            _inventory.TryEquip(dummy, item, slot.Name, true, true);
                        }
                    }
                }
            }
        }

        if (job.StartingGear == null)
            return;

        var gear = _prototypeManager.Index<StartingGearPrototype>(job.StartingGear);

        foreach (var slot in slots)
        {
            var itemType = gear.GetGear(slot.Name);

            if (_inventory.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
            {
                EntityManager.DeleteEntity(unequippedItem.Value);
            }

            if (itemType != string.Empty)
            {
                var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                _inventory.TryEquip(dummy, item, slot.Name, true, true);
            }
        }
    }

    public EntityUid? GetPreviewDummy()
    {
        return _previewDummy;
    }
}
