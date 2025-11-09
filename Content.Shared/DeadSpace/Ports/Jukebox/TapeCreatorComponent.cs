using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Ports.Jukebox;

[RegisterComponent, NetworkedComponent]
public sealed partial class TapeCreatorComponent : Component
{
    [DataField("coins")]
    public int CoinBalance { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Recording { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public NetEntity? InsertedTape { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public Container TapeContainer { get; set; } = default!;
}

[Serializable, NetSerializable]
public sealed class TapeCreatorComponentState : ComponentState
{
    public int CoinBalance { get; set; }
    public bool Recording { get; set; }
    public NetEntity? InsertedTape { get; set; }
}
