using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Client.Lobby.UI;
using Content.Client.Station;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby;

public sealed partial class LobbyUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [UISystemDependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [UISystemDependency] private readonly ClientInventorySystem _inventory = default!;
    [UISystemDependency] private readonly StationSpawningSystem _spawn = default!;

    private CharacterSetupGui? _characterSetup;
    private HumanoidProfileEditor? _profileEditor;

    /// <summary>
    /// This is the characher preview panel in the chat. This should only update if their character updates.
    /// </summary>
    private LobbyCharacterPreviewPanel? _previewPanel;

    /// <summary>
    /// This is the modified profile currently being edited.
    /// </summary>
    private HumanoidCharacterProfile? _profile;

    /// <summary>
    /// Should we show clothes on the preview dummy.
    /// </summary>
    private bool _showClothes = true;

    public override void Initialize()
    {
        base.Initialize();
        _preferencesManager.OnServerDataLoaded += PreferencesDataLoaded;
    }

    private void PreferencesDataLoaded()
    {
        ReloadCharacterSetup();
    }

    public void OnStateEntered(LobbyState state)
    {
        ReloadCharacterSetup();
    }

    public void OnStateExited(LobbyState state)
    {
        // TODO: Attach preview dummy to the profile editor
        EntityManager.DeleteEntity(_previewDummy);
        _previewDummy = null;

        _profileEditor?.Dispose();
        _characterSetup?.Dispose();
        _previewPanel?.Dispose();

        _characterSetup = null;
        _profileEditor = null;
        _previewPanel = null;
    }

    /// <summary>
    /// Reloads every single character setup control.
    /// </summary>
    private void ReloadCharacterSetup()
    {
        RefreshLobbyPreview();
        var (characterGui, profileEditor) = EnsureGui();
        characterGui.ReloadCharacterPickers();
        // TODO: Profile editor thing
    }

    /// <summary>
    /// Refreshes the character preview in the lobby chat.
    /// </summary>
    private void RefreshLobbyPreview()
    {
        _previewPanel?.Dispose();
        _previewPanel = new LobbyCharacterPreviewPanel();
        // Get selected character, load it, then set it
        var character = _preferencesManager.Preferences?.SelectedCharacter;

        if (character == null)
            return;

        var dummy = LoadProfileEntity((HumanoidCharacterProfile) character);
        _previewPanel.SetSprite(dummy);
    }

    private void SaveProfile()
    {
        DebugTools.Assert(_profile != null);

        if (_profile == null)
            return;

        var selected = _preferencesManager.Preferences?.SelectedCharacterIndex;

        if (selected == null)
            return;

        _preferencesManager.UpdateCharacter(_profile, selected.Value);
        ReloadCharacterSetup();
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
            var loadout = profile.GetLoadoutOrDefault(LoadoutSystem.GetJobPrototype(job.ID), profile.Species, EntityManager, _prototypeManager);
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

    private (CharacterSetupGui, HumanoidProfileEditor) EnsureGui()
    {
        if (_characterSetup != null && _profileEditor != null)
            return (_characterSetup, _profileEditor);

        _profileEditor = new HumanoidProfileEditor(_preferencesManager, _prototypeManager, _configurationManager);
        _characterSetup = new CharacterSetupGui(EntityManager, _prototypeManager, _resourceCache, _preferencesManager, _profileEditor);

        _characterSetup.CloseButton.OnPressed += _ =>
        {
            // Reset sliders etc.
            _characterSetup?.UpdateControls();

            SetClothes(true);
            UpdateProfile();
            _lobby.SwitchState(LobbyGui.LobbyGuiState.Default);
        };

        _characterSetup.SaveButton.OnPressed += _ =>
        {
            SaveProfile();
        };

        _characterSetup.SelectCharacter += args =>
        {
            _preferencesManager.SelectCharacter(args);
            ReloadCharacterSetup();
        };

        _characterSetup.DeleteCharacter += args =>
        {
            _preferencesManager.DeleteCharacter(args);
            _characterSetup.ReloadCharacterPickers();
        };

        _lobby.CharacterSetupState.AddChild(_characterSetup);

        return (_characterSetup, _profileEditor);
    }

    /// <summary>
    /// Loads the profile onto a dummy entity.
    /// </summary>
    public EntityUid LoadProfileEntity(HumanoidCharacterProfile? humanoid)
    {
        EntityUid dummyEnt;

        if (humanoid is not null)
        {
            var dummy = _prototypeManager.Index<SpeciesPrototype>(humanoid.Species).DollPrototype;
            dummyEnt = EntityManager.SpawnEntity(dummy, MapCoordinates.Nullspace);
        }
        else
        {
            dummyEnt = EntityManager.SpawnEntity(_prototypeManager.Index<SpeciesPrototype>(SharedHumanoidAppearanceSystem.DefaultSpecies).DollPrototype, MapCoordinates.Nullspace);
        }

        _humanoid.LoadProfile(dummyEnt, humanoid);

        if (humanoid != null)
        {
            var job = GetPreferredJob(humanoid);
            GiveDummyJobClothes(dummyEnt, humanoid, job);

            if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID)))
            {
                var loadout = humanoid.GetLoadoutOrDefault(LoadoutSystem.GetJobPrototype(job.ID), humanoid.Species, EntityManager, _prototypeManager);
                GiveDummyLoadout(dummyEnt, loadout);
            }
        }

        return dummyEnt;
    }
}
