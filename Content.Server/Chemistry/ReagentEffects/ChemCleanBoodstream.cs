/*using Content.Shared.Chemistry.Reagent;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using JetBrains.Annotations;
using Content.Server.Chemistry.EntitySystems;


namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed class ChemCleanBoodstream : ReagentEffect
    {
        [DataField("cureChance")]
        public float CureChance = 0.15f;

        public override void Effect(ReagentEffectArgs args)
        {
           // if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.SolutionEntity, out var blood))
           // {
                var sys = EntitySystem.Get<BloodstreamSystem>();
                //sys.TryModifyBleedAmount(args.SolutionEntity, 1.0f, blood);
                //sys.SpillAllSolutions(args.SolutionEntity);
                var splitSolution = EntitySystem.Get<SolutionContainerSystem>().SplitSolution(args.SolutionEntity, args.Source, args.Source.MaxVolume);
                splitSolution.RemoveSolution(splitSolution.TotalVolume * (1 - solutionFraction));
           // }
        }
    }
}
*/

using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Chemistry.ReagentEffects;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Content.Server.Body.Systems;

namespace Content.Server.Chemistry.ReactionEffects
{
    /// <summary>
    /// Basically smoke and foam reactions.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemCleanBoodstream : ReagentEffect
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Source == null)
                return;

            var solutionSys = EntitySystem.Get<SolutionContainerSystem>();

         
            //solutionSys.FlushlSolution(args.SolutionEntity,args.Reagent.ID, 3.0f);

            var bloodstreamSys = EntitySystem.Get<BloodstreamSystem>();
            bloodstreamSys.FlushChemicals(args.SolutionEntity, args.Reagent.ID, 3.0f);

            //var splitSolution = EntitySystem.Get<SolutionContainerSystem>().SplitSolution(args.SolutionEntity, args.Source, args.Source.MaxVolume);
        }
    }
}
