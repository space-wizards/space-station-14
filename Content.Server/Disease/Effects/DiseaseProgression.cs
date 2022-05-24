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
        /// The rate that's increased over time. Defaults to 1% so the probability can be varied in yaml
        /// </summary>
        [DataField("rate")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Rate = 0.01f;

        public override void Effect(DiseaseEffectArgs args)
        {
            args.Disease.DiseaseSeverity = +Rate;
        }
    }
}
