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
        Vector2 WorldPosition);

    // Resistance references the live component dictionary. This is safe because the parallel job
    // runs only while the main thread is blocked in ProcessNow.
    private readonly record struct GridSnapshot(
        EntityUid Uid,
        NetEntity NetUid,
        MapId MapId,
        Matrix3x2 InvLocalMatrix,
        ushort TileSize,
        Dictionary<Vector2i, float> Resistance,
        Box2 WorldAABB);

    private SourceSnapshot[] _sourceSnapshots = Array.Empty<SourceSnapshot>();
    private ReceiverSnapshot[] _receiverSnapshots = Array.Empty<ReceiverSnapshot>();
    private GridSnapshot[] _gridSnapshots = Array.Empty<GridSnapshot>();

    private int _sourceCount;
    private int _receiverCount;
    private int _gridCount;

    private readonly Dictionary<EntityUid, int> _gridSnapshotIndex = new();
    private readonly B2DynamicTree<int> _sourceTree = new();

    private void BuildSourceSnapshots(bool saveDebugInfo)
    {
        var capacity = Count<RadiationSourceComponent>();
        if (_sourceSnapshots.Length < capacity)
            _sourceSnapshots = new SourceSnapshot[capacity];

        _sourceCount = 0;
        var query = EntityQueryEnumerator<RadiationSourceComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var source, out var xform))
        {
            if (!source.Enabled ||
                !float.IsFinite(source.Intensity) ||
                source.Intensity <= 0f ||
                !float.IsFinite(source.Slope) ||
                source.Slope < 0f)
            {
                continue;
            }

            var worldPosition = _transform.GetWorldPosition(xform);
            if (!IsSanePosition(worldPosition))
                continue;

            var intensity = source.Intensity * _stack.GetCount(uid);
            if (!float.IsFinite(intensity) || intensity <= 0f)
                continue;

            intensity = GetAdjustedRadiationIntensity(uid, intensity);
            if (!float.IsFinite(intensity) || intensity <= 0f)
                continue;

            var slope = source.Slope;
            var maxRange = slope > 0f ? intensity / slope : GridcastMaxDistance;

            if (!float.IsFinite(maxRange))
                maxRange = GridcastMaxDistance;

            maxRange = Math.Min(maxRange, GridcastMaxDistance);

            _sourceSnapshots[_sourceCount++] = new SourceSnapshot(
                uid,
                saveDebugInfo ? GetNetEntity(uid) : default,
                xform.MapID,
                xform.GridUid,
                xform.ParentUid,
                xform.LocalPosition,
                worldPosition,
                intensity,
                slope,
                maxRange);
        }
    }

    private void BuildReceiverSnapshots(bool saveDebugInfo)
    {
        var capacity = Count<RadiationReceiverComponent>();
        if (_receiverSnapshots.Length < capacity)
            _receiverSnapshots = new ReceiverSnapshot[capacity];

        _receiverCount = 0;
        var query = EntityQueryEnumerator<RadiationReceiverComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var receiver, out var xform))
        {
            var worldPosition = _transform.GetWorldPosition(xform);
            if (!IsSanePosition(worldPosition))
            {
                receiver.CurrentRadiation = 0f;
                continue;
            }

            _receiverSnapshots[_receiverCount++] = new ReceiverSnapshot(
                uid,
                saveDebugInfo ? GetNetEntity(uid) : default,
                xform.MapID,
                xform.GridUid,
                xform.ParentUid,
                xform.LocalPosition,
                worldPosition);
        }
    }

    private void BuildGridSnapshots(bool saveDebugInfo)
    {
        _gridSnapshotIndex.Clear();

        var capacity = Count<RadiationGridResistanceComponent>();
        if (_gridSnapshots.Length < capacity)
            _gridSnapshots = new GridSnapshot[capacity];

        var previousCount = _gridCount;
        _gridCount = 0;
        var query = EntityQueryEnumerator<RadiationGridResistanceComponent, MapGridComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var resistance, out var grid, out var xform))
        {
            if (grid.TileSize == 0)
                continue;

            var worldAABB = TransformBox(grid.LocalAABB, _transform.GetWorldMatrix(xform));
            var invLocalMatrix = xform.InvLocalMatrix;

            if (!IsSaneBox(worldAABB) || !IsFinite(invLocalMatrix))
                continue;

            _gridSnapshots[_gridCount] = new GridSnapshot(
                uid,
                saveDebugInfo ? GetNetEntity(uid) : default,
                xform.MapID,
                invLocalMatrix,
                grid.TileSize,
                resistance.ResistancePerTile,
                worldAABB);

            _gridSnapshotIndex[uid] = _gridCount;
            _gridCount++;
        }

        if (previousCount > _gridCount)
            Array.Clear(_gridSnapshots, _gridCount, previousCount - _gridCount);
    }

    private void BuildSourceTree()
    {
        var state = false;
        _sourceTree.Clear(ref state, static (ref bool clearState, in int sourceIndex) => { });

        for (var i = 0; i < _sourceCount; i++)
        {
            ref readonly var source = ref _sourceSnapshots[i];
            var size = new Vector2(source.MaxRange * 2f, source.MaxRange * 2f);
            var aabb = Box2.CenteredAround(source.WorldPosition, size);
            _sourceTree.CreateProxy(in aabb, uint.MaxValue, i);
        }
    }

    private const float MaxSanePosition = PhysicsConstants.Huge - 100f;

    private static bool IsSanePosition(Vector2 value)
    {
        return MathF.Abs(value.X) < MaxSanePosition &&
               MathF.Abs(value.Y) < MaxSanePosition;
    }

    private static bool IsFinite(Matrix3x2 matrix)
    {
        return float.IsFinite(matrix.M11) &&
               float.IsFinite(matrix.M12) &&
               float.IsFinite(matrix.M21) &&
               float.IsFinite(matrix.M22) &&
               float.IsFinite(matrix.M31) &&
               float.IsFinite(matrix.M32);
    }

    private static bool IsSaneBox(Box2 box)
    {
        return MathF.Abs(box.Left) < PhysicsConstants.Huge &&
               MathF.Abs(box.Bottom) < PhysicsConstants.Huge &&
               MathF.Abs(box.Right) < PhysicsConstants.Huge &&
               MathF.Abs(box.Top) < PhysicsConstants.Huge;
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
