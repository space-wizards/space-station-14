using Content.Shared.Damage;
using Content.Shared.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Repairable
{
    [RegisterComponent]
    public sealed class RepairableComponent : Component
    {
        /// <summary>
        ///     All the damage to change information is stored in this <see cref="DamageSpecifier"/>.
        /// </summary>
        /// <remarks>
        ///     If this data-field is specified, it will change damage by this amount instead of setting all damage to 0.
        ///     in order to heal/repair the damage values have to be negative.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("damage")]
        public DamageSpecifier? Damage;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("fuelCost")]
        public int FuelCost = 5;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string QualityNeeded = "Welding";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("doAfterDelay")]
        public int DoAfterDelay = 1;
    }
}
