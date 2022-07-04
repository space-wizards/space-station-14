using System.Linq;
using Robust.Shared.Random;
using Content.Server.Body.Systems;
using Content.Server.Disease.Components;
using Content.Server.Drone.Components;
using Content.Server.Weapon.Melee;
using Content.Shared.Chemistry.Components;
using Content.Shared.MobState.Components;
using Content.Server.Disease;

namespace Content.Server.Zombies
{
    public sealed class ZombieSystem : EntitySystem
    {
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly ZombifyOnDeathSystem _zombify = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ZombieComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, ZombieComponent component, MeleeHitEvent args)
        {
            if (!EntityManager.TryGetComponent<ZombieComponent>(args.User, out var zombieComp))
                return;

            if (!args.HitEntities.Any())
                return;

            foreach (EntityUid entity in args.HitEntities)
            {
                if (args.User == entity)
                    continue;

                if (!TryComp<MobStateComponent>(entity, out var mobState) || HasComp<DroneComponent>(entity))
                    continue;

                if (_robustRandom.Prob(0.5f) && HasComp<DiseaseCarrierComponent>(entity))
                    _disease.TryAddDisease(entity, "ActiveZombieVirus");

                if (HasComp<ZombieComponent>(entity))
                    args.BonusDamage = args.BaseDamage * zombieComp.OtherZombieDamageCoefficient;

                if ((mobState.IsDead() || mobState.IsCritical())
                    && !HasComp<ZombieComponent>(entity))
                {
                    _zombify.ZombifyEntity(entity);
                    args.BonusDamage = -args.BaseDamage;
                }
                else if (mobState.IsAlive()) //heals when zombies bite live entities
                {
                    var healingSolution = new Solution();
                    healingSolution.AddReagent("Bicaridine", 1.00); //if OP, reduce/change chem
                    _bloodstream.TryAddToChemicals(args.User, healingSolution);
                }
            }
        }
    }
}
