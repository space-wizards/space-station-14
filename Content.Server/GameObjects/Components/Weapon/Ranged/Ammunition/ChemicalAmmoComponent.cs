using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition
{
    [RegisterComponent]
    public class ChemicalAmmoComponent : Component
    {
        public override string Name => "ChemicalAmmo";

        public override void HandleMessage(ComponentMessage message, IComponent component)
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
            if (!Owner.TryGetComponent<SolutionContainerComponent>(out var solutionContainer))
                return;

            var projectile = barrelFired.FiredProjectile;

            if (!projectile.TryGetComponent<SolutionContainerComponent>(out var projectileSolutionContainer))
                return;

            projectileSolutionContainer.TryAddSolution(solutionContainer.Solution);
        }
    }
}
