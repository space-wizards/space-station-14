using Content.Shared.Disease;

namespace Content.Server.Disease.Cures
{
    /// <summary>
    /// Automatically removes the disease after a
    /// certain amount of time.
    /// </summary>
    public sealed class JustWait : DiseaseCure
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public int Ticker = 0;
        [DataField("maxLength", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxLength = 150;

        public override bool Cure(DiseaseEffectArgs args)
        {
            if (Ticker < MaxLength)
            {
                Ticker++;
                return false;
            }
            return true;
        }

        public override string CureText()
        {
            return Loc.GetString("diagnoser-cure-wait", ("time", MaxLength));
        }
    }
}
