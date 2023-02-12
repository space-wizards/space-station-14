using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Content.Server.Atmos.Miasma;
using Content.Server.Disease;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// The miasma system rotates between 1 disease at a time.
    /// This gives all entities the disease the miasme system is currently on.
    /// For things ingested by one person, you probably want ChemCauseRandomDisease instead.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMiasmaPoolSource : ReagentEffect
    {
        // JUSTIFICATION: Only used for miasma.
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => null;

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Scale != 1f)
                return;

            string disease = EntitySystem.Get<MiasmaSystem>().RequestPoolDisease();

            EntitySystem.Get<DiseaseSystem>().TryAddDisease(args.SolutionEntity, disease);
        }
    }
}
