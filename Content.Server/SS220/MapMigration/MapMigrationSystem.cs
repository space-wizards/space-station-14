using Content.Server.GameTicking;
using Content.Shared.Doors.Components;
using Content.Shared.SS220.CCVars;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.MapMigration;

public sealed class MapMigrationSystem_SS220 : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _rotateDoors = false;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars220.MigrationAlignDoors, value =>
        {
            _rotateDoors = value;

            if (value)
                SubscribeLocalEvent<AirlockComponent, MapInitEvent>(OnCompInit);
        }, true);
    }

    private void OnCompInit(Entity<AirlockComponent> entity, ref MapInitEvent args)
    {
        if (!_rotateDoors)
            return;

        RotateDoor(entity);
    }

    private bool CheckTileOccupied(Vector2i pos, EntityUid gridUid, MapGridComponent grid)
    {
        var entitiesOnTile = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, pos);
        while (entitiesOnTile.MoveNext(out var entity))
        {
            if (!entity.HasValue)
                continue;

            var proto = MetaData(entity.Value).EntityPrototype;
            if (proto != null)
            {
                if (proto.ID == "FirelockEdge")
                    continue;

                if (proto.Parents != null && Array.IndexOf(proto.Parents, "Table") > -1)
                    return true;
            }

            if (_tag.HasAnyTag(entity.Value, "Wall", "Window"))
                return true;

            if (HasComp<DoorComponent>(entity))
                return true;
        }

        return false;
    }

    public void RotateDoor(EntityUid airlockUid, EntityUid? gridUid = null)
    {
        var transform = Transform(airlockUid);
        if (!gridUid.HasValue)
            gridUid = transform.GridUid;

        if (!gridUid.HasValue)
            return;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        // Игнорируем некоторые структуры
        var proto = MetaData(airlockUid).EntityPrototype;
        if (proto?.ID == "FirelockEdge")
            return;

        if (proto != null && proto.Parents != null)
        {
            if (Array.IndexOf(proto.Parents, "BaseWindoor") > -1 ||
                Array.IndexOf(proto.Parents, "Windoor") > -1 ||
                Array.IndexOf(proto.Parents, "BaseSecureWindoor") > -1 ||
                Array.IndexOf(proto.Parents, "WindoorSecure") > -1)
            {
                return;
            }
        }

        if (transform.Anchored)
        {
            var pos = _map.CoordinatesToTile(gridUid.Value, grid, transform.Coordinates);

            if (!CheckTileOccupied(pos + new Vector2i(1, 0), gridUid.Value, grid))
            {
                _transform.SetLocalRotationNoLerp(airlockUid, Angle.FromDegrees(90), transform);
                return;
            }

            if (!CheckTileOccupied(pos + new Vector2i(-1, 0), gridUid.Value, grid))
            {
                _transform.SetLocalRotationNoLerp(airlockUid, Angle.FromDegrees(270), transform);
                return;
            }

            if (!CheckTileOccupied(pos + new Vector2i(0, 1), gridUid.Value, grid))
            {
                _transform.SetLocalRotationNoLerp(airlockUid, Angle.FromDegrees(180), transform);
                return;
            }

            if (!CheckTileOccupied(pos + new Vector2i(0, -1), gridUid.Value, grid))
            {
                _transform.SetLocalRotationNoLerp(airlockUid, Angle.FromDegrees(0), transform);
                return;
            }
        }
    }
}
