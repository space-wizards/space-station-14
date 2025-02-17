using System.Linq;
using System.Numerics;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Damage;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Backmen.Blob;

public abstract class SharedBlobbernautSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    protected abstract DamageSpecifier? TryChangeDamage(string msg, EntityUid ent, DamageSpecifier dmg);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var mapGridQuery = GetEntityQuery<MapGridComponent>();
        var tileQuery = GetEntityQuery<BlobTileComponent>();
        var blobFactoryQuery = EntityQueryEnumerator<BlobbernautComponent>();
        while (blobFactoryQuery.MoveNext(out var ent, out var comp))
        {
            if (comp.IsDead)
                return;

            if (_gameTiming.CurTime < comp.NextDamage)
                return;

            if (comp.Factory == null)
            {
                TryChangeDamage("blobberaut-factory-destroy", ent, comp.Damage);
                comp.NextDamage = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.DamageFrequency);
                return;
            }

            var xform = Transform(ent);

            if (!mapGridQuery.TryGetComponent(xform.GridUid, out var grid))
            {
                return;
            }

            var radius = 1f;

            var localPos = xform.Coordinates.Position;
            var nearbyTile = _map.GetLocalTilesIntersecting(xform.GridUid.Value, grid,
                new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius))
                ).ToArray();

            foreach (var tileRef in nearbyTile)
            {
                foreach (var entOnTile in _map.GetAnchoredEntities(xform.GridUid.Value, grid, tileRef.GridIndices))
                {
                    if (tileQuery.TryGetComponent(entOnTile, out var blobTileComponent) && blobTileComponent.Core != null)
                        return;
                }
            }

            TryChangeDamage("blobberaut-not-on-blob-tile", ent, comp.Damage);
            comp.NextDamage = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.DamageFrequency);
        }
    }
}
