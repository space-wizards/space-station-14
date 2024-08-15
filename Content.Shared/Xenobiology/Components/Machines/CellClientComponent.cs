namespace Content.Shared.Xenobiology.Components.Machines;

[RegisterComponent]
public sealed partial class CellClientComponent : Component
{
    [ViewVariables]
    public bool ConnectedToServer => Server != null;

    [ViewVariables]
    public EntityUid? Server;
}
