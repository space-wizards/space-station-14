using Content.Server.Popups;
using Content.Shared.Actions;
using Robust.Shared.Containers;
using Content.Shared.DoAfter;
using Content.Shared.DeadSpace.Abilities.SpawnAbility.Components;
using Content.Shared.Maps;
using Content.Shared.DeadSpace.Abilities.SpawnAbility;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Shared.DeadSpace.EntityPanel;
using Content.Shared.Mobs.Components;
using Content.Shared.Tag;
using Content.Server.Spreader;

namespace Content.Server.DeadSpace.Abilities.SpawnAbility;

public sealed class SpawnAgainstSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnAgainstComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SpawnAgainstComponent, SpawnAgainstDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SpawnAgainstComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpawnAgainstComponent, SpawnAgainstActionEvent>(OnSpawnAgainstAction);

        SubscribeNetworkEvent<SelectEntityEvent>(OnSelectEntity);
    }

    private void OnComponentInit(EntityUid uid, SpawnAgainstComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.SpawnAgainstActionEntity, component.SpawnAgainstAction);
    }

    private void OnShutdown(EntityUid uid, SpawnAgainstComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.SpawnAgainstActionEntity);
    }

    private void OnSpawnAgainstAction(EntityUid uid, SpawnAgainstComponent component, SpawnAgainstActionEvent args)
    {
        if (args.Handled)
            return;

        if (EntityManager.TryGetComponent<ActorComponent?>(uid, out var actorComponent))
        {
            var ev = new RequestEntityMenuEvent(uid.Id, false, false);

            foreach (var entityUid in component.SpawnedEntities)
            {
                ev.Prototypes.Add(entityUid);
            }

            ev.Prototypes.Sort();
            RaiseNetworkEvent(ev, actorComponent.PlayerSession);

        }

        args.Handled = true;
    }

    private void OnSelectEntity(SelectEntityEvent msg)
    {
        if (!msg.IsUseEvolutionSystem && !msg.IsUseSpawnPointSystem)
        {
            if (EntityManager.TryGetComponent<SpawnAgainstComponent>(new EntityUid(msg.Target), out var spawnAgainstComponent))
            {
                spawnAgainstComponent.SelectEntity = msg.PrototypeId;
                SpawnAgainst(new EntityUid(msg.Target), spawnAgainstComponent);
            }
        }
    }
    private void SpawnAgainst(EntityUid uid, SpawnAgainstComponent comp)
    {
        var tile = GetTileCoords(uid);

        if (tile == null)
            return;

        BeginSpawn(uid, comp);
    }

    private TileRef? GetTileCoords(EntityUid uid)
    {
        if (_container.IsEntityOrParentInContainer(uid))
            return null;

        var xform = Transform(uid);
        // Get the tile in front of the uni
        var offsetValue = xform.LocalRotation.ToWorldVec();
        var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager, _mapMan);
        var tile = coords.GetTileRef(EntityManager, _mapMan);
        if (tile == null)
            return null;

        // Check there are no walls there
        if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable) ||
        _turf.IsTileBlocked(tile.Value, CollisionGroup.MidImpassable) ||
        _turf.IsTileBlocked(tile.Value, CollisionGroup.DoorPassable) ||
        _turf.IsTileBlocked(tile.Value, CollisionGroup.LowImpassable) ||
        _turf.IsTileBlocked(tile.Value, CollisionGroup.HighImpassable))
        {
            _popupSystem.PopupEntity(Loc.GetString("Тут нельзя разместить"), uid, uid);
            return null;
        }

        foreach (var entity in _lookup.GetLocalEntitiesIntersecting(tile.Value, 0f))
        {
            if (HasComp<MobStateComponent>(entity) && entity != uid)
            {
                _popupSystem.PopupEntity(Loc.GetString("Тут нельзя разместить!"), uid, uid);
                return null;
            }
        }

        bool contains = false;
        _spreader.GetNeighbors(uid, xform, "Kudzu", out var freeTiles, out _, out _);

        foreach (var (grid, tileRef) in freeTiles)
        {
            if (tileRef == tile.Value)
                contains = true;
        }

        if (!contains)
        {
            _popupSystem.PopupEntity(Loc.GetString("Тут нельзя разместить!"), uid, uid);
            return null;
        }


        return tile;
    }

    private void BeginSpawn(EntityUid uid, SpawnAgainstComponent component)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(component.Duration), new SpawnAgainstDoAfterEvent(), uid)
        {
            DistanceThreshold = 3,
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, SpawnAgainstComponent component, SpawnAgainstDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var tile = GetTileCoords(uid);

        if (tile == null)
            return;

        var ent = Spawn(component.SelectEntity, _turf.GetTileCenter(tile.Value));

        if (component.SpawnSound == null)
            return;

        _audio.PlayPvs(component.SpawnSound, uid);
    }
}
