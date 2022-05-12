using Content.Server.Access.Systems;
using Content.Server.CharacterAppearance.Systems;
using Content.Server.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Server.PDA;
using Content.Server.Roles;
using Content.Server.Station.Components;
using Content.Shared.Access.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Species;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Station.Systems;

/// <summary>
/// Manages spawning into the game, tracking available spawn points.
/// Also provides helpers for spawning in the player's mob.
/// </summary>
[PublicAPI]
public sealed class StationSpawningSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    [Dependency] private readonly IdCardSystem _cardSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly PDASystem _pdaSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized);
    }

    private void OnStationInitialized(StationInitializedEvent ev)
    {
        AddComp<StationSpawningComponent>(ev.Station);
    }

    /// <summary>
    /// Attempts to spawn a player character onto the given station.
    /// </summary>
    /// <param name="station">Station to spawn onto.</param>
    /// <param name="job">The job to assign, if any.</param>
    /// <param name="profile">The character profile to use, if any.</param>
    /// <param name="stationSpawning">Resolve pattern, the station spawning component for the station.</param>
    /// <returns>The resulting player character, if any.</returns>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    /// <remarks>
    /// This only spawns the character, and does none of the mind-related setup you'd need for it to be playable.
    /// </remarks>
    public EntityUid? SpawnPlayerCharacterOnStation(EntityUid? station, Job? job, HumanoidCharacterProfile? profile, StationSpawningComponent? stationSpawning = null)
    {
        if (station != null && !Resolve(station.Value, ref stationSpawning))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        var ev = new PlayerSpawningEvent(job, profile, station);
        RaiseLocalEvent(ev);

        DebugTools.Assert(ev.SpawnResult is {Valid: true} or null);

        return ev.SpawnResult;
    }

    //TODO: Figure out if everything in the player spawning region belongs somewhere else.
    #region Player spawning helpers

    /// <summary>
    /// Spawns in a player's mob according to their job and character information at the given coordinates.
    /// Used by systems that need to handle spawning players.
    /// </summary>
    /// <param name="coordinates">Coordinates to spawn the character at.</param>
    /// <param name="job">Job to assign to the character, if any.</param>
    /// <param name="profile">Appearance profile to use for the character.</param>
    /// <returns>The spawned entity</returns>
    public EntityUid SpawnPlayerMob(EntityCoordinates coordinates, Job? job, HumanoidCharacterProfile? profile)
    {
        var entity = EntityManager.SpawnEntity(
            _prototypeManager.Index<SpeciesPrototype>(profile?.Species ?? SpeciesManager.DefaultSpecies).Prototype,
            coordinates);

        if (job?.StartingGear != null)
        {
            var startingGear = _prototypeManager.Index<StartingGearPrototype>(job.StartingGear);
            EquipStartingGear(entity, startingGear, profile);
            if (profile != null)
                EquipIdCard(entity, profile.Name, job.Prototype);
        }

        if (profile != null)
        {
            _humanoidAppearanceSystem.UpdateFromProfile(entity, profile);
            EntityManager.GetComponent<MetaDataComponent>(entity).EntityName = profile.Name;
        }

        foreach (var jobSpecial in job?.Prototype.Special ?? Array.Empty<JobSpecial>())
        {
            jobSpecial.AfterEquip(entity);
        }

        return entity;
    }

    /// <summary>
    /// Equips starting gear onto the given entity.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="startingGear">Starting gear to use.</param>
    /// <param name="profile">Character profile to use, if any.</param>
    public void EquipStartingGear(EntityUid entity, StartingGearPrototype startingGear, HumanoidCharacterProfile? profile)
    {
        if (_inventorySystem.TryGetSlots(entity, out var slotDefinitions))
        {
            foreach (var slot in slotDefinitions)
            {
                var equipmentStr = startingGear.GetGear(slot.Name, profile);
                if (!string.IsNullOrEmpty(equipmentStr))
                {
                    var equipmentEntity = EntityManager.SpawnEntity(equipmentStr, EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
                    _inventorySystem.TryEquip(entity, equipmentEntity, slot.Name, true);
                }
            }
        }

        if (!TryComp(entity, out HandsComponent? handsComponent))
            return;

        var inhand = startingGear.Inhand;
        var coords = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
        foreach (var (hand, prototype) in inhand)
        {
            var inhandEntity = EntityManager.SpawnEntity(prototype, coords);
            _handsSystem.TryPickup(entity, inhandEntity, hand, checkActionBlocker: false, handsComp: handsComponent);
        }
    }

    /// <summary>
    /// Equips an ID card and PDA onto the given entity.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="characterName">Character name to use for the ID.</param>
    /// <param name="jobPrototype">Job prototype to use for the PDA and ID.</param>
    public void EquipIdCard(EntityUid entity, string characterName, JobPrototype jobPrototype)
    {
        if (!_inventorySystem.TryGetSlotEntity(entity, "id", out var idUid))
            return;

        if (!EntityManager.TryGetComponent(idUid, out PDAComponent? pdaComponent) || pdaComponent.ContainedID == null)
            return;

        var card = pdaComponent.ContainedID;
        _cardSystem.TryChangeFullName(card.Owner, characterName, card);
        _cardSystem.TryChangeJobTitle(card.Owner, jobPrototype.Name, card);

        var access = EntityManager.GetComponent<AccessComponent>(card.Owner);
        var accessTags = access.Tags;
        accessTags.UnionWith(jobPrototype.Access);
        _pdaSystem.SetOwner(pdaComponent, characterName);
    }


    #endregion Player spawning helpers
}

/// <summary>
/// Ordered broadcast event fired on any spawner eligible to attempt to spawn a player.
/// This event's success is measured by if SpawnResult is not null.
/// You should not make this event's success rely on random chance.
/// This event is designed to use ordered handling. You probably want SpawnPointSystem to be the last handler.
/// </summary>
[PublicAPI]
public sealed class PlayerSpawningEvent : EntityEventArgs
{
    /// <summary>
    /// The entity spawned, if any. You should set this if you succeed at spawning the character, and leave it alone if it's not null.
    /// </summary>
    public EntityUid? SpawnResult;
    /// <summary>
    /// The job to use, if any.
    /// </summary>
    public readonly Job? Job;
    /// <summary>
    /// The profile to use, if any.
    /// </summary>
    public readonly HumanoidCharacterProfile? HumanoidCharacterProfile;
    /// <summary>
    /// The target station, if any.
    /// </summary>
    public readonly EntityUid? Station;

    public PlayerSpawningEvent(Job? job, HumanoidCharacterProfile? humanoidCharacterProfile, EntityUid? station)
    {
        Job = job;
        HumanoidCharacterProfile = humanoidCharacterProfile;
        Station = station;
    }
}
