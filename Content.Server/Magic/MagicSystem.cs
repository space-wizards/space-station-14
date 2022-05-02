using Content.Server.Coordinates.Helpers;
using Content.Server.Decals;
using Content.Server.Magic.Events;
using Content.Server.Wieldable;
using Content.Server.Wieldable.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
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

    private readonly List<EntityUid> ActiveWalls = new ();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpellbookComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpellbookComponent, ItemWieldedEvent>(OnWielded);
        SubscribeLocalEvent<SpellbookComponent, ItemUnwieldedEvent>(OnUnWield);

        SubscribeLocalEvent<RuneMagicEvent>(OnRuneMagic);
        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<ForceWallEvent>(OnForceWallSpell);
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var forcewall in EntityQuery<ForceWallComponent>())
        {
            forcewall.Timer += frameTime;

            if (forcewall.Timer > forcewall.ForceWallCooldown)
            {
                EntityManager.QueueDeleteEntity(forcewall.Owner);
                ActiveWalls.Remove(forcewall.Owner);
            }
        }
    }

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

    }

    private void OnForceWallSpell(ForceWallEvent args)
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
            EnsureComp<ForceWallComponent>(wall);
        }

        args.Handled = true;
    }
}
