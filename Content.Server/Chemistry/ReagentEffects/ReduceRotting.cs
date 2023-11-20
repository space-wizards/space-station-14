using Content.Shared.Chemistry.Reagent;
using Content.Server.Medical;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.Atmos.Miasma;
using Content.Server.Atmos.Miasma;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Rejuvinates the patient, reducing the rotting effect on them.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ReduceRotting : ReagentEffect
        //Just a place holder at the moment until I figure out how to do this.
    {
        [DataField("rottingAmount")]
        public float RottingAmount = 1;
        /// How many units of thirst to add each time we vomit
        [DataField("thirstAmount")]
        public float ThirstAmount = -8f;
        /// How many units of hunger to add each time we vomit
        [DataField("hungerAmount")]
        public float HungerAmount = -8f;


        //Should probably change this when I find out how to work it.
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
}
