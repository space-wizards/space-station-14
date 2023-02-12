using System.Linq;
using Content.Shared.Chemistry.Reagent;
using Content.Server.Disease;
using Content.Shared.Disease;
using Content.Shared.Disease.Components;
using Content.Shared.Localizations;
using Robust.Shared.Random;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

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

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            var diseaseNames =
                Diseases
                .Select(x => prototype.Index<DiseasePrototype>(x).Name)
                .ToList();

            return Loc.GetString("reagent-effect-guidebook-chem-cause-random-disease",
                ("chance", Probability),
                ("diseases", ContentLocalizationManager.FormatList(diseaseNames)));
        }

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent<DiseasedComponent>(args.SolutionEntity, out var diseased))
                return;

            if (args.Scale != 1f)
                return;

            var random = IoCManager.Resolve<IRobustRandom>();

            EntitySystem.Get<DiseaseSystem>().TryAddDisease(args.SolutionEntity, random.Pick(Diseases));
        }
    }
}
