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
        ///     All the damage to heal information is stored in this <see cref="DamageSpecifier"/>.
        /// </summary>
        /// <remarks>
        ///     If this data-field is specified, it will heal by this amount instead of fully.
        ///     to heal the values have to be negative.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("heal")]
        public DamageSpecifier? Heal;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("fuelCost")]
        public int FuelCost = 5;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string QualityNeeded = "Welding";

        [ViewVariables(VVAccess.ReadWrite)] [DataField("doAfterDelay")]
        public int DoAfterDelay = 1;
    }
}
