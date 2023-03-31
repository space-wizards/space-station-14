using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Gravity;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Afterlight.ThirdDimension;

/// <summary>
/// This handles Z levels. I'm sorry to everyone who has to witness this.
/// </summary>
public sealed class SharedZLevelSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    [ViewVariables]
    private MapId?[] _mapAbove = new MapId?[] { };
    [ViewVariables]
    private MapId?[] _mapBelow = new MapId?[] { };

    public IReadOnlyList<MapId?> MapAbove => _mapAbove;
    public IReadOnlyList<MapId?> MapBelow => _mapBelow;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MapChangedEvent>(OnMapChanged);
        SubscribeNetworkEvent<MapListChangedEvent>(OnMapListChanged);
        if (_net.IsServer)
        {
            SubscribeLocalEvent<MoveEvent>(OnMove); // Sloth forgive me.
            _conHost.RegisterCommand("ztool", ZTool);
        }
    }

    private void OnMove(ref MoveEvent ev)
    {
        if (_mapBelow[(int) ev.Component.MapID] == null || HasComp<MapGridComponent>(ev.Sender) || _gravity.IsWeightless(ev.Sender) || HasComp<SharedGhostComponent>(ev.Sender))
            return; // get out!

        var mapEid = _map.GetMapEntityId(ev.Component.MapID);

        if (ev.Component.Coordinates.EntityId != mapEid)
            return; // Can't fall through the map if we're not on it!

        var phys = Comp<PhysicsComponent>(ev.Sender);
        if (phys.Momentum.Length < 0.2)
        {
            TryTraverse(false, ev.Sender);
            // THWACK
            _damageableSystem.TryChangeDamage(ev.Sender,
                new DamageSpecifier(_prototype.Index<DamageTypePrototype>("Blunt"), FixedPoint2.New(69)));
        }
    }

    [AnyCommand]
    private void ZTool(IConsoleShell shell, string argstr, string[] args)
    {
        if (shell.Player?.AttachedEntity is not {} ent)
            return;

        if (!_admin.IsAdmin(ent))
        {
            shell.WriteLine("This code may be bad but it's not gonna let you break things.");
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "above":
            {
                var xform = Transform(ent);
                var map = MapAbove[(int) xform.MapID];
                shell.WriteLine($"The map above you is {(map is not null ? ToPrettyString(_map.GetMapEntityId(map.Value)) : "none")}");
                break;
            }
            case "below":
            {
                var xform = Transform(ent);
                var map = MapBelow[(int) xform.MapID];
                shell.WriteLine($"The map below you is {(map is not null ? ToPrettyString(_map.GetMapEntityId(map.Value)) : "none")}");
                break;
            }
            case "link":
            {
                var dir = args[2].ToLowerInvariant();
                var baseMap = new MapId(int.Parse(args[1]));
                var linkedMap = new MapId(int.Parse(args[3]));

                if (dir != "above" && dir != "below")
                    return;

                if (dir != "above")
                {
                    LinkMaps(baseMap, linkedMap);
                }
                else
                {
                    LinkMaps(linkedMap, baseMap);
                }

                break;
            }
            case "traverse":
            {
                var dir = args[1].ToLowerInvariant();

                if (dir != "above" && dir != "below")
                    return;

                TryTraverse(dir == "above", ent);

                break;
            }
        }
    }

    public int AllMapsBelow(MapId map, ref MapId[] maps)
    {
        var curr = map;
        var idx = 0;

        while (MapBelow[(int)curr] is { } below && idx < maps.Length)
        {
            maps[idx++] = below;
            curr = below;
        }

        return idx;
    }

    public bool TryTraverse(bool direction, EntityUid traverser, TransformComponent? xform = default!)
    {
        if (!Resolve(traverser, ref xform))
            return false;

        var worldPosition = _xformSystem.GetWorldPosition(xform);
        MapId? newMap;
        if (direction) // Going up!
        {
            newMap = MapAbove[(int)xform.MapID];
        }
        else
        {
            newMap = MapBelow[(int)xform.MapID];
        }
        Logger.Debug($"Traversing to {newMap}..");

        if (newMap is null)
            return false;

        var coords = EntityCoordinates.FromMap(_map, new MapCoordinates(worldPosition, newMap.Value));
        _xformSystem.SetCoordinates(traverser, coords);

        return true;
    }

    public void LinkMaps(MapId below, MapId above)
    {
        _mapBelow[(int)above] = below;
        _mapAbove[(int)below] = above;
        UpdateMapList();
    }

    private void OnMapListChanged(MapListChangedEvent ev)
    {
        if (!_net.IsClient)
            return;

        //yoink
        _mapAbove = ev.MapAbove;
        _mapBelow = ev.MapBelow;
    }

    private void OnMapChanged(MapChangedEvent ev)
    {
        if (!ev.Created)
            return;

        if ((int) ev.Map + 1 > _mapAbove.Length)
        {
            // Resize time.
            Array.Resize(ref _mapAbove, (int) ev.Map + 1);
            Array.Resize(ref _mapBelow, (int) ev.Map + 1);
            UpdateMapList();
        }
    }

    public void UpdateMapList()
    {
        if (!_net.IsServer)
            return;

        RaiseNetworkEvent(new MapListChangedEvent(_mapAbove, _mapBelow));
    }

    [Serializable, NetSerializable]
    public sealed class MapListChangedEvent : EntityEventArgs
    {
        public MapId?[] MapAbove;
        public MapId?[] MapBelow;

        public MapListChangedEvent(MapId?[] mapAbove, MapId?[] mapBelow)
        {
            MapAbove = mapAbove;
            MapBelow = mapBelow;
        }
    }
}
