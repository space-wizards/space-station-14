using Content.Server.Anomaly.Effects.Components;
using Content.Server.Spreader;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class SpreaderAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyControlledSpreaderComponent, SpreadNeighborsEvent>(OnSpread);
        SubscribeLocalEvent<SpreadGroupUpdateRate>(OnUpdateRate);
    }
    private void OnSpread(EntityUid uid, AnomalyControlledSpreaderComponent component, ref SpreadNeighborsEvent args)
    {
        if (args.NeighborFreeTiles.Count == 0 || args.Updates <= 0)
        {
            return;
        }

        var prototype = MetaData(uid).EntityPrototype?.ID;

        if (prototype == null)
        {
            RemCompDeferred<EdgeSpreaderComponent>(uid);
            return;
        }

        if (!IsWithinSpreadRange(uid))
            return;

        //todo: get this on the component somehow
        if (!_random.Prob(0.3f))
            return;

        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var neighborUid = Spawn(prototype, neighbor.Grid.GridTileToLocal(neighbor.Tile));
            EnsureComp<EdgeSpreaderComponent>(neighborUid);
            args.Updates--;

            if (args.Updates <= 0)
                return;
        }
    }

    private void OnUpdateRate(ref SpreadGroupUpdateRate ev)
    {
        var emo = true;
        foreach (var (spreader, anomaly) in EntityQuery<SpreaderAnomalyComponent, AnomalyComponent>())
        {
            if (spreader.Group != ev.Name)
                continue;

            if (emo)
                ev.UpdatesPerSecond = 0;
            emo = false;

            var amount = (int) MathF.Round(MathHelper.Lerp(spreader.MinUpdatesPerSecond, spreader.MaxUpdatesPerSecond, anomaly.Severity));
            ev.UpdatesPerSecond += amount;
        }
    }

    private bool IsWithinSpreadRange(EntityUid uid)
    {
        var xform = Transform(uid);
        var query = EntityQueryEnumerator<SpreaderAnomalyComponent, AnomalyComponent, TransformComponent>();
        while (query.MoveNext(out _, out var spreader, out var anom, out var anomXform))
        {
            var range = MathHelper.Lerp(spreader.MinSpreadRadius, spreader.MaxSpreadRadius, anom.Severity);
            if ((_transform.GetWorldPosition(xform) - _transform.GetWorldPosition(anomXform)).Length() < range)
                return true;
        }

        return false;
    }

}
