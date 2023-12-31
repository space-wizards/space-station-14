using Content.Server.Administration.Logs;
using Content.Server.Ame.Components;
using Content.Server.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction;

public sealed class SharedFlatpackSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FlatpackComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<FlatpackComponent> ent, ref InteractUsingEvent args)
    {
        var (uid, comp) = ent;
        if (!_tool.HasQuality(args.Used, comp.QualityNeeded))
            return;

        var xform = Transform(ent);

        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        args.Handled = true;

        if (comp.Entity is not { } entProtoId)
        {
            Log.Error($"No entity prototype present for flatpack {ToPrettyString(ent)}.");
            QueueDel(ent);
            return;
        }

        var buildPos = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        var entProto = _prototypeManager.Index(entProtoId);
        var gridXform = Transform(grid);
        var localPos = _map.GridTileToLocal(grid, gridComp, buildPos);

        if (entProto.TryGetComponent<FixturesComponent>(out var fixtures))
        {
            var collisions = new HashSet<Entity<PhysicsComponent>>();
            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!fixture.Hard)
                    continue;

                collisions.Clear();

                //todo fix this shit
                var transform = new Transform(localPos.ToMapPos(EntityManager, _transform)., _transform.GetWorldRotation(gridXform));
                var aabb = fixture.Shape.ComputeAABB(transform, 0);
                _entityLookup.GetEntitiesIntersecting(xform.MapID, aabb, collisions, LookupFlags.Dynamic | LookupFlags.Static);

                if (collisions.Count > 0)
                {
                    _popup.PopupEntity(Loc.GetString("flatpack-unpack-no-room"), uid, args.User);
                    return;
                }
            }
        }

        var spawn = Spawn(comp.Entity, _map.GridTileToLocal(grid, gridComp, buildPos));
        _adminLogger.Add(LogType.Construction, LogImpact.Low,
            $"{ToPrettyString(args.User):player} unpacked {ToPrettyString(spawn):entity} at {xform.Coordinates} from {ToPrettyString(uid):entity}");
        _audio.PlayPredicted(comp.UnpackSound, spawn, args.User);

        QueueDel(uid);
    }
}
