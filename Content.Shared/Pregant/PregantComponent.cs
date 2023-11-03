using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Pregant.Components
{
    [RegisterComponent]
    public sealed partial class PregantComponent : Component
    {

        /// <summary>
        ///     The entity prototype of the mob that Raise Army summons
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("armyMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ArmyMobSpawnId = "MobSlasher";

    }
}
