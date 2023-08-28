using Content.Shared.Chemistry.Reagent;
using Content.Server.Medical;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Forces you to vomit.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ChemVomit : ReagentEffect
    {
        /// How many units of thirst to add each time we vomit
        [DataField("thirstAmount")]
        public float ThirstAmount = -40f;
        /// How many units of hunger to add each time we vomit
        [DataField("hungerAmount")]
        public float HungerAmount = -40f;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-chem-vomit", ("chance", Probability));

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Scale != 1f)
                return;

            var vomitSys = args.EntityManager.EntitySysManager.GetEntitySystem<VomitSystem>();

            vomitSys.Vomit(args.SolutionEntity, ThirstAmount, HungerAmount);
        }
    }
}
