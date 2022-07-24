using Content.Shared.Chemistry.Reagent;
using Content.Server.Disease;
using Content.Shared.Disease.Components;
using Robust.Shared.Random;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Causes a random disease from a list, if the user is not already diseased.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemCauseRandomDisease : ReagentEffect
    {
        /// <summary>
        /// A disease to choose from.
        /// </summary>
        [DataField("diseases", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public List<string> Diseases = default!;

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent<DiseasedComponent>(args.SolutionEntity, out var diseased))
                return;

            var random = IoCManager.Resolve<IRobustRandom>();

            EntitySystem.Get<DiseaseSystem>().TryAddDisease(args.SolutionEntity, random.Pick(Diseases));
        }
    }
}
