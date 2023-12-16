using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Magic.Components;
using Content.Shared.Magic.Events;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Content.Shared.Storage;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Spawners;

namespace Content.Shared.Magic;

// TODO: 2 split spellbooks into their own ECS
// TODO: 3 suffer when refactoring

/// <summary>
/// Handles learning and using spells (actions)
/// </summary>
public abstract class SharedMagicSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _seriMan = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        // TODO: Make Magic Comp/Magic Caster Comp
        SubscribeLocalEvent<MagicComponent, MapInitEvent>(OnInit, after: new []{typeof(SharedSpellbookSystem)});
        // TODO: Upgradeable spells

        // TODO: More spells
        SubscribeLocalEvent<InstantSpawnSpellEvent>(OnInstantSpawn);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<WorldSpawnSpellEvent>(OnWorldSpawn);
        SubscribeLocalEvent<ProjectileSpellEvent>(OnProjectileSpell);
        SubscribeLocalEvent<ChangeComponentsSpellEvent>(OnChangeComponentsSpell);
        SubscribeLocalEvent<SmiteSpellEvent>(OnSmiteSpell);
        SubscribeLocalEvent<KnockSpellEvent>(OnKnockSpell);
    }

    private void OnInit(EntityUid uid, MagicComponent component, MapInitEvent args)
    {
        // TODO: Reconsider just setting on protos
        // _actions.SetUseDelay(uid, component.Cooldown);
        // _actions.SetUsesBeforeDelay(uid, component.Uses);
    }

    #region Spells

    /// <summary>
    /// Handles the instant action (i.e. on the caster) attempting to spawn an entity.
    /// </summary>
    private void OnInstantSpawn(InstantSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        foreach (var position in GetSpawnPositions(transform, args.Pos))
        {
            if (_net.IsServer)
            {
                var ent = Spawn(args.Prototype, position.SnapToGrid(EntityManager, _mapManager));

                if (args.PreventCollideWithCaster)
                {
                    var comp = EnsureComp<PreventCollideComponent>(ent);
                    comp.Uid = args.Performer;
                }
            }
        }

        Speak(args);
        args.Handled = true;
    }

    // TODO: Deduplicate copied gun code
    private void OnProjectileSpell(ProjectileSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);

        var xform = Transform(ev.Performer);
        var fromCoords = xform.Coordinates;
        var toCoords = ev.Target;
        var userVelocity = _physics.GetMapLinearVelocity(ev.Performer);

        // If applicable, this ensures the projectile is parented to grid on spawn, instead of the map.
        var toMap = toCoords.ToMap(EntityManager, _transform);
        var fromMap = fromCoords.ToMap(EntityManager, _transform);
        var spawnCoords = _mapManager.TryFindGridAt(fromMap, out var gridUid, out _)
            ? fromCoords.WithEntityId(gridUid, EntityManager)
            : new(_mapManager.GetMapEntityId(fromMap.MapId), fromMap.Position);

        if (_net.IsServer)
        {
            var ent = Spawn(ev.Prototype, spawnCoords);
            var direction = toCoords.ToMapPos(EntityManager, _transform) -
                            spawnCoords.ToMapPos(EntityManager, _transform);
            _gunSystem.ShootProjectile(ent, direction, userVelocity, ev.Performer, ev.Performer);
        }
    }

    // TODO: See what this is used for and try to swap out component.Owner
    private void OnChangeComponentsSpell(ChangeComponentsSpellEvent ev)
    {
        if (ev.Handled)
            return;
        ev.Handled = true;
        Speak(ev);

        foreach (var toRemove in ev.ToRemove)
        {
            if (_compFact.TryGetRegistration(toRemove, out var registration))
                RemComp(ev.Target, registration.Type);
        }

        foreach (var (name, data) in ev.ToAdd)
        {
            if (HasComp(ev.Target, data.Component.GetType()))
                continue;

            var component = (Component) _compFact.GetComponent(name);
            component.Owner = ev.Target;
            var temp = (object) component;
            _seriMan.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(ev.Target, (Component) temp!);
        }
    }

    // TODO: Rework this somehow & COMMENT
    private List<EntityCoordinates> GetSpawnPositions(TransformComponent casterXform, MagicSpawnData data)
    {
        switch (data)
        {
            // TODO: Rename to TargetUnderCaster
            case TargetCasterPos:
                return new List<EntityCoordinates>(1) {casterXform.Coordinates};
            // TODO: Rename to TargetInFront
            case TargetInFrontSingle:
            {
                var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized());

                if (!_mapManager.TryGetGrid(casterXform.GridUid, out var mapGrid))
                    return new List<EntityCoordinates>();
                if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
                    return new List<EntityCoordinates>();

                var tileIndex = tileReference.Value.GridIndices;
                return new List<EntityCoordinates>(1) { mapGrid.GridTileToLocal(tileIndex) };
            }
            // TODO: Rename to TargetLineInFront
            case TargetInFront:
            {
                // This is shit but you get the idea.
                var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized());

                if (!_mapManager.TryGetGrid(casterXform.GridUid, out var mapGrid))
                    return new List<EntityCoordinates>();

                if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
                    return new List<EntityCoordinates>();

                var tileIndex = tileReference.Value.GridIndices;
                var coords = mapGrid.GridTileToLocal(tileIndex);
                EntityCoordinates coordsPlus;
                EntityCoordinates coordsMinus;

                var dir = casterXform.LocalRotation.GetCardinalDir();
                switch (dir)
                {
                    case Direction.North:
                    case Direction.South:
                    {
                        coordsPlus = mapGrid.GridTileToLocal(tileIndex + (1, 0));
                        coordsMinus = mapGrid.GridTileToLocal(tileIndex + (-1, 0));
                        return new List<EntityCoordinates>(3)
                        {
                            coords,
                            coordsPlus,
                            coordsMinus,
                        };
                    }
                    case Direction.East:
                    case Direction.West:
                    {
                        coordsPlus = mapGrid.GridTileToLocal(tileIndex + (0, 1));
                        coordsMinus = mapGrid.GridTileToLocal(tileIndex + (0, -1));
                        return new List<EntityCoordinates>(3)
                        {
                            coords,
                            coordsPlus,
                            coordsMinus,
                        };
                    }
                }

                return new List<EntityCoordinates>();
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // TODO: should probably add better validation that the clicked location is on the users screen somewhere,
    /// <summary>
    /// Teleports the user to the clicked location
    /// </summary>
    /// <param name="args"></param>
    private void OnTeleportSpell(TeleportSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        if (transform.MapID != args.Target.GetMapId(EntityManager))
            return;

        // TODO: xform query

        _transform.SetCoordinates(args.Performer, args.Target);
        _transform.AttachToGridOrMap(args.Performer, transform);
        Speak(args);
        args.Handled = true;
    }

    /// <summary>
    /// Spawns entity prototypes from a list within range of click.
    /// </summary>
    /// <remarks>
    /// It will offset mobs after the first mob based on the OffsetVector2 property supplied.
    /// </remarks>
    /// <param name="args"> The Spawn Spell Event args.</param>
    private void OnWorldSpawn(WorldSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var targetMapCoords = args.Target;

        SpawnSpellHelper(args.Contents, targetMapCoords, args.Lifetime, args.Offset);
        Speak(args);
        args.Handled = true;
    }

    // TODO: Try to get off of Prototypes?
    /// <summary>
    /// Loops through a supplied list of entity prototypes and spawns them
    /// </summary>
    /// <remarks>
    /// If an offset of 0, 0 is supplied then the entities will all spawn on the same tile.
    /// Any other offset will spawn entities starting from the source Map Coordinates and will increment the supplied
    /// offset
    /// </remarks>
    /// <param name="entityEntries"> The list of Entities to spawn in</param>
    /// <param name="entityCoords"> Map Coordinates where the entities will spawn</param>
    /// <param name="lifetime"> Check to see if the entities should self delete</param>
    /// <param name="offsetVector2"> A Vector2 offset that the entities will spawn in</param>
    private void SpawnSpellHelper(List<EntitySpawnEntry> entityEntries, EntityCoordinates entityCoords, float? lifetime, Vector2 offsetVector2)
    {
        var getProtos = EntitySpawnCollection.GetSpawns(entityEntries, _random);

        var offsetCoords = entityCoords;
        foreach (var proto in getProtos)
        {
            // TODO: Share this code with instant because they're both doing similar things for positioning.
            if (_net.IsServer)
            {
                var entity = Spawn(proto, offsetCoords);
                offsetCoords = offsetCoords.Offset(offsetVector2);

                if (lifetime != null)
                {
                    var comp = EnsureComp<TimedDespawnComponent>(entity);
                    comp.Lifetime = lifetime.Value;
                }
            }
        }
    }

    private void OnSmiteSpell(SmiteSpellEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        Speak(ev);

        var direction = Transform(ev.Target).MapPosition.Position - Transform(ev.Performer).MapPosition.Position;
        var impulseVector = direction * 10000;

        _physics.ApplyLinearImpulse(ev.Target, impulseVector);

        if (!TryComp<BodyComponent>(ev.Target, out var body))
            return;

        _bodySystem.GibBody(ev.Target, true, body);
    }

    /// <summary>
    /// Opens all doors within range
    /// </summary>
    /// <param name="args"></param>
    private void OnKnockSpell(KnockSpellEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Speak(args);

        //Get the position of the player
        var transform = Transform(args.Performer);
        var coords = transform.Coordinates;

        // TODO: Can probably use in range for teleport spells as well
        // Look for doors and don't open them if they're already open.
        // Magic doesn't respect locks, it just opens
        foreach (var entity in _lookup.GetEntitiesInRange(coords, args.Range))
        {
            // TODO: See about unlocking lockers too

            if (TryComp<DoorComponent>(entity, out var doorComp) && doorComp.State is not DoorState.Open)
                _doorSystem.StartOpening(entity);
        }
    }

    #endregion

    // TODO: Refactor into an event
    // When any spell is cast it will raise this as an event, so then it can be played in server or something. At least until chat gets moved to shared
    protected void Speak(BaseActionEvent args)
    {
        /*if (args is not ISpeakSpell speak || string.IsNullOrWhiteSpace(speak.Speech))
            return;

        _chat.TrySendInGameICMessage(args.Performer, Loc.GetString(speak.Speech),
            InGameICChatType.Speak, false);*/
    }
}
