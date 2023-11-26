using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Weapons.Ranged.Components;
using Content.Shared.Chemistry.Solutions;
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
            if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var ammoSolution))
                return;

            var projectiles = args.FiredProjectiles;

            var projectileSolutionContainers = new List<(EntityUid, Solution)>();
            foreach (var projectile in projectiles)
            {
                if (_solutionContainerSystem
                    .TryGetSolution(projectile, component.SolutionName, out var projectileSolutionContainer))
                {
                    projectileSolutionContainers.Add((uid, projectileSolutionContainer));
                }
            }

            if (!projectileSolutionContainers.Any())
                return;

            var solutionPerProjectile = ammoSolution.Volume * (1 / projectileSolutionContainers.Count);

            foreach (var (projectileUid, projectileSolution) in projectileSolutionContainers)
            {
                var solutionToTransfer = _solutionSystem.SplitSolution(uid, ammoSolution, solutionPerProjectile);
                _solutionSystem.TryAddSolution(projectileUid, projectileSolution, solutionToTransfer);
            }

            _solutionSystem.RemoveAllSolution(uid, ammoSolution);
        }
    }
}
