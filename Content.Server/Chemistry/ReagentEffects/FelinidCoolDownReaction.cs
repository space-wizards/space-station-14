using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Ganimed.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed partial class FelinidCoolDownReaction : ReagentEffect
    {
		protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-extinguish-reaction", ("chance", Probability));

        public override void Effect(ReagentEffectArgs args)
        {	
			var doAfterSystem = EntitySystem.Get<SharedDoAfterSystem>();
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out FelinidComponent? felinid)) return;
			if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out DoAfterComponent? comp)) return;
			doAfterSystem.CancelAll(comp);
        }
    }
}
