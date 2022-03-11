using Content.Shared.Disease;
using Content.Shared.FixedPoint;
using Content.Server.Body.Components;

namespace Content.Server.Disease.Cures
{
    public sealed class DiseaseReagentCure : DiseaseCure
    {
        [DataField("reagent")]
        public string? Reagent;

        public override bool Cure(DiseaseEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.DiseasedEntity, out var bloodstream))
                return false;

            if (Reagent != null && bloodstream.ChemicalSolution.ContainsReagent(Reagent))
                return true;

            return false;
        }
    }
}
