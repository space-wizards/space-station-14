using System.Collections.Generic;
using System.Linq;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Weapon.Ranged.Ammunition.Components
{
    [RegisterComponent]
    public class ChemicalAmmoComponent : Component
    {
        public override string Name => "ChemicalAmmo";
        public const string DefaultSolutionName = "ammo";

        [DataField("solution")]
        public string SolutionName { get; set; } = DefaultSolutionName;

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
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var ammoSolution))
                return;

            var projectiles = barrelFired.FiredProjectiles;
            var solutionContainerSystem = EntitySystem.Get<SolutionContainerSystem>();

            var projectileSolutionContainers = new List<(EntityUid, Solution)>();
            foreach (var projectile in projectiles)
            {
                if (EntitySystem.Get<SolutionContainerSystem>()
                    .TryGetSolution(projectile, SolutionName, out var projectileSolutionContainer))
                {
                    projectileSolutionContainers.Add((projectile.Uid, projectileSolutionContainer));
                }
            }

            if (!projectileSolutionContainers.Any())
                return;

            var solutionPerProjectile = ammoSolution.CurrentVolume * (1 / projectileSolutionContainers.Count);

            foreach (var (projectileUid, projectileSolution) in projectileSolutionContainers)
            {
                var solutionToTransfer = solutionContainerSystem.SplitSolution(Owner.Uid, ammoSolution, solutionPerProjectile);
                solutionContainerSystem.TryAddSolution(projectileUid, projectileSolution, solutionToTransfer);
            }

            solutionContainerSystem.RemoveAllSolution(Owner.Uid, ammoSolution);
        }
    }
}
