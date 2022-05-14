using Content.Server.Construction.Conditions;
using Content.Server.Coordinates.Helpers;
using Content.Server.Decals;
using Content.Server.DoAfter;
using Content.Server.Doors.Components;
using Content.Server.Magic.Events;
using Content.Server.Wieldable;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

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

    private readonly List<EntityUid> ActiveWalls = new ();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpellbookComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpellbookComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<SpellbookComponent, LearnDoAfterComplete>(TeachSpell);

        SubscribeLocalEvent<RuneMagicEvent>(OnRuneMagic);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<ForceWallSpellEvent>(OnForceWallSpell);
        SubscribeLocalEvent<KnockSpellEvent>(OnKnockSpell);
        SubscribeLocalEvent<SpawnSpellEvent>(OnSpawnSpell);
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var forcewall in EntityQuery<ForceWallSpellComponent>())
        {
            forcewall.Timer += frameTime;

            if (forcewall.Timer > forcewall.ForceWallCooldown)
            {
                EntityManager.QueueDeleteEntity(forcewall.Owner);
                ActiveWalls.Remove(forcewall.Owner);
            }
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

    #region Spells

    private void OnRuneMagic(RuneMagicEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);
        Spawn(args.RunePrototype, transform.Coordinates.SnapToGrid());

        args.Handled = true;
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
            transform.WorldPosition = args.Target.Position;
            transform.AttachParent(mapEntity);
            SoundSystem.Play(Filter.Pvs(args.Target), args.BlinkSound.GetSound());
        }

        args.Handled = true;
    }

    /// <summary>
    /// Spawns 3 walls in front of the caster in a 3x1/1x3 pattern
    /// Disappears after a set amount of time
    /// </summary>
    /// <param name="args"></param>
    private void OnForceWallSpell(ForceWallSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);
        var directionPos = transform.Coordinates.Offset(transform.LocalRotation.ToWorldVec().Normalized);

        if (!_mapManager.TryGetGrid(transform.GridID, out var mapGrid))
            return;
        if (!directionPos.TryGetTileRef(out var tileReference, EntityManager, _mapManager))
            return;

        var tileIndex = tileReference.Value.GridIndices;
        var coords = mapGrid.GridTileToLocal(tileIndex);
        EntityCoordinates coordsPlus;
        EntityCoordinates coordsMinus;

        var dir = transform.LocalRotation.GetCardinalDir();
        switch (dir)
        {
            case Direction.North:
            case Direction.South:
            {
                coordsPlus = mapGrid.GridTileToLocal(tileIndex + (1, 0));
                coordsMinus = mapGrid.GridTileToLocal(tileIndex + (-1, 0));
                break;
            }
            case Direction.East:
            case Direction.West:
            {
                coordsPlus = mapGrid.GridTileToLocal(tileIndex + (0, 1));
                coordsMinus = mapGrid.GridTileToLocal(tileIndex + (0, -1));
                break;
            }
            default:
                return;
        }

        SoundSystem.Play(Filter.Pvs(coords), args.ForceWallSound.GetSound(), AudioParams.Default.WithVolume(args.ForceWallVolume));
        ActiveWalls.Add(Spawn(args.WallPrototype, coords));
        ActiveWalls.Add(Spawn(args.WallPrototype, coordsPlus));
        ActiveWalls.Add(Spawn(args.WallPrototype, coordsMinus));

        foreach (var wall in ActiveWalls)
        {
            EnsureComp<ForceWallSpellComponent>(wall);
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
    private void OnSpawnSpell(SpawnSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);
        var targetMapCoords = args.Target;

        if (!_mapManager.TryGetGrid(transform.GridID, out var mapGrid))
            return;

        var getProtos = EntitySpawnCollection.GetSpawns(args.Contents);

        //Loop through the supplied list of entity prototypes.
        //Starting from initial click coordinates, spawning each uniquely on a new offset position.
        var offsetCoords = targetMapCoords;
        foreach (var proto in getProtos)
        {
            Spawn(proto, offsetCoords).SnapToGrid();
            offsetCoords = offsetCoords.Offset(args.OffsetVector2);
        }

        args.Handled = true;
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
