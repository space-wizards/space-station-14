using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadModifyIdCardComponent : Component
{
    [DataField]
    public string? Title;

    [DataField]
    public ProtoId<JobIconPrototype>? Icon;

    [DataField]
    public string? Name;

}