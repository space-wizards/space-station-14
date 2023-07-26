using System.Linq;
using Content.Shared.Blob;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Robust.Shared.Map;

namespace Content.Server.Blob
{
    public sealed class BlobbernautSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BlobbernautComponent, MobStateChangedEvent>(OnMobStateChanged);

        }

        private void OnMobStateChanged(EntityUid uid, BlobbernautComponent component, MobStateChangedEvent args)
        {
            component.IsDead = args.NewMobState switch
            {
                MobState.Dead => true,
                MobState.Alive => false,
                _ => component.IsDead
            };
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var blobFactoryQuery = EntityQueryEnumerator<BlobbernautComponent>();
            while (blobFactoryQuery.MoveNext(out var ent, out var comp))
            {
                if (comp.IsDead)
                    return;

                comp.Accumulator += frameTime;

                if (comp.Accumulator <= comp.DamageFrequency)
                    continue;
                comp.Accumulator = 0;

                if (comp.Factory == null)
                {
                    _damageableSystem.TryChangeDamage(ent, comp.Damage);
                }
                else
                {
                    var xform = Transform(ent);

                    if (!_map.TryGetGrid(xform.GridUid, out var grid))
                    {
                        return;
                    }

                    var localPos = xform.Coordinates.Position;
                    var centerTile = grid.GetLocalTilesIntersecting(
                        new Box2(localPos, localPos)).ToArray();

                    foreach (var tileRef in centerTile)
                    {
                        foreach (var entOnTile in grid.GetAnchoredEntities(tileRef.GridIndices))
                        {
                            if (HasComp<BlobTileComponent>(entOnTile))
                                return;
                        }
                    }

                    _damageableSystem.TryChangeDamage(ent, comp.Damage);
                }
            }
        }
    }
}
