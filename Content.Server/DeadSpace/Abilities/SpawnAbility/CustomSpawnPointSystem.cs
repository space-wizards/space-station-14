using Content.Server.Popups;
using Content.Shared.Actions;
using Robust.Shared.Containers;
using Content.Shared.DoAfter;
using Content.Shared.DeadSpace.Abilities.SpawnAbility.Components;
using Content.Shared.Maps;
using Content.Shared.DeadSpace.Abilities.SpawnAbility;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Shared.DeadSpace.EntityPanel;
using Content.Shared.Mobs.Components;

namespace Content.Server.DeadSpace.Abilities.SpawnAbility;

public sealed class CustomSpawnPointSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CustomSpawnPointComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CustomSpawnPointComponent, SpawnPointDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<CustomSpawnPointComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CustomSpawnPointComponent, SelectEntityActionEvent>(OnSelectEntityAction);
        SubscribeLocalEvent<CustomSpawnPointComponent, SpawnPointActionEvent>(OnSpawnPointAction);

        SubscribeNetworkEvent<SelectEntityEvent>(OnSelectEntity);
    }

    private void OnComponentInit(EntityUid uid, CustomSpawnPointComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.SpawnPointActionEntity, component.SpawnPointAction);
        _actionsSystem.AddAction(uid, ref component.SelectEntityActionEntity, component.SelectEntityAction);
    }

    private void OnShutdown(EntityUid uid, CustomSpawnPointComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.SpawnPointActionEntity);
        _actionsSystem.RemoveAction(uid, component.SelectEntityActionEntity);
    }

    private void OnSelectEntityAction(EntityUid uid, CustomSpawnPointComponent component, SelectEntityActionEvent args)
    {
        if (args.Handled)
            return;

        if (EntityManager.TryGetComponent<ActorComponent?>(uid, out var actorComponent))
        {
            var ev = new RequestEntityMenuEvent(uid.Id, false, true);

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
        if (msg.IsUseSpawnPointSystem)
        {
            if (EntityManager.TryGetComponent<CustomSpawnPointComponent>(new EntityUid(msg.Target), out var CustomSpawnPointComponent))
            {
                CustomSpawnPointComponent.SelectEntity = msg.PrototypeId;
            }
        }
    }
    private void OnSpawnPointAction(EntityUid uid, CustomSpawnPointComponent component, SpawnPointActionEvent args)
    {
        if (args.Handled)
            return;

        var tile = GetTileCoords(uid, args.Target);

        if (tile == null)
            return;

        component.Coords = args.Target;
        BeginSpawn(uid, component);

        args.Handled = true;
    }

    private TileRef? GetTileCoords(EntityUid uid, EntityCoordinates coords)
    {
        if (_container.IsEntityOrParentInContainer(uid))
            return null;

        var tile = coords.GetTileRef(EntityManager, _mapMan);

        if (tile == null)
            return null;

        if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable) ||
        _turf.IsTileBlocked(tile.Value, CollisionGroup.MidImpassable) ||
        _turf.IsTileBlocked(tile.Value, CollisionGroup.DoorPassable) ||
        _turf.IsTileBlocked(tile.Value, CollisionGroup.LowImpassable) ||
        _turf.IsTileBlocked(tile.Value, CollisionGroup.HighImpassable))
        {
            _popupSystem.PopupEntity(Loc.GetString("Тут нельзя разместить"), uid, uid);
            return null;
        }

        foreach (var entity in _lookup.GetLocalEntitiesIntersecting(tile.Value, 0f, LookupFlags.Static))
        {
            if (HasComp<MobStateComponent>(entity) && entity != uid)
            {
                _popupSystem.PopupEntity(Loc.GetString("Тут нельзя разместить!"), uid, uid);
                return null;
            }
        }

        return tile;
    }

    private void BeginSpawn(EntityUid uid, CustomSpawnPointComponent component)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(component.Duration), new SpawnPointDoAfterEvent(), uid)
        {
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, CustomSpawnPointComponent component, SpawnPointDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var tile = GetTileCoords(uid, component.Coords);

        if (tile == null)
            return;

        var ent = Spawn(component.SelectEntity, _turf.GetTileCenter(tile.Value));

        if (component.SpawnSound == null)
            return;

        _audio.PlayPvs(component.SpawnSound, ent);
    }
}
