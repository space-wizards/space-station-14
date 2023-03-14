using Content.Shared.Disease;
using JetBrains.Annotations;
using Robust.Shared.Audio;

namespace Content.Server.Disease
{
    /// <summary>
    /// Makes the diseased honk.
    /// or neither.
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseHonk : DiseaseEffect
    {
        /// <summary>
        /// Message to play when honking.
        /// </summary>
        [DataField("honkMessage")]
        public string HonkMessage = "disease-honk";

        /// <summary>
        /// Sound to play when honking.
        /// </summary>
        [DataField("honkSound")]
        public SoundSpecifier? HonkSound;

        /// <summary>
        /// Whether to spread the disease through the air.
        /// </summary>
        [DataField("airTransmit")]
        public bool AirTransmit = false;

        public override void Effect(DiseaseEffectArgs args)
        {
            EntitySystem.Get<DiseaseSystem>().SneezeCough(args.DiseasedEntity, args.Disease, HonkMessage, HonkSound, AirTransmit);
        }
    }
}
