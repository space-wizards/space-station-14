using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Weapons.Ranged.Components;
using Content.Shared.Chemistry.Solutions;
using Content.Shared.Chemistry.Solutions.Components;
using Content.Shared.Chemistry.Solutions.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;
using System.Linq;

namespace Content.Server.Weapons.Ranged.Systems
{
    public sealed class ChemicalAmmoSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SolutionSystem _solutionSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<ChemicalAmmoComponent, AmmoShotEvent>(OnFire);
        }

        private void OnFire(EntityUid uid, ChemicalAmmoComponent component, AmmoShotEvent args)
        {
            if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var ammoSoln, out var ammoSolution))
                return;

            var projectiles = args.FiredProjectiles;

            var projectileSolutionContainers = new List<(EntityUid, Entity<SolutionComponent>)>();
            foreach (var projectile in projectiles)
            {
                if (_solutionContainerSystem
                    .TryGetSolution(projectile, component.SolutionName, out var projectileSoln, out _))
                {
                    projectileSolutionContainers.Add((uid, projectileSoln));
                }
            }

            if (!projectileSolutionContainers.Any())
                return;

            var solutionPerProjectile = ammoSolution.Volume * (1 / projectileSolutionContainers.Count);

            foreach (var (_, projectileSolution) in projectileSolutionContainers)
            {
                var solutionToTransfer = _solutionSystem.SplitSolution(ammoSoln, solutionPerProjectile);
                _solutionSystem.TryAddSolution(projectileSolution, solutionToTransfer);
            }

            _solutionSystem.RemoveAllSolution(ammoSoln);
        }
    }
}
