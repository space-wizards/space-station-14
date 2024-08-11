using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CellCollectorComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4f);

    [DataField]
    public bool Used;
}
