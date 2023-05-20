using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed class FlammableReaction : ReagentEffect
    {
        [DataField("multiplier")]
        public float Multiplier = 0.05f;

        public override bool ShouldLog => true;
        public override LogImpact LogImpact => LogImpact.Medium;

        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out FlammableComponent? flammable))
                return;

            args.EntityManager.System<FlammableSystem>().AdjustFireStacks(args.SolutionEntity, args.Quantity.Float() * Multiplier, flammable);

            if (args.Reagent != null)
                args.Source?.RemoveReagent(args.Reagent.ID, args.Quantity);
        }
    }
}
