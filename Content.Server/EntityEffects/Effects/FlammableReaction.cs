using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class FlammableReaction : EntityEffect
    {
        [DataField]
        public float Multiplier = 0.05f;

        [DataField]
        public float MultiplierOnExisting = 1f;

        public override bool ShouldLog => true;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-flammable-reaction", ("chance", Probability));

        public override LogImpact LogImpact => LogImpact.Medium;

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.TargetEntity, out FlammableComponent? flammable))
                return;

            var multiplier = flammable.FireStacks == 0f ? Multiplier : MultiplierOnExisting;
            var quantity = 1f;
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                quantity = reagentArgs.Quantity.Float();
                reagentArgs.EntityManager.System<FlammableSystem>().AdjustFireStacks(args.TargetEntity, quantity * multiplier, flammable);
                if (reagentArgs.Reagent != null)
                    reagentArgs.Source?.RemoveReagent(reagentArgs.Reagent.ID, reagentArgs.Quantity);
            } else
            {
                args.EntityManager.System<FlammableSystem>().AdjustFireStacks(args.TargetEntity, multiplier, flammable);
            }
        }
    }
}
