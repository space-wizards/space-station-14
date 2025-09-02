namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadFaxOnFailedComponent : Component, IRailroadFaxComponent
{
    [DataField]
    public HashSet<string> Addresses { get; set; } = [];

    [DataField(required: true)]
    public List<RailroadFaxLetter> Letters { get; set; } = [];
}
