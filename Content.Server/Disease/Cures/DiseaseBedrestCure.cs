using Content.Shared.Disease;
using Content.Server.Buckle.Components;
using Content.Server.Bed.Components;

namespace Content.Server.Disease.Cures
{
    /// <summary>
    /// Cures the disease after a certain amount of time
    /// strapped.
    /// </summary>
    public sealed class DiseaseBedrestCure : DiseaseCure
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public int Ticker = 0;
        [DataField("maxLength", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxLength = 60;

        public override bool Cure(DiseaseEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent<BuckleComponent>(args.DiseasedEntity, out var buckle) ||
                !args.EntityManager.HasComponent<HealOnBuckleComponent>(buckle.BuckledTo?.Owner))
                return false;
            if (buckle.Buckled)
                Ticker++;
            return Ticker >= MaxLength;
        }

        public override string CureText()
        {
            return (Loc.GetString("diagnoser-cure-bedrest", ("time", MaxLength)));
        }
    }
}
