using Content.Shared.Chat.Prototypes;
using Content.Shared.Disease;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
        /// Emote to play when snoughing
        /// </summary>
        [DataField("emote", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EmotePrototype>))]
        public string EmoteId = String.Empty;

        /// <summary>
        /// Whether to spread the disease through the air
        /// </summary>
        [DataField("airTransmit")]
        public bool AirTransmit = true;

        public override void Effect(DiseaseEffectArgs args)
        {
            EntitySystem.Get<DiseaseSystem>().SneezeCough(args.DiseasedEntity, args.Disease, EmoteId, AirTransmit);
        }
    }
}
