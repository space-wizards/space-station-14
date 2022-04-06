using Content.Server.Disease.Components;
using JetBrains.Annotations;
using Content.Shared.Disease;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Handles a disease which incubates over a period of time
    /// before adding another component to the infected entity
    /// currently used for zombie virus
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

        /// <summary>
        /// The current amount of progression that has built up.
        /// </summary>
        [DataField("progression")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Progression = 0.00f;

        public override void Effect(DiseaseEffectArgs args)
        {
            if ( Progression < 1) //increases steadily until 100%
            {
                Progression += Rate;
                
            }
            else //adds the component for the later stage of the disease.
            {
                args.EntityManager.EnsureComponent<DiseaseZombieComponent>(args.DiseasedEntity); //TODO: needs to be generalized.
            }
        }
    }
}
