using JetBrains.Annotations;

namespace Content.Shared.Disease
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class DiseaseCure
    {
        /// <summary>
        /// This returns true if the disease should be cured
        /// and false otherwise
        /// </summary>
        public abstract bool Cure(DiseaseEffectArgs args);

        /// <summary>
        /// This is used by the disease diangoser machine
        /// to generate reports to tell people all of a disease's
        /// special cures using in-game methods.
        /// So it should return a localization string describing
        /// the cure
        /// </summary>
        public abstract string CureText();
    }
}
