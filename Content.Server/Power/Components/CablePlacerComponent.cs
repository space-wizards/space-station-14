using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Power;
using Content.Shared.Whitelist;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed partial class CablePlacerComponent : Component
    {
        /// <summary>
        /// The structure prototype for the cable coil to place.
        /// </summary>
        [DataField("cablePrototypeID", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? CablePrototypeId = "CableHV";

        /// <summary>
        /// What kind of wire prevents placing this wire over it as CableType.
        /// </summary>
        [DataField]
        public CableType BlockingCableType = CableType.HighVoltage;

        /// <summary>
        /// Block placement over entities by blacklist instead of CableType?
        /// </summary>
        [DataField]
        public bool UsePlaceOverBlacklist = false;

        [DataField]
        public EntityWhitelist Blacklist { get; private set; } = new();

        /// <summary>
        /// Whether the placed cable should go over tiles or not.
        /// </summary>
        [DataField]
        public bool OverTile = false;
    }
}
