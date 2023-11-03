using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.InfectorDead.Components
{

    [RegisterComponent, NetworkedComponent]
    public sealed partial class InfectorDeadComponent : Component
    {
        [DataField("infectedDuration")]
        public float InfectedDuration = 2.5f;

        [ViewVariables(VVAccess.ReadWrite), DataField("armyMobSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ArmyMobSpawnId = "MobSlasher";
    }

}
