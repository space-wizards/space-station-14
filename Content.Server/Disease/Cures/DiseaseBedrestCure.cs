using Content.Shared.Disease;
using Content.Server.Buckle.Components;

namespace Content.Server.Disease.Cures
{
    /// Lie down for a time to cure this one
    /// TODO: Revisit after bed pr merged
    public sealed class DiseaseBedrestCure : DiseaseCure
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public int Ticker = 0;
        [DataField("maxLength", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxLength = 60;

        public override bool Cure(DiseaseEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent<BuckleComponent>(args.DiseasedEntity, out var buckle))
                return false;
            if (buckle.Buckled)
            {
                Ticker++;            }
            if (Ticker >= MaxLength)
                return true;
            return false;
        }
    }
}
