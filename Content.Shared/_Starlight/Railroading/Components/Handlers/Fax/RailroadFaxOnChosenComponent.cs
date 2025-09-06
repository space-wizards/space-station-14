using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadFaxOnChosenComponent : Component, IRailroadFaxComponent
{
    [DataField]
    public HashSet<string> Addresses { get; set; } = [];

    [DataField(required: true)]
    public List<RailroadFaxLetter> Letters { get; set; } = [];
}
