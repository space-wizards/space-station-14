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

        public override void Effect(EntityEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.TargetEntity, out FlammableComponent? flammable))
                return;

            var multiplier = flammable.FireStacks == 0f ? Multiplier : MultiplierOnExisting;
            args.EntityManager.System<FlammableSystem>().AdjustFireStacks(args.TargetEntity, args.Quantity.Float() * multiplier, flammable);

            if (args.Reagent != null)
                args.Source?.RemoveReagent(args.Reagent.ID, args.Quantity);
        }
    }
}
