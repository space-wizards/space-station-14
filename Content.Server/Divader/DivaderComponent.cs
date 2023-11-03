using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Divader
{
    [RegisterComponent]
    public sealed partial class DivaderComponent : Component
    {

        [ViewVariables(VVAccess.ReadWrite), DataField("DivaderRHMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string RHMobSpawnId = "MobDivaderRH";

        [ViewVariables(VVAccess.ReadWrite), DataField("DivaderHMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string HMobSpawnId = "MobDivaderH";

        [ViewVariables(VVAccess.ReadWrite), DataField("DivaderLHMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string LHMobSpawnId = "MobDivaderLH";

    }
}
