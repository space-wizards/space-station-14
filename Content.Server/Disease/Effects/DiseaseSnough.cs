using Content.Shared.Disease;
using JetBrains.Annotations;
using Robust.Shared.Audio;

namespace Content.Server.Disease
{
    /// <summary>
    /// Makes the diseased sneeze or cough
    /// or neither.
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseSnough : DiseaseEffect
    {
        /// <summary>
        /// Message to play when snoughing
        /// </summary>
        [DataField("snoughMessage")]
        public string SnoughMessage = "disease-sneeze";

        /// <summary>
        /// Sound to play when snoughing
        /// </summary>
        [DataField("snoughSound")]
        public SoundSpecifier? SnoughSound;

        /// <summary>
        /// Whether to spread the disease through the air
        /// </summary>
        [DataField("airTransmit")]
        public bool AirTransmit = true;

        public override void Effect(DiseaseEffectArgs args)
        {
            EntitySystem.Get<DiseaseSystem>().SneezeCough(args.DiseasedEntity, args.Disease, SnoughMessage, SnoughSound, AirTransmit);
        }
    }
}
