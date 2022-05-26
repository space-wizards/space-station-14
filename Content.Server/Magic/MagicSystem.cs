using Content.Server.Coordinates.Helpers;
using Content.Server.Decals;
using Content.Server.DoAfter;
using Content.Server.Doors.Components;
using Content.Server.Magic.Events;
using Content.Server.Spawners.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Magic;

public sealed class MagicSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpellbookComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpellbookComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<SpellbookComponent, LearnDoAfterComplete>(TeachSpell);
        SubscribeLocalEvent<SpellbookComponent, LearnDoAfterCancel>(OnLearnFail);

        SubscribeLocalEvent<InstantSpawnSpellEvent>(OnSpawnMagic);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<KnockSpellEvent>(OnKnockSpell);
        SubscribeLocalEvent<WorldSpawnSpellEvent>(OnSpawnSpell);

    }

    private void OnInit(EntityUid uid, SpellbookComponent component, ComponentInit args)
    {
        //Negative charges means the spell can be used without it running out.
        foreach (var (id, charges) in component.WorldSpells)
        {
            var spell = new WorldTargetAction(_prototypeManager.Index<WorldTargetActionPrototype>(id));
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell);
        }

        foreach (var (id, charges) in component.InstantSpells)
        {
            var spell = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(id));
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell);
        }

        foreach (var (id, charges) in component.EntitySpells)
        {
            var spell = new EntityTargetAction(_prototypeManager.Index<EntityTargetActionPrototype>(id));
            _actionsSystem.SetCharges(spell, charges < 0 ? null : charges);
            component.Spells.Add(spell);
        }
    }

    private void OnUse(EntityUid uid, SpellbookComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        AttemptLearn(uid, component, args);

        args.Handled = true;
    }

    private void AttemptLearn(EntityUid uid, SpellbookComponent component, UseInHandEvent args)
    {
        component.CancelToken = new();
        var doAfterEventArgs = new DoAfterEventArgs(args.User, component.LearnTime, component.CancelToken.Token, uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            NeedHand = true, //What, are you going to read with your eyes only??
            TargetFinishedEvent = new LearnDoAfterComplete(args.User),
            TargetCancelledEvent = new LearnDoAfterCancel(),
        };

        _doAfter.DoAfter(doAfterEventArgs);
    }

    private void TeachSpell(EntityUid uid, SpellbookComponent component, LearnDoAfterComplete ev)
    {
        component.CancelToken = null;

        if (ev.User == null)
            return;

        _actionsSystem.AddActions(ev.User, component.Spells, uid);
    }

    private void OnLearnFail(EntityUid uid, SpellbookComponent component, LearnDoAfterCancel args)
    {
    }

    #region Spells

    private void OnSpawnMagic(InstantSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        foreach (var position in GetSpawnPositions(transform, args.Pos))
        {
            var ent = Spawn(args.Prototype, position.SnapToGrid(EntityManager, _mapManager));

            if (args.PreventCollideWithCaster)
            {
                var comp = EnsureComp<PreventCollideComponent>(ent);
                comp.Uid = args.Performer;
            }
        }

        args.Handled = true;
    }

    private List<EntityCoordinates> GetSpawnPositions(TransformComponent casterXform, MagicSpawnData data)
    {
        switch (data)
        {
            case TargetCasterPos caster:
                return new List<EntityCoordinates>(1) {casterXform.Coordinates};
            case TargetInFront front:
            {
                // This is shit but you get the idea.
                var directionPos = casterXform.Coordinates.Offset(casterXform.LocalRotation.ToWorldVec().Normalized);

                if (!_mapManager.TryGetGrid(casterXform.GridID, out var mapGrid))
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
                        return new List<EntityCoordinates>()
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
                        return new List<EntityCoordinates>()
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

    /// <summary>
    /// Teleports the user to the clicked location
    /// </summary>
    /// <param name="args"></param>
    private void OnTeleportSpell(TeleportSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        if (_mapManager.TryFindGridAt(args.Target, out var grid))
        {
            var gridPosition = grid.WorldToLocal(args.Target.Position);

            transform.Coordinates = new EntityCoordinates(grid.GridEntityId, gridPosition);
            SoundSystem.Play(Filter.Pvs(args.Target), args.BlinkSound.GetSound());
        }
        else
        {
            var mapEntity = _mapManager.GetMapEntityIdOrThrow(args.Target.MapId);
            transform.AttachParent(mapEntity);
            transform.WorldPosition = args.Target.Position;
            SoundSystem.Play(Filter.Pvs(args.Target), args.BlinkSound.GetSound());
        }

        args.Handled = true;
    }

    /// <summary>
    /// Opens all doors within range
    /// </summary>
    /// <param name="args"></param>
    private void OnKnockSpell(KnockSpellEvent args)
    {
        if (args.Handled)
            return;

        //Get the position of the player
        var transform = Transform(args.Performer);
        var coords = transform.Coordinates;

        SoundSystem.Play(Filter.Pvs(coords), args.KnockSound.GetSound(), AudioParams.Default.WithVolume(args.KnockVolume));

        //Look for doors and don't open them if they're already open.
        foreach (var entity in _lookup.GetEntitiesInRange(coords, args.Range))
        {
            if (TryComp<AirlockComponent>(entity, out var airlock))
                airlock.BoltsDown = false;

            if (TryComp<DoorComponent>(entity, out var doorComp) && doorComp.State is not DoorState.Open)
                _doorSystem.StartOpening(doorComp.Owner);
        }

        args.Handled = true;
    }

    /// <summary>
    /// Spawns entity prototypes from a list within range of click.
    /// </summary>
    /// <remarks>
    /// It will offset mobs after the first mob based on the OffsetVector2 property supplied.
    /// </remarks>
    /// <param name="args"> The Spawn Spell Event args.</param>
    private void OnSpawnSpell(WorldSpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var targetMapCoords = args.Target;

        SpawnSpellHelper(args.Contents, targetMapCoords, args.TemporarySummon, args.OffsetVector2, args.Lifetime, _random);

        args.Handled = true;
    }

    /// <summary>
    /// Loops through a supplied list of entity prototypes and spawns them
    /// </summary>
    /// <remarks>
    /// If an offset of 0, 0 is supplied then the entities will all spawn on the same tile.
    /// Any other offset will spawn entities starting from the source Map Coordinates and will increment the supplied
    /// offset
    /// </remarks>
    /// <param name="entityEntries"> The list of Entities to spawn in</param>
    /// <param name="mapCoords"> Map Coordinates where the entities will spawn</param>
    /// <param name="temporarySummon"> Check to see if the entities should self delete</param>
    /// <param name="offsetVector2"> A Vector2 offset that the entities will spawn in</param>
    /// <param name="lifetime"> Sets the <see cref="SpawnSpellComponent"/> lifetime from the <see cref="WorldSpawnSpellEvent"/> args</param>
    /// <param name="random"> Resolves param, check out <see cref="EntitySpawnEntry"/> for what to put on the prototype</param>
    private void SpawnSpellHelper(List<EntitySpawnEntry> entityEntries, MapCoordinates mapCoords, bool temporarySummon, Vector2 offsetVector2, float lifetime, IRobustRandom? random)
    {
        var getProtos = EntitySpawnCollection.GetSpawns(entityEntries, random);

        var offsetCoords = mapCoords;
        foreach (var proto in getProtos)
        {
            var entity = Spawn(proto, offsetCoords);
            entity.SnapToGrid();
            offsetCoords = offsetCoords.Offset(offsetVector2);

            if (temporarySummon)
            {
                var comp = EnsureComp<TimedDespawnComponent>(entity);
                comp.Lifetime = lifetime;
            }
        }
    }

    #endregion

    #region DoAfterClasses

    private sealed class LearnDoAfterComplete : EntityEventArgs
    {
        public readonly EntityUid User;

        public LearnDoAfterComplete(EntityUid uid)
        {
            User = uid;
        }
    }

    private sealed class LearnDoAfterCancel : EntityEventArgs { }

    #endregion
}
