using Content.Server.Weapons.Ranged.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Chemistry.EntitySystems;
using System.Linq;

namespace Content.Server.Weapons.Ranged.Systems
{
    public sealed class ChemicalAmmoSystem : EntitySystem
    {
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<ChemicalAmmoComponent, AmmoShotEvent>(OnFire);
        }

        private void OnFire(Entity<ChemicalAmmoComponent> entity, ref AmmoShotEvent args)
        {
            if (!_solutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out var ammoSoln, out var ammoSolution))
                return;

            var projectiles = args.FiredProjectiles;

            var projectileSolutionContainers = new List<(EntityUid, Entity<SolutionComponent>)>();
            foreach (var projectile in projectiles)
            {
                if (_solutionContainerSystem
                    .TryGetSolution(projectile, entity.Comp.SolutionName, out var projectileSoln, out _))
                {
                    projectileSolutionContainers.Add((projectile, projectileSoln.Value));
                }
            }

            if (!projectileSolutionContainers.Any())
                return;

            var solutionPerProjectile = ammoSolution.Volume * (1 / projectileSolutionContainers.Count);

            foreach (var (_, projectileSolution) in projectileSolutionContainers)
            {
                var solutionToTransfer = _solutionContainerSystem.SplitSolution(ammoSoln.Value, solutionPerProjectile);
                _solutionContainerSystem.TryAddSolution(projectileSolution, solutionToTransfer);
            }

            _solutionContainerSystem.RemoveAllSolution(ammoSoln.Value);
        }
    }
}
