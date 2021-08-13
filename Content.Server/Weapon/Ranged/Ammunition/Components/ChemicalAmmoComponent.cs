using System.Collections.Generic;
using System.Linq;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Solution;
using Content.Shared.Chemistry.Solution.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Weapon.Ranged.Ammunition.Components
{
    [RegisterComponent]
    public class ChemicalAmmoComponent : Component
    {
        public override string Name => "ChemicalAmmo";

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case BarrelFiredMessage barrelFired:
                    TransferSolution(barrelFired);
                    break;
            }
        }

        private void TransferSolution(BarrelFiredMessage barrelFired)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, "ammo", out var ammoSolution))
                return;

            var projectiles = barrelFired.FiredProjectiles;
            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();

            var projectileSolutionContainers = new List<Solution>();
            foreach (var projectile in projectiles)
            {
                // if (projectile.TryGetComponent<SolutionHolder>(out var projectileSolutionContainer))
                if (EntitySystem.Get<SolutionContainerSystem>()
                    .TryGetDefaultSolution(Owner, out var projectileSolutionContainer))
                {
                    projectileSolutionContainers.Add(projectileSolutionContainer);
                }
            }

            if (!projectileSolutionContainers.Any())
                return;

            var solutionPerProjectile = ammoSolution.CurrentVolume * (1 / projectileSolutionContainers.Count);

            foreach (var projectileSolutionContainer in projectileSolutionContainers)
            {
                var solutionToTransfer = solutionContainerSystem.SplitSolution(ammoSolution, solutionPerProjectile);
                solutionContainerSystem.TryAddSolution(projectileSolutionContainer, solutionToTransfer);
            }

            solutionContainerSystem.RemoveAllSolution(ammoSolution);
        }
    }
}
