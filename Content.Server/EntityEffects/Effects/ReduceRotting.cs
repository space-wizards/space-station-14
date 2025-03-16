using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Server.Atmos.Rotting;

namespace Content.Server.EntityEffects.Effects
{
    /// <summary>
    /// Reduces the rotting accumulator on the patient, making them revivable.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ReduceRotting : EntityEffect
    {
        [DataField("seconds")]
        public double RottingAmount = 10;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-reduce-rotting",
                ("chance", Probability),
                ("time", RottingAmount));
        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                if (reagentArgs.Scale != 1f)
                    return;
            }

            var rottingSys = args.EntityManager.EntitySysManager.GetEntitySystem<RottingSystem>();

            rottingSys.ReduceAccumulator(args.TargetEntity, TimeSpan.FromSeconds(RottingAmount));
        }
    }
}
