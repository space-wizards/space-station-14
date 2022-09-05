namespace Content.Server.Containers;

[RegisterComponent]
public sealed class ContainerFillComponent : Component
{
    [ViewVariables]
    [DataField("construction")]
    public bool Construction;

    [ViewVariables]
    [DataField("containers")]
    public Dictionary<string, List<string>> Containers = new();
}
