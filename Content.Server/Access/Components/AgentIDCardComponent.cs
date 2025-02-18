using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Server.Access.Components;

/// <summary>
///     Allows an ID card to copy accesses from other IDs and to change the name, job title and job icon via an interface.
/// </summary>
[RegisterComponent]
public sealed partial class AgentIDCardComponent : Component
{
    /// <summary>
    ///     Groups of job icons this can ID can use.
    /// </summary>
    [DataField]
    public List<ProtoId<JobIconGroupPrototype>> IconGroups = new();
}
