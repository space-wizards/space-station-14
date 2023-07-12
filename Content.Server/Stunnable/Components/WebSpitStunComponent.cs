using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Server.Stunnable.Components

{
    [RegisterComponent, Access(typeof(WebSpitStunSystem))]
    public sealed class WebSpitStunComponent : Component
    {

        [DataField("actionWebSpit", required: true)]
        public WorldTargetAction ActionWebSpit = new();

        [ViewVariables(VVAccess.ReadOnly)]
        public Container Container = default!;

        [DataField("walkSpeedMultiplier")]
        public float WalkSpeedMultiplier = 1f;

        [DataField("runSpeedMultiplier")]
        public float RunSpeedMultiplier = 1f;

        [ViewVariables(VVAccess.ReadWrite),
         DataField("bulletWebSpitId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string BulletWebSpitId = "BulletWebSpit";

    }

}
