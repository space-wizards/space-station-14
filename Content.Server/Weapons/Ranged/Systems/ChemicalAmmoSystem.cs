using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Weapons.Ranged.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.Weapons.Ranged.Systems
{
    public sealed class ChemicalAmmoSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<ChemicalAmmoComponent, AmmoShotEvent>(OnFire);
        }

        private void OnFire(EntityUid uid, ChemicalAmmoComponent component, AmmoShotEvent args)
        {
            if (!_solutionSystem.TryGetSolution(uid, component.SolutionName, out var ammoSolution))
                return;

            var projectiles = args.FiredProjectiles;

            var projectileSolutionContainers = new List<(EntityUid, Solution)>();
            foreach (var projectile in projectiles)
            {
                if (_solutionSystem
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
