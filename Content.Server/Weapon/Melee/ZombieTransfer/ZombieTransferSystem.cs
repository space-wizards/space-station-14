using System.Linq;
using Robust.Shared.Random;
using Content.Server.Body.Systems;
using Content.Server.Disease.Components;
using Content.Server.Disease.Zombie.Components;
using Content.Server.Drone.Components;
using Content.Server.Weapon.Melee;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Server.Disease;
using Content.Server.Weapons.Melee.ZombieTransfer.Components;
using Content.Server.Body.Components;

namespace Content.Server.Weapons.Melee.ZombieTransfer
{
    public sealed class ZombieTransferSystem : EntitySystem
    {
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ZombieTransferComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, ZombieTransferComponent component, MeleeHitEvent args)
        {
            if (!EntityManager.TryGetComponent<DiseaseZombieComponent>(args.User, out var diseaseZombieComp))
                return;

            if (!args.HitEntities.Any())
                return;

            foreach (EntityUid entity in args.HitEntities)
            {
                if (args.User == entity)
                    continue;

                if (!HasComp<MobStateComponent>(entity) || HasComp<DroneComponent>(entity))
                    continue;

                if (_robustRandom.Prob(diseaseZombieComp.Probability) && HasComp<DiseaseCarrierComponent>(entity))
                {
                    _disease.TryAddDisease(entity, "ZombieInfection");
                }

                EntityManager.EnsureComponent<MobStateComponent>(entity, out var mobState);
                if ((mobState.IsDead() || mobState.IsCritical()) && !HasComp<DiseaseZombieComponent>(entity)) //dead entities are eautomatically infected. MAYBE: have activated infect ability?
                {
                    EntityManager.AddComponent<DiseaseZombieComponent>(entity);
                    var dspec = new DamageSpecifier();
                    //these damages match the zombie claw
                    dspec.DamageDict.TryAdd("Slash", -12);
                    dspec.DamageDict.TryAdd("Piercing", -7);
                    args.BonusDamage += dspec;
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
