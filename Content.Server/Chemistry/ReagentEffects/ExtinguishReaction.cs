using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed partial class ExtinguishReaction : ReagentEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-extinguish-reaction", ("chance", Probability));

        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out FlammableComponent? flammable)) return;

            var flammableSystem = EntitySystem.Get<FlammableSystem>();
            flammableSystem.Extinguish(args.SolutionEntity, flammable);
            flammableSystem.AdjustFireStacks(args.SolutionEntity, -1.5f * (float) args.Quantity, flammable);
        }
    }
}
