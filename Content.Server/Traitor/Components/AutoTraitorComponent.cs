using Content.Server.Traitor.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Traitor.Components;

/// <summary>
/// Makes the entity a traitor either instantly if it has a mind or when a mind is added.
/// </summary>
[RegisterComponent, Access(typeof(AutoTraitorSystem))]
public sealed partial class AutoTraitorComponent : Component
{
    /// <summary>
    /// The traitor profile to use
    /// </summary>
    [DataField]
    public EntProtoId Profile = "Traitor";
}
