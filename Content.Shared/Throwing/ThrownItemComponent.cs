using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Throwing
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ThrownItemComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("thrower")]
        public EntityUid? Thrower { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class ThrownItemComponentState : ComponentState
    {
        public NetEntity? Thrower { get; }

        public ThrownItemComponentState(NetEntity? thrower)
        {
            Thrower = thrower;
        }
    }
}
