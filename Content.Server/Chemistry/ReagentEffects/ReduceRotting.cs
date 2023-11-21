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
        //How much time are we taking off the accumulator
        [DataField("rottingAmount"), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan RottingAmount = TimeSpan.FromSeconds(5);


        //Main Plan:
        //Add method to the rotting system that enables it to be reduced
        //Use Entity manager to get the system "RottingSystem" and store it to the variable rottingSys.
        //All  I'll then need to do is put: rottingSys.reduceRotting(arguments) - putting the system in here will feel like hard coding
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-chem-vomit", ("chance", Probability));
        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Scale != 1f)
                return;

            var rottingSys = args.EntityManager.EntitySysManager.GetEntitySystem<RottingSystem>();

            rottingSys.ReduceRotting(args.SolutionEntity, RottingAmount);
        }
    }
}
