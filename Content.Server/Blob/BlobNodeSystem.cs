using System.Linq;
using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Blob;

public sealed class BlobNodeSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobNodeComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, BlobNodeComponent component, ComponentStartup args)
    {

    }

    private void Pulse(EntityUid uid, BlobNodeComponent component)
    {
        var xform = Transform(uid);

        var radius = component.PulseRadius;

        var localPos = xform.Coordinates.Position;

        if (!_map.TryGetGrid(xform.GridUid, out var grid))
        {
            return;
        }

        if (!TryComp<BlobTileComponent>(uid, out var blobTileComponent) || blobTileComponent.Core == null)
            return;

        var innerTiles = grid.GetLocalTilesIntersecting(
            new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius)), false).ToArray();

        _random.Shuffle(innerTiles);

        var explain = true;
        foreach (var tileRef in innerTiles)
        {
            foreach (var ent in grid.GetAnchoredEntities(tileRef.GridIndices))
            {
                if (!HasComp<BlobTileComponent>(ent))
                    continue;

                var ev = new BlobTileGetPulseEvent
                {
                    Explain = explain
                };
                RaiseLocalEvent(ent, ev);
                explain = false;
            }
        }

        foreach (var lookupUid in _lookup.GetEntitiesInRange(xform.Coordinates, radius))
        {
            if (!HasComp<BlobMobComponent>(lookupUid))
                continue;
            var ev = new BlobMobGetPulseEvent();
            RaiseLocalEvent(lookupUid, ev);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var blobFactoryQuery = EntityQueryEnumerator<BlobNodeComponent>();
        while (blobFactoryQuery.MoveNext(out var ent, out var comp))
        {
            if (_gameTiming.CurTime < comp.NextPulse)
                return;

            if (TryComp<BlobTileComponent>(ent, out var blobTileComponent) && blobTileComponent.Core != null)
            {
                Pulse(ent, comp);
            }

            comp.NextPulse = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.PulseFrequency);
        }
    }
}
