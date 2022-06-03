using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Holosign
{
    [RegisterComponent]
    public sealed class HolosignProjectorComponent : Component
    {
        [ViewVariables]
        [DataField("maxCharges")]
        public int MaxCharges = 6;

        [ViewVariables(VVAccess.ReadWrite), DataField("charges")]
        public int CurrentCharges = 6;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("signProto", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string SignProto = "HolosignWetFloor";

        /// <summary>
        /// When the holosign was last used.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("lastUse")]
        public TimeSpan LastUsed = TimeSpan.Zero;

        /// <summary>
        /// How long it takes for 1 charge to accumulate.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rechargeTime")]
        public TimeSpan RechargeTime = TimeSpan.FromSeconds(30);
    }
}
