using System.Linq;
using System.Numerics;
using Content.Server.Emp;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Blob;
using Content.Shared.Damage;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Blob
{
    public sealed class BlobbernautSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _map = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly EmpSystem _empSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BlobbernautComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<BlobbernautComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<BlobbernautComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, BlobbernautComponent component, MeleeHitEvent args)
        {
            if (args.HitEntities.Count >= 1)
                return;
            if (!TryComp<BlobTileComponent>(component.Factory, out var blobTileComponent))
                return;
            if (!TryComp<BlobCoreComponent>(blobTileComponent.Core, out var blobCoreComponent))
                return;
            if (blobCoreComponent.CurrentChem == BlobChemType.ExplosiveLattice)
            {
                _explosionSystem.QueueExplosion(args.HitEntities.FirstOrDefault(), blobCoreComponent.BlobExplosive, 4, 1, 2, maxTileBreak: 0);
            }
            if (blobCoreComponent.CurrentChem == BlobChemType.ElectromagneticWeb)
            {
                var xform = Transform(args.HitEntities.FirstOrDefault());
                if (_random.Prob(0.2f))
                    _empSystem.EmpPulse(xform.MapPosition, 3f, 50f, 3f);
            }
        }

        private void OnGetState(EntityUid uid, BlobbernautComponent component, ref ComponentGetState args)
        {
            args.State = new BlobbernautComponentState()
            {
                Color = component.Color
            };
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

                if (_gameTiming.CurTime < comp.NextDamage)
                    return;

                if (comp.Factory == null)
                {
                    _popup.PopupEntity(Loc.GetString("blobberaut-factory-destroy"), ent, ent, PopupType.LargeCaution);
                    _damageableSystem.TryChangeDamage(ent, comp.Damage);
                    comp.NextDamage = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.DamageFrequency);
                    return;
                }

                var xform = Transform(ent);

                if (!_map.TryGetGrid(xform.GridUid, out var grid))
                {
                    return;
                }

                var radius = 1f;

                var localPos = xform.Coordinates.Position;
                var nearbyTile = grid.GetLocalTilesIntersecting(
                    new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius))).ToArray();

                foreach (var tileRef in nearbyTile)
                {
                    foreach (var entOnTile in grid.GetAnchoredEntities(tileRef.GridIndices))
                    {
                        if (TryComp<BlobTileComponent>(entOnTile, out var blobTileComponent) && blobTileComponent.Core != null)
                            return;
                    }
                }

                _popup.PopupEntity(Loc.GetString("blobberaut-not-on-blob-tile"), ent, ent, PopupType.LargeCaution);
                _damageableSystem.TryChangeDamage(ent, comp.Damage);
                comp.NextDamage = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.DamageFrequency);
            }
        }
    }
}
