using Content.Server.Worldgen.Prototypes;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Worldgen.Systems.Planes;

public sealed class DebrisPlaneSystem : WorldChunkPlaneSystem<DebrisChunkData, DebrisPlaneConfig>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DebrisGeneratorSystem _debrisGeneratorSystem = default!;

    public override Matrix3 CoordinateTransformMatrix => Matrix3.CreateScale(ChunkSize, ChunkSize);
    public override int ChunkSize => 128;

    protected override DebrisChunkData InitializeChunk(MapId map, Vector2i chunk)
    {
        var data = new DebrisChunkData();
        var points = GeneratePoissonDiskPointsInChunk(80.0f/ChunkSize, chunk);
        var weights = _prototypeManager.Index<WeightedRandomPrototype>(MapConfigurations[map].DebrisChoices);
        foreach (var point in points)
        {
            data.Debris.Add(new DebrisData(null, _prototypeManager.Index<DebrisPrototype>(weights.Pick(_random)), new MapCoordinates(point, map)));
        }
        return data;
    }

    protected override void LoadChunk(MapId map, Vector2i chunk)
    {
        var data = GetChunk(map, chunk);

        for (var i = 0; i < data.Debris.Count; i++)
        {
            var debris = data.Debris[i];

            _mapManager.FindGridsIntersectingEnumerator(map, new Box2(debris.Coordinates.Position - debris.Prototype.ExclusionZone, debris.Coordinates.Position + debris.Prototype.ExclusionZone), out var enumerator, true);

            if (enumerator.MoveNext(out _))
            {
                data.Debris.RemoveAt(i); // We're blocked, self-annihilate.
                continue;
            }

            _debrisGeneratorSystem.TryGenerateDebris(debris.Coordinates, debris.Prototype!, out var ent);
            debris.Grid = ent;
        }
    }

    protected override void UnloadChunk(MapId map, Vector2i chunk)
    {

    }

    public override bool TryClearWorldSpace(Box2Rotated area)
    {
        throw new NotImplementedException();
    }

    public override bool TryClearWorldSpace(Circle area)
    {
        throw new NotImplementedException();
    }
}

[Access(typeof(DebrisPlaneSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed class DebrisChunkData
{
    public List<DebrisData> Debris = new();
}

[Access(typeof(DebrisPlaneSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed class DebrisData
{
    public EntityUid? Grid;
    public DebrisPrototype Prototype;
    public MapCoordinates Coordinates;

    public DebrisData(EntityUid? grid, DebrisPrototype prototype, MapCoordinates coordinates)
    {
        Grid = grid;
        Prototype = prototype;
        Coordinates = coordinates;
    }
}

[DataDefinition]
public sealed class DebrisPlaneConfig
{
    [DataField("debrisChoices", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string DebrisChoices = default!;
}
