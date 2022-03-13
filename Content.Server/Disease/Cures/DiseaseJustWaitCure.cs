using Content.Shared.Disease;

namespace Content.Server.Disease.Cures
{
    /// <summary>
    /// Automatically removes the disease after a
    /// certain amount of time.
    /// </summary>
    public sealed class DiseaseJustWaitCure : DiseaseCure
    {
        /// <summary>
        /// All of these are in seconds
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Ticker = 0;
        [DataField("maxLength", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxLength = 150;

        public override bool Cure(DiseaseEffectArgs args)
        {
            Ticker++;
            return Ticker >= MaxLength;
        }

        public override string CureText()
        {
            return Loc.GetString("diagnoser-cure-wait", ("time", MaxLength));
        }
    }
}
