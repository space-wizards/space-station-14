using Content.Server.Disease.Components;
using JetBrains.Annotations;
using Content.Shared.Disease;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Handles any disease with stages
    /// You are gonna need this if you want the stage to work
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseProgression : DiseaseEffect
    {
        /// <summary>
        /// How much to increase the rate by each time this effect happens. 0.01 is 1% progression, 0.1 is 10%, and so on
        /// Use the probability field on the effect to make this more random
        /// </summary>
        [DataField("rate")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Rate = 0.01f;

        public override void Effect(DiseaseEffectArgs args)
        {
            args.Disease.DiseaseSeverity += Rate;
        }
    }
}
