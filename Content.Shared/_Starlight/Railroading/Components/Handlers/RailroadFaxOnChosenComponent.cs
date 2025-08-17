using Content.Shared.Paper;
using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadFaxOnChosenComponent : Component
{
    [DataField]
    public HashSet<string> Addresses = [];

    [DataField(required: true)]
    public List<RailroadFaxLetter> Letters = [];
}

[DataDefinition]
public sealed partial class RailroadFaxLetter
{
    [DataField("name", required: true)]
    public string PaperName;

    [DataField("content", required: true)]
    public string PaperContent;

    [DataField]
    public string? PaperLabel;

    [DataField]
    public string? StampState;

    [DataField]
    public List<StampDisplayInfo> StampedBy = [];

    [DataField]
    public string PaperPrototype = "";

    [DataField]
    public bool Locked;
}
