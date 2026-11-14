using System.Numerics;
using Content.Server.Radiation.Components;
using Content.Shared.Radiation.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    private readonly record struct SourceSnapshot(
        EntityUid Uid,
        NetEntity NetUid,
        MapId MapId,
        EntityUid? GridUid,
        EntityUid ParentUid,
        Vector2 LocalPosition,
        Vector2 WorldPosition,
        float Intensity,
        float Slope,
        float MaxRange);

    private readonly record struct ReceiverSnapshot(
        EntityUid Uid,
        NetEntity NetUid,
        MapId MapId,
        EntityUid? GridUid,
        EntityUid ParentUid,
        Vector2 LocalPosition,
        Vector2 WorldPosition,
        float Resistance);

    private readonly record struct GridSnapshot(
        EntityUid Uid,
        NetEntity NetUid,
        MapId MapId,
        Matrix3x2 InvWorldMatrix,
        ushort TileSize,
        Dictionary<Vector2i, float> Resistance,
        Box2 WorldAABB);

    private SourceSnapshot[] _sourceSnapshots = Array.Empty<SourceSnapshot>();
    private ReceiverSnapshot[] _receiverSnapshots = Array.Empty<ReceiverSnapshot>();
    private GridSnapshot[] _gridSnapshots = Array.Empty<GridSnapshot>();

    private const int SourceTreeThreshold = 16;

    private readonly Dictionary<EntityUid, int> _gridSnapshotIndex = new();
    private readonly B2DynamicTree<int> _sourceTree = new();

    private void BuildSnapshots()
    {
        BuildSourceSnapshots();
        BuildReceiverSnapshots();
        BuildGridSnapshots();
        BuildSourceTree();
    }

    private void BuildSourceSnapshots()
    {
        var capacity = Count<RadiationSourceComponent>();
        if (_sourceSnapshots.Length != capacity)
            _sourceSnapshots = new SourceSnapshot[capacity];

        var count = 0;
        var query = EntityQueryEnumerator<RadiationSourceComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var source, out var xform))
        {
            if (!source.Enabled)
                continue;

            var worldPosition = _transform.GetWorldPosition(xform);
            var intensity = source.Intensity * _stack.GetCount(uid);
            intensity = GetAdjustedRadiationIntensity(uid, intensity);

            if (intensity <= 0f)
                continue;

            var slope = source.Slope;
            var maxRange = slope > float.Epsilon ? intensity / slope : GridcastMaxDistance;
            maxRange = Math.Min(maxRange, GridcastMaxDistance);

            _sourceSnapshots[count++] = new SourceSnapshot(
                uid,
                GetNetEntity(uid),
                xform.MapID,
                xform.GridUid,
                xform.ParentUid,
                xform.LocalPosition,
                worldPosition,
                intensity,
                slope,
                maxRange);
        }

        if (count != _sourceSnapshots.Length)
            Array.Resize(ref _sourceSnapshots, count);
    }

    private void BuildReceiverSnapshots()
    {
        var capacity = Count<RadiationReceiverComponent>();
        if (_receiverSnapshots.Length != capacity)
            _receiverSnapshots = new ReceiverSnapshot[capacity];

        var count = 0;
        var query = EntityQueryEnumerator<RadiationReceiverComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var xform))
        {
            var worldPosition = _transform.GetWorldPosition(xform);
            var resistance = GetRadiationResistance(uid);

            _receiverSnapshots[count++] = new ReceiverSnapshot(
                uid,
                GetNetEntity(uid),
                xform.MapID,
                xform.GridUid,
                xform.ParentUid,
                xform.LocalPosition,
                worldPosition,
                resistance);
        }

        if (count != _receiverSnapshots.Length)
            Array.Resize(ref _receiverSnapshots, count);
    }

    private void BuildGridSnapshots()
    {
        _gridSnapshotIndex.Clear();

        var capacity = Count<RadiationGridResistanceComponent>();
        if (_gridSnapshots.Length != capacity)
            _gridSnapshots = new GridSnapshot[capacity];

        var count = 0;
        var query = EntityQueryEnumerator<RadiationGridResistanceComponent, MapGridComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var resistance, out var grid, out var xform))
        {
            var worldAABB = TransformBox(grid.LocalAABB, _transform.GetWorldMatrix(xform));

            _gridSnapshots[count] = new GridSnapshot(
                uid,
                GetNetEntity(uid),
                xform.MapID,
                _transform.GetInvWorldMatrix(xform),
                grid.TileSize,
                resistance.ResistancePerTile,
                worldAABB);

            _gridSnapshotIndex[uid] = count;
            count++;
        }

        if (count != _gridSnapshots.Length)
            Array.Resize(ref _gridSnapshots, count);
    }

    private void BuildSourceTree()
    {
        var state = false;
        _sourceTree.Clear(ref state, static (ref bool clearState, in int sourceIndex) => { });

        if (_sourceSnapshots.Length <= SourceTreeThreshold)
            return;

        for (var i = 0; i < _sourceSnapshots.Length; i++)
        {
            var source = _sourceSnapshots[i];
            var size = new Vector2(source.MaxRange * 2f, source.MaxRange * 2f);
            var aabb = Box2.CenteredAround(source.WorldPosition, size);
            _sourceTree.CreateProxy(in aabb, uint.MaxValue, i);
        }
    }

    private static Box2 TransformBox(Box2 box, Matrix3x2 matrix)
    {
        var bottomLeft = Vector2.Transform(new Vector2(box.Left, box.Bottom), matrix);
        var bottomRight = Vector2.Transform(new Vector2(box.Right, box.Bottom), matrix);
        var topLeft = Vector2.Transform(new Vector2(box.Left, box.Top), matrix);
        var topRight = Vector2.Transform(new Vector2(box.Right, box.Top), matrix);

        var min = Vector2.Min(Vector2.Min(bottomLeft, bottomRight), Vector2.Min(topLeft, topRight));
        var max = Vector2.Max(Vector2.Max(bottomLeft, bottomRight), Vector2.Max(topLeft, topRight));

        return new Box2(min, max);
    }
}
