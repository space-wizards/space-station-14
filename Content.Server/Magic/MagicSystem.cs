using Content.Server.Decals;
using Content.Server.Magic.Events;
using Content.Server.Wieldable;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Maps;
using Robust.Shared.Map;
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

    private readonly List<EntityUid> ActiveWalls = new ();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpellbookComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpellbookComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<SpellbookComponent, ItemUnwieldedEvent>(OnUnWield);

        SubscribeLocalEvent<RuneMagicEvent>(OnRuneMagic);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<ForceWallSpellEvent>(OnForceWallSpell);
        SubscribeLocalEvent<KnockSpellEvent>(OnKnockSpell);
    }

    private void OnInit(EntityUid uid, SpellbookComponent component, ComponentInit args)
    {
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

    #region Wielding

    private void OnWielded(EntityUid uid, SpellbookComponent component, ItemWieldedEvent args)
    {
        if (args.User == null)
            return;

        _actionsSystem.AddActions(args.User.Value, component.Spells, uid);
    }

    private void OnUnWield(EntityUid uid, SpellbookComponent component, ItemUnwieldedEvent args)
    {
        if (args.User == null)
            return;

        _actionsSystem.RemoveProvidedActions(args.User.Value, uid);
    }

    #endregion

    #region Spells

    private void OnRuneMagic(RuneMagicEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);
        Spawn(args.RunePrototype, transform.Coordinates);
        args.Handled = true;
    }

    private void OnTeleportSpell(TeleportSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        if (_mapManager.TryFindGridAt(args.Target, out var grid))
        {
            var gridPosition = grid.WorldToLocal(args.Target.Position);

            transform.Coordinates = new EntityCoordinates(grid.GridEntityId, gridPosition);
        }
        else
        {
            var mapEntity = _mapManager.GetMapEntityIdOrThrow(args.Target.MapId);
            transform.WorldPosition = args.Target.Position;
            transform.AttachParent(mapEntity);
        }

        args.Handled = true;
    }

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

        //Look for doors and don't open them if they're already open.
        foreach (var entity in _lookup.GetEntitiesInRange(coords, args.Range))
        {
            if (TryComp<DoorComponent>(entity, out var doorComp) && doorComp.State is not DoorState.Open)
                _doorSystem.StartOpening(doorComp.Owner);
        }

        args.Handled = true;
    }

    #endregion
}
