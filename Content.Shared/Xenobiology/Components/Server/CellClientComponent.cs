using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components.Server;

[RegisterComponent, NetworkedComponent]
public sealed partial class CellClientComponent : Component
{
    [ViewVariables]
    public bool ConnectedToServer => Server != null;

    [ViewVariables]
    public EntityUid? Server;
}
