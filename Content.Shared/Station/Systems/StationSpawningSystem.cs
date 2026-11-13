using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Body;
using Content.Shared.CCVar;
using Content.Shared.Clothing;
using Content.Shared.DetailExaminable;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Station.Systems;

public sealed partial class StationSpawningSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configurationManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ActorSystem _actors = default!;
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _metadata = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedTransformSystem _xformSystem = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private SharedAccessSystem _accessSystem = default!;
    [Dependency] private SharedIdCardSystem _cardSystem = default!;
    [Dependency] private SharedPdaSystem _pdaSystem = default!;

    [Dependency] private EntityQuery<HandsComponent> _handsQuery;
    [Dependency] private EntityQuery<InventoryComponent> _inventoryQuery;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery;

    /// <summary>
    ///     Equips the data from a `RoleLoadout` onto an entity.
    /// </summary>
    [PublicAPI]
    public void EquipRoleLoadout(EntityUid entity, RoleLoadout loadout, RoleLoadoutPrototype roleProto)
    {
        // Order loadout selections by the order they appear on the prototype.
        foreach (var group in loadout.SelectedLoadouts.OrderBy(x => roleProto.Groups.FindIndex(e => e == x.Key)))
        {
            foreach (var items in group.Value)
            {
                if (!ProtoMan.TryIndex(items.Prototype, out var loadoutProto))
                {
                    Log.Error($"Unable to find loadout prototype for {items.Prototype}");
                    continue;
                }

                EquipStartingGear(entity, loadoutProto, raiseEvent: false);
            }
        }

        EquipRoleName(entity, loadout, roleProto);
    }

    /// <summary>
    /// Applies the role's name as applicable to the entity.
    /// </summary>
    [PublicAPI]
    public void EquipRoleName(EntityUid entity, RoleLoadout loadout, RoleLoadoutPrototype roleProto)
    {
        string? name = null;

        if (roleProto.CanCustomizeName)
        {
            name = loadout.EntityName;
        }

        if (string.IsNullOrEmpty(name) && ProtoMan.Resolve(roleProto.NameDataset, out var nameData))
        {
            name = Loc.GetString(_random.Pick(nameData.Values));
        }

        if (!string.IsNullOrEmpty(name))
        {
            _metadata.SetEntityName(entity, name);
        }
    }

    public void EquipStartingGear(EntityUid entity, LoadoutPrototype loadout, bool raiseEvent = true)
    {
        EquipStartingGear(entity, loadout.StartingGear, raiseEvent);
        EquipStartingGear(entity, (IEquipmentLoadout) loadout, raiseEvent);
    }

    /// <summary>
    /// <see cref="EquipStartingGear(Robust.Shared.GameObjects.EntityUid,System.Nullable{Robust.Shared.Prototypes.ProtoId{Content.Shared.Roles.StartingGearPrototype}},bool)"/>
    /// </summary>
    [PublicAPI]
    public void EquipStartingGear(EntityUid entity, ProtoId<StartingGearPrototype>? startingGear, bool raiseEvent = true)
    {
        ProtoMan.Resolve(startingGear, out var gearProto);
        EquipStartingGear(entity, gearProto, raiseEvent);
    }

    /// <summary>
    /// <see cref="EquipStartingGear(Robust.Shared.GameObjects.EntityUid,System.Nullable{Robust.Shared.Prototypes.ProtoId{Content.Shared.Roles.StartingGearPrototype}},bool)"/>
    /// </summary>
    [PublicAPI]
    public void EquipStartingGear(EntityUid entity, StartingGearPrototype? startingGear, bool raiseEvent = true)
    {
        EquipStartingGear(entity, (IEquipmentLoadout?) startingGear, raiseEvent);
    }

    /// <summary>
    /// Equips starting gear onto the given entity.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="startingGear">Starting gear to use.</param>
    /// <param name="raiseEvent">Should we raise the event for equipped. Set to false if you will call this manually</param>
    [PublicAPI]
    public void EquipStartingGear(EntityUid entity, IEquipmentLoadout? startingGear, bool raiseEvent = true)
    {
        if (startingGear == null)
            return;

        var xform = _xformQuery.GetComponent(entity);

        if (_inventory.TryGetSlots(entity, out var slotDefinitions))
        {
            foreach (var slot in slotDefinitions)
            {
                var equipmentStr = startingGear.GetGear(slot.Name);
                if (!string.IsNullOrEmpty(equipmentStr))
                {
                    var equipmentEntity = Spawn(equipmentStr, xform.Coordinates);
                    _inventory.TryEquip(entity, equipmentEntity, slot.Name, silent: true, force: true);
                }
            }
        }

        if (_handsQuery.TryComp(entity, out var handsComponent))
        {
            var inhand = startingGear.Inhand;
            var coords = xform.Coordinates;
            foreach (var prototype in inhand)
            {
                var inhandEntity = Spawn(prototype, coords);

                if (_handsSystem.TryGetEmptyHand((entity, handsComponent), out var emptyHand))
                {
                    _handsSystem.TryPickup(entity, inhandEntity, emptyHand, checkActionBlocker: false, handsComp: handsComponent);
                }
            }
        }

        if (startingGear.Storage.Count > 0)
        {
            var coords = _xformSystem.GetMapCoordinates(entity);
            _inventoryQuery.TryComp(entity, out var inventoryComp);

            foreach (var (slotName, entProtos) in startingGear.Storage)
            {
                if (entProtos.Count == 0)
                    continue;

                if (inventoryComp != null &&
                    _inventory.TryGetSlotEntity(entity, slotName, out var slotEnt, inventoryComponent: inventoryComp) &&
                    _storageQuery.TryComp(slotEnt, out var storage))
                {

                    foreach (var entProto in entProtos)
                    {
                        var spawnedEntity = Spawn(entProto, coords);

                        _storage.Insert(slotEnt.Value, spawnedEntity, out _, storageComp: storage, playSound: false);
                    }
                }
            }
        }

        if (raiseEvent)
        {
            var ev = new StartingGearEquippedEvent(entity);
            RaiseLocalEvent(entity, ref ev);
        }
    }

    /// <summary>
    ///     Gets all the gear for a given slot when passed a loadout.
    /// </summary>
    /// <param name="loadout">The loadout to look through.</param>
    /// <param name="slot">The slot that you want the clothing for.</param>
    /// <returns>
    ///     If there is a value for the given slot, it will return the proto id for that slot.
    ///     If nothing was found, will return null
    /// </returns>
    [PublicAPI]
    public string? GetGearForSlot(RoleLoadout? loadout, string slot)
    {
        if (loadout == null)
            return null;

        foreach (var group in loadout.SelectedLoadouts)
        {
            foreach (var items in group.Value)
            {
                if (!ProtoMan.Resolve(items.Prototype, out var loadoutPrototype))
                    return null;

                var gear = ((IEquipmentLoadout) loadoutPrototype).GetGear(slot);
                if (gear != string.Empty)
                    return gear;
            }
        }

        return null;
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
    public EntityUid? SpawnPlayerCharacterOnStation(EntityUid? station, ProtoId<JobPrototype>? job, HumanoidCharacterProfile? profile, StationSpawningComponent? stationSpawning = null)
    {
        if (station != null && !Resolve(station.Value, ref stationSpawning))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        var ev = new PlayerSpawningEvent(job, profile, station);

        RaiseLocalEvent(ev);
        DebugTools.Assert(ev.SpawnResult is { Valid: true } or null);

        return ev.SpawnResult;
    }

    /// <summary>
    /// Sets the ID card and PDA name, job, and access data.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="characterName">Character name to use for the ID.</param>
    /// <param name="jobPrototype">Job prototype to use for the PDA and ID.</param>
    /// <param name="station">The station this player is being spawned on.</param>
    [PublicAPI]
    public void SetPdaAndIdCardData(EntityUid entity,
        string characterName,
        JobPrototype jobPrototype,
        EntityUid? station)
    {
        if (!_inventory.TryGetSlotEntity(entity, "id", out var idUid))
            return;

        var cardId = idUid.Value;
        if (TryComp<PdaComponent>(idUid, out var pdaComponent) && pdaComponent.ContainedId != null)
            cardId = pdaComponent.ContainedId.Value;

        if (!TryComp<IdCardComponent>(cardId, out var card))
            return;

        _cardSystem.TryChangeFullName(cardId, characterName, card);
        _cardSystem.TryChangeJobTitle(cardId, jobPrototype.LocalizedName, card);

        if (ProtoMan.Resolve(jobPrototype.Icon, out var jobIcon))
            _cardSystem.TryChangeJobIcon(cardId, jobIcon, card);

        var extendedAccess = false;
        if (station != null)
        {
            var data = Comp<StationJobsComponent>(station.Value);
            extendedAccess = data.ExtendedAccess;
        }

        _accessSystem.SetAccessToJob(cardId, jobPrototype, extendedAccess);

        if (pdaComponent != null)
            _pdaSystem.SetOwner(idUid.Value, pdaComponent, entity, characterName);
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
    /// <param name="station">The station this player is being spawned on.</param>
    /// <param name="entity">The entity to use, if one already exists.</param>
    /// <returns>The spawned entity</returns>
    public EntityUid SpawnPlayerMob(
        EntityCoordinates coordinates,
        ProtoId<JobPrototype>? job,
        HumanoidCharacterProfile? profile,
        EntityUid? station,
        EntityUid? entity = null)
    {
        ProtoMan.Resolve(job, out var prototype);
        RoleLoadout? loadout = null;

        // Need to get the loadout up-front to handle names if we use an entity spawn override.
        var jobLoadout = LoadoutSystem.GetJobPrototype(prototype?.ID);

        if (ProtoMan.TryIndex(jobLoadout, out RoleLoadoutPrototype? roleProto))
        {
            profile?.Loadouts.TryGetValue(jobLoadout, out loadout);

            // Set to default if not present
            if (loadout == null)
            {
                loadout = new RoleLoadout(jobLoadout);
                loadout.SetDefault(profile, _actors.GetSession(entity), ProtoMan);
            }
        }

        // If we're not spawning a humanoid, we're gonna exit early without doing all the humanoid stuff.
        if (prototype?.JobEntity != null)
        {
            DebugTools.Assert(entity is null);
            var jobEntity = Spawn(prototype.JobEntity, coordinates);
            _mindSystem.MakeSentient(jobEntity);

            // Make sure custom names get handled, what is gameticker control flow whoopy.
            if (loadout != null)
            {
                EquipRoleName(jobEntity, loadout, roleProto!);
            }

            DoJobSpecials(job, jobEntity);
            _identity.QueueIdentityUpdate(jobEntity);
            return jobEntity;
        }

        string speciesId = profile != null ? profile.Species : HumanoidCharacterProfile.DefaultSpecies;

        if (!ProtoMan.TryIndex<SpeciesPrototype>(speciesId, out var species))
            throw new ArgumentException($"Invalid species prototype was used: {speciesId}");

        entity ??= Spawn(species.Prototype, coordinates);

        if (profile != null)
        {
            _visualBody.ApplyProfileTo(entity.Value, profile);
            _humanoidProfile.ApplyProfileTo(entity.Value, profile);
            _metadata.SetEntityName(entity.Value, profile.Name);

            if (profile.FlavorText != "" && _configurationManager.GetCVar(CCVars.FlavorText))
            {
                AddComp<DetailExaminableComponent>(entity.Value).Content = profile.FlavorText;
            }
        }

        if (loadout != null)
        {
            EquipRoleLoadout(entity.Value, loadout, roleProto!);
        }

        if (prototype?.StartingGear != null)
        {
            var startingGear = ProtoMan.Index<StartingGearPrototype>(prototype.StartingGear);
            EquipStartingGear(entity.Value, startingGear, raiseEvent: false);
        }

        var gearEquippedEv = new StartingGearEquippedEvent(entity.Value);
        RaiseLocalEvent(entity.Value, ref gearEquippedEv);

        if (prototype != null && TryComp(entity.Value, out MetaDataComponent? metaData))
        {
            SetPdaAndIdCardData(entity.Value, metaData.EntityName, prototype, station);
        }

        DoJobSpecials(job, entity.Value);
        _identity.QueueIdentityUpdate(entity.Value);
        return entity.Value;
    }

    private void DoJobSpecials(ProtoId<JobPrototype>? job, EntityUid entity)
    {
        if (!ProtoMan.Resolve(job, out JobPrototype? prototype))
            return;

        foreach (var jobSpecial in prototype.Special)
        {
            jobSpecial.AfterEquip(entity);
        }
    }
    #endregion Player spawning helpers
}

/// <summary>
/// Ordered broadcast event fired on any spawner eligible to attempt to spawn a player.
/// This event's success is measured by if SpawnResult is not null.
/// You should not make this event's success rely on random chance.
/// This event is designed to use ordered handling. You probably want SpawnPointSystem to be the last handler.
/// </summary>
public sealed class PlayerSpawningEvent : EntityEventArgs
{
    /// <summary>
    /// The entity spawned, if any. You should set this if you succeed at spawning the character, and leave it alone if it's not null.
    /// </summary>
    public EntityUid? SpawnResult;
    /// <summary>
    /// The job to use, if any.
    /// </summary>
    public readonly ProtoId<JobPrototype>? Job;
    /// <summary>
    /// The profile to use, if any.
    /// </summary>
    public readonly HumanoidCharacterProfile? HumanoidCharacterProfile;
    /// <summary>
    /// The target station, if any.
    /// </summary>
    public readonly EntityUid? Station;

    public PlayerSpawningEvent(ProtoId<JobPrototype>? job, HumanoidCharacterProfile? humanoidCharacterProfile, EntityUid? station)
    {
        Job = job;
        HumanoidCharacterProfile = humanoidCharacterProfile;
        Station = station;
    }
}
