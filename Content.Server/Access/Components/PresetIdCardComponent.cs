using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Access.Components;

/// <summary>
/// Changes icon, job and name of the owner ID-Card on init.
/// </summary>
[RegisterComponent]
public sealed partial class PresetIdCardComponent : Component
{
    /// <summary>
    /// Job prototype used to configure the ID-Card.
    /// </summary>
    [DataField("job")]
    public ProtoId<JobPrototype>? JobName;

    /// <summary>
    /// Name of the ID-Card.
    /// </summary>
    [DataField("name")]
    public LocId? IdName;
}
