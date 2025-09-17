using Content.Shared.StatusIcon;
using Content.Shared.Access;
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

    /// <summary>
    /// List of accesses to add to the ID card if not already held
    /// </summary>
    [DataField]
    public IReadOnlyCollection<ProtoId<AccessLevelPrototype>> AccessAdd = Array.Empty<ProtoId<AccessLevelPrototype>>();

    /// <summary>
    /// List of accesses to remove from the ID card if they are currently held
    /// </summary>
    [DataField]
    public IReadOnlyCollection<ProtoId<AccessLevelPrototype>> AccessRemove = Array.Empty<ProtoId<AccessLevelPrototype>>();

    [DataField]
    public string DefaultIdPrototypeIfNoneFound = "AssistantIdCard";
}