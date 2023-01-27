using Robust.Shared.Serialization;

namespace Content.Shared.Dragon;

[Serializable, NetSerializable]
public sealed partial class DragonRiftComponentState : ComponentState
{
    public DragonRiftState State;
}
