using Content.Server.Power.EntitySystems;
using Content.Shared.Tools;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Allows the attached entity to be destroyed by a cutting tool, dropping a piece of cable.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(CableSystem))]
    public sealed partial class CableComponent : Component
    {
        [DataField("cableDroppedOnCutPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string CableDroppedOnCutPrototype = "CableHVStack1";

        [DataField("cuttingQuality", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string CuttingQuality = "Cutting";

        /// <summary>
        ///     Checked by <see cref="CablePlacerComponent"/> to determine if there is
        ///     already a cable of a type on a tile.
        /// </summary>
        [DataField("cableType")]
        public CableType CableType = CableType.HighVoltage;

        [DataField("cuttingDelay")]
        public float CuttingDelay = 1f;
    }

    public enum CableType
    {
        HighVoltage,
        MediumVoltage,
        Apc,
    }
}
