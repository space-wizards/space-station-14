using Content.Server.Pointing.EntitySystems;
using Content.Shared.Pointing.Components;

namespace Content.Server.Pointing.Components
{
    [RegisterComponent]
    [Access(typeof(RoguePointingSystem))]
    public sealed partial class RoguePointingArrowComponent : SharedRoguePointingArrowComponent
    {
        [ViewVariables]
        public EntityUid? Chasing;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("turningDelay")]
        public float TurningDelay = 2;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chasingSpeed")]
        public float ChasingSpeed = 5;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chasingTime")]
        public float ChasingTime = 1;
    }
}
