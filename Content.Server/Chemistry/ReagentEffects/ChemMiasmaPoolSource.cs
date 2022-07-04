using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Content.Server.Atmos.Miasma;
using Content.Server.Disease;

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
        public override void Effect(ReagentEffectArgs args)
        {
            string disease = EntitySystem.Get<MiasmaSystem>().RequestPoolDisease();

            EntitySystem.Get<DiseaseSystem>().TryAddDisease(args.SolutionEntity, disease);
        }
    }
}
