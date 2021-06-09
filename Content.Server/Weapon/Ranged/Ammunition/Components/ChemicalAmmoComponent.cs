using System.Collections.Generic;
using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Robust.Shared.GameObjects;

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
            if (!Owner.TryGetComponent<SolutionContainerComponent>(out var ammoSolutionContainer))
                return;

            var projectiles = barrelFired.FiredProjectiles;

            var projectileSolutionContainers = new List<SolutionContainerComponent>();
            foreach (var projectile in projectiles)
            {
                if (projectile.TryGetComponent<SolutionContainerComponent>(out var projectileSolutionContainer))
                {
                    projectileSolutionContainers.Add(projectileSolutionContainer);
                }
            }

            if (!projectileSolutionContainers.Any())
                return;

            var solutionPerProjectile = ammoSolutionContainer.CurrentVolume * (1 / projectileSolutionContainers.Count);

            foreach (var projectileSolutionContainer in projectileSolutionContainers)
            {
                var solutionToTransfer = ammoSolutionContainer.SplitSolution(solutionPerProjectile);
                projectileSolutionContainer.TryAddSolution(solutionToTransfer);
            }

            ammoSolutionContainer.RemoveAllSolution();
        }
    }
}
