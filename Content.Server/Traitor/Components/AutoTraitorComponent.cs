using Content.Server.Traitor.Systems;

namespace Content.Server.Traitor.Components;

/// <summary>
/// Makes the entity a traitor either instantly if it has a mind or when a mind is added.
/// </summary>
[RegisterComponent, Access(typeof(AutoTraitorSystem))]
public sealed partial class AutoTraitorComponent : Component
{
    /// <summary>
    /// Whether to give the traitor an uplink or not.
    /// </summary>
    [DataField("giveUplink"), ViewVariables(VVAccess.ReadWrite)]
    public bool GiveUplink = true;

    /// <summary>
    /// Whether to give the traitor objectives or not.
    /// </summary>
    [DataField("giveObjectives"), ViewVariables(VVAccess.ReadWrite)]
    public bool GiveObjectives = true;
}
