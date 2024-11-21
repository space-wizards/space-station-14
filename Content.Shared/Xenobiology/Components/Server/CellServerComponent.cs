using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components.Server;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CellServerComponent : Component
{
    [DataField, ViewVariables, AutoNetworkedField]
    public List<Cell> Cells = [];

    [ViewVariables, AutoNetworkedField]
    public int Id;

    [ViewVariables]
    public HashSet<EntityUid> Clients = [];
}
