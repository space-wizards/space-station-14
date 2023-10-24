using System.Linq;
using System.Numerics;
using Content.Server.Maps;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Effects.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class TileAnomalySystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TileSpawnAnomalyComponent, AnomalyStabilityChangedEvent>(OnSeverityChanged);
    }

    private void OnSeverityChanged(EntityUid uid, TileSpawnAnomalyComponent component, ref AnomalyStabilityChangedEvent args)
    {
        var xform = Transform(uid);
        if (!_map.TryGetGrid(xform.GridUid, out var grid))
            return;

        var radius = component.SpawnRange * args.Stability;
        var fleshTile = (ContentTileDefinition) _tiledef[component.FloorTileId];
        var localpos = xform.Coordinates.Position;
        var tilerefs = grid.GetLocalTilesIntersecting(
            new Box2(localpos + new Vector2(-radius, -radius), localpos + new Vector2(radius, radius)));
        foreach (var tileref in tilerefs)
        {
            if (!_random.Prob(component.SpawnChance))
                continue;
            _tile.ReplaceTile(tileref, fleshTile);
        }
    }
}
