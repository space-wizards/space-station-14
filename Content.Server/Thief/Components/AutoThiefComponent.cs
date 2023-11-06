using Content.Server.Thief.Systems;

namespace Content.Server.Thief.Components;

/// <summary>
/// Makes the entity a thief either instantly if it has a mind or when a mind is added.
/// </summary>
[RegisterComponent, Access(typeof(AutoThiefSystem))]
public sealed partial class AutoThiefComponent : Component
{
    /// <summary>
    /// Whether to give the traitor an uplink or not.
    /// </summary>
    //[DataField("giveUplink"), ViewVariables(VVAccess.ReadWrite)]
    //public bool GiveUplink = true;

    /// <summary>
    /// Whether to give the traitor objectives or not.
    /// </summary>
    //[DataField("giveObjectives"), ViewVariables(VVAccess.ReadWrite)]
    //public bool GiveObjectives = true;
    //

    //Settings for make thief
}
