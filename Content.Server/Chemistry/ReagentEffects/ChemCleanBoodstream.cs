using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Content.Server.Body.Systems;

namespace Content.Server.Chemistry.ReactionEffects
{
    /// <summary>
    /// Basically smoke and foam reactions.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemCleanBloodstream : ReagentEffect
    {
        [DataField("cleanseRate")]
        public float CleanseRate = 3.0f;
        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Source == null)
                return;

            var cleanseRate = CleanseRate;

            cleanseRate *= args.Scale;

            var bloodstreamSys = EntitySystem.Get<BloodstreamSystem>();
            bloodstreamSys.FlushChemicals(args.SolutionEntity, args.Reagent.ID, cleanseRate);
        }
    }
}
