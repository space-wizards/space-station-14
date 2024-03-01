using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Client.Lobby.UI;
using Content.Client.Preferences;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby;

public sealed class LobbyUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [UISystemDependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [UISystemDependency] private readonly ClientInventorySystem _inventory = default!;

    private LobbyCharacterPreviewPanel? _previewPanel;

    /// <summary>
    /// Preview dummy for role gear.
    /// </summary>
    private EntityUid? _previewDummy;

    /// <summary>
    /// If we currently have a loadout selected.
    /// </summary>
    private JobPrototype? _dummyJob;

    // TODO: Load the species directly and don't update entity ever.
    public event Action<EntityUid>? PreviewDummyUpdated;

    public override void Initialize()
    {
        base.Initialize();
        _preferencesManager.OnServerDataLoaded += PreferencesDataLoaded;
    }

    private void PreferencesDataLoaded()
    {
        UpdateCharacterUI();
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
        UpdateCharacterUI();
    }

    public void SetDummyJob(JobPrototype? job)
    {
        if (_dummyJob == job)
            return;

        _dummyJob = job;
        UpdateCharacterUI();
    }

    public void UpdateCharacterUI()
    {
        if (!_preferencesManager.ServerDataLoaded)
        {
            _previewPanel?.SetLoaded(false);
            return;
        }

        _previewPanel?.SetLoaded(true);

        if (_preferencesManager.Preferences?.SelectedCharacter is not HumanoidCharacterProfile selectedCharacter)
        {
            _previewPanel?.SetSummaryText(string.Empty);
        }
        else
        {
            EntityManager.DeleteEntity(_previewDummy);
            _previewDummy = EntityManager.SpawnEntity(_prototypeManager.Index<SpeciesPrototype>(selectedCharacter.Species).DollPrototype, MapCoordinates.Nullspace);
            _previewPanel?.SetSprite(_previewDummy.Value);
            _previewPanel?.SetSummaryText(selectedCharacter.Summary);
            _humanoid.LoadProfile(_previewDummy.Value, selectedCharacter);

            GiveDummyJobClothes(_previewDummy.Value, selectedCharacter);
            PreviewDummyUpdated?.Invoke(_previewDummy.Value);
        }
    }

    /// <summary>
    /// Applies the highest priority job's clothes to the dummy.
    /// </summary>
    public void GiveDummyJobClothes(EntityUid dummy, HumanoidCharacterProfile profile)
    {
        JobPrototype job;

        if (_dummyJob != null)
        {
            job = _dummyJob;
        }
        else
        {
            var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;
            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract (what is resharper smoking?)
            job = _prototypeManager.Index<JobPrototype>(highPriorityJob ?? SharedGameTicker.FallbackOverflowJob);
        }

        GiveDummyJobClothes(dummy, profile, job);
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
            foreach (var loadoutId in jobLoadout.SelectedLoadouts.Values)
            {
                if (loadoutId == null || !_prototypeManager.TryIndex(loadoutId.Value, out var loadoutProto))
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
                        if (!_inventory.TryEquip(dummy, item, slot.Name, true, true))
                        {
                            EntityManager.DeleteEntity(item);
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
                if (!_inventory.TryEquip(dummy, item, slot.Name, true, true))
                {
                    EntityManager.DeleteEntity(item);
                }
            }
        }
    }

    public EntityUid? GetPreviewDummy()
    {
        return _previewDummy;
    }
}
