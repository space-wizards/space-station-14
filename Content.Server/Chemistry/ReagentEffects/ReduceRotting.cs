using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Server.Atmos.Rotting;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Reduces the rotting accumulator on the patient, making them revivable.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ReduceRotting : ReagentEffect
    {
        [DataField("seconds")]
        public double RottingAmount = 10;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-reduce-rotting",
                ("chance", Probability),
                ("time", RottingAmount));
        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Scale != 1f)
                return;

            var rottingSys = args.EntityManager.EntitySysManager.GetEntitySystem<RottingSystem>();

            rottingSys.ReduceAccumulator(args.SolutionEntity, TimeSpan.FromSeconds(RottingAmount));
        }
    }
}
