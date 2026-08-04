// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
using System.Numerics;
using Content.Server.Weapons.Melee;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Lavaland.SmokeBlade;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Lavaland;

public sealed class SmokeBladeSystem : EntitySystem
{
    private static readonly Vector2i[] Neighbours =
    {
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1, 0),               new(1, 0),
        new(-1, 1),  new(0, 1),  new(1, 1),
    };

    private readonly HashSet<EntityUid> _tileBlockers = new();

    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmokeBladeActionEvent>(OnAction);
        SubscribeLocalEvent<SmokeBladeProtectionComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SmokeBladeCloudComponent>();
        while (query.MoveNext(out var uid, out var cloud))
        {
            if (now >= cloud.EndTime)
            {
                if (TryComp<SmokeBladeProtectionComponent>(cloud.Creator, out _) &&
                    !HasActiveOwnedCloud(cloud.Creator, uid))
                    RemComp<SmokeBladeProtectionComponent>(cloud.Creator);
                _audio.Stop(cloud.AmbientStream);
                foreach (var visual in cloud.Visuals)
                {
                    if (Exists(visual))
                        QueueDel(visual);
                }
                QueueDel(uid);
                continue;
            }

            if (now < cloud.NextTick || !TryComp<MapGridComponent>(cloud.Grid, out var grid))
                continue;

            cloud.NextTick = now + cloud.TickInterval;
            TickCloud(cloud, grid);
        }
    }

    private void OnAction(SmokeBladeActionEvent args)
    {
        if (args.Handled ||
            !_turf.TryGetTileRef(Transform(args.Performer).Coordinates, out var center) ||
            !TryComp<MapGridComponent>(center.Value.GridUid, out var grid))
            return;

        var controller = Spawn(null, Transform(args.Performer).Coordinates);
        var cloud = AddComp<SmokeBladeCloudComponent>(controller);
        cloud.Creator = args.Performer;
        cloud.Grid = center.Value.GridUid;
        cloud.Tiles = SpreadTiles(center.Value, grid, args.Radius);
        foreach (var tile in cloud.Tiles)
        {
            var coordinates = _map.GridTileToLocal(cloud.Grid, grid, tile);
            cloud.Visuals.Add(Spawn(args.VisualPrototype, coordinates));
        }
        cloud.AmbientStream = _audio.PlayPvs(
            args.AmbientSound,
            controller,
            args.AmbientSound.Params.WithLoop(true))?.Entity;
        cloud.Damage = args.Damage;
        cloud.TickInterval = args.TickInterval;
        cloud.NextTick = _timing.CurTime;
        cloud.EndTime = _timing.CurTime + args.Duration;

        EnsureComp<SmokeBladeProtectionComponent>(args.Performer);
        args.Handled = true;
    }

    private HashSet<Vector2i> SpreadTiles(TileRef center, MapGridComponent grid, int radius)
    {
        var result = new HashSet<Vector2i> { center.GridIndices };
        var queue = new Queue<(Vector2i Tile, int Distance)>();
        queue.Enqueue((center.GridIndices, 0));

        while (queue.TryDequeue(out var entry))
        {
            if (entry.Distance >= radius)
                continue;

            foreach (var offset in Neighbours)
            {
                var next = entry.Tile + offset;
                if (Math.Abs(next.X - center.GridIndices.X) > radius ||
                    Math.Abs(next.Y - center.GridIndices.Y) > radius ||
                    result.Contains(next) ||
                    !_map.TryGetTileRef(center.GridUid, grid, next, out var tile) ||
                    tile.Tile.IsEmpty ||
                    IsSmokeBlocked(center.GridUid, next, grid))
                    continue;

                result.Add(next);
                queue.Enqueue((next, entry.Distance + 1));
            }
        }

        return result;
    }

    private bool IsSmokeBlocked(EntityUid gridUid, Vector2i tile, MapGridComponent grid)
    {
        _tileBlockers.Clear();
        _lookup.GetLocalEntitiesIntersecting(
            gridUid,
            tile,
            _tileBlockers,
            0f,
            LookupFlags.Static,
            grid);

        foreach (var blocker in _tileBlockers)
        {
            if (!TryComp<FixturesComponent>(blocker, out var fixtures))
                continue;

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (fixture.Hard && (fixture.CollisionLayer & (int) CollisionGroup.Opaque) != 0)
                    return true;
            }
        }

        return false;
    }
    private void TickCloud(SmokeBladeCloudComponent cloud, MapGridComponent grid)
    {
        var hit = new HashSet<EntityUid>();

        foreach (var tile in cloud.Tiles)
        {
            var coords = _map.GridTileToLocal(cloud.Grid, grid, tile);
            foreach (var target in _lookup.GetEntitiesInRange(coords, 0.45f))
            {
                if (target == cloud.Creator || !hit.Add(target))
                    continue;

                _damage.TryChangeDamage(target, cloud.Damage, origin: cloud.Creator, interruptsDoAfters: false);

                if (!HasComp<ActorComponent>(target) ||
                    cloud.NextAttackAnimation.TryGetValue(target, out var nextAnimation) && nextAnimation > _timing.CurTime)
                    continue;

                _melee.DoLunge(
                    target,
                    target,
                    Angle.Zero,
                    new Vector2(0.75f, 0f),
                    "WeaponArcFist",
                    predicted: false);
                cloud.NextAttackAnimation[target] = _timing.CurTime + cloud.AttackAnimationInterval;
            }
        }


    }

    private void OnBeforeDamage(Entity<SmokeBladeProtectionComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!_turf.TryGetTileRef(Transform(ent).Coordinates, out var tile))
            return;

        var query = EntityQueryEnumerator<SmokeBladeCloudComponent>();
        while (query.MoveNext(out _, out var cloud))
        {
            if (cloud.Creator != ent.Owner ||
                tile.Value.GridUid != cloud.Grid ||
                !cloud.Tiles.Contains(tile.Value.GridIndices))
                continue;

            args.Cancelled = true;
            return;
        }
    }

    private bool HasActiveOwnedCloud(EntityUid creator, EntityUid excluding)
    {
        var query = EntityQueryEnumerator<SmokeBladeCloudComponent>();
        while (query.MoveNext(out var uid, out var cloud))
        {
            if (uid != excluding && cloud.Creator == creator && cloud.EndTime > _timing.CurTime)
                return true;
        }

        return false;
    }
}

[RegisterComponent]
public sealed partial class SmokeBladeCloudComponent : Component
{
    public EntityUid Creator;
    public EntityUid Grid;
    public HashSet<Vector2i> Tiles = new();
    public DamageSpecifier Damage = new();
    public TimeSpan TickInterval;
    public TimeSpan NextTick;
    public TimeSpan EndTime;
    public EntityUid? AmbientStream;
    public readonly List<EntityUid> Visuals = new();
    public TimeSpan AttackAnimationInterval = TimeSpan.FromSeconds(0.4);
    public readonly Dictionary<EntityUid, TimeSpan> NextAttackAnimation = new();
}

[RegisterComponent]
public sealed partial class SmokeBladeProtectionComponent : Component
{
}