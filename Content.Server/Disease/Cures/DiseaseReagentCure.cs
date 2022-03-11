using Content.Shared.Disease;
using Content.Shared.FixedPoint;
using Content.Server.Body.Components;

namespace Content.Server.Disease.Cures
{
    public sealed class DiseaseReagentCure : DiseaseCure
    {
        [DataField("min")]
        public FixedPoint2 Min = 1;
        [DataField("reagent")]
        public string? Reagent;

        public override bool Cure(DiseaseEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent<BloodstreamComponent>(args.DiseasedEntity, out var bloodstream))
                return false;

            var quant = FixedPoint2.Zero;
            if (Reagent != null && bloodstream.ChemicalSolution.ContainsReagent(Reagent))
            {
                quant = bloodstream.ChemicalSolution.GetReagentQuantity(Reagent);
            }

            return quant >= Min;
        }
    }
}
