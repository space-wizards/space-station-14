// Moffstation - Start - Added stuff for the thieving toggle
using Content.Shared.Alert;
using Content.Shared._Moffstation.Strip.Components;
using Robust.Shared.Prototypes;
// Moffstation - End

namespace Content.Shared.Strip.Components;

/// <summary>
/// Give this to an entity when you want to decrease stripping times
/// </summary>
[RegisterComponent]
public sealed partial class ThievingComponent : Component
{
    /// <summary>
    /// How much the strip time should be shortened by
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("stripTimeReduction")]
    public TimeSpan StripTimeReduction = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// Should it notify the user if they're stripping a pocket?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("stealthy")]
    public bool Stealthy;

    // Moffstation - Start - Adding a variable for the alert module
    /// <summary>
    /// Variable pointing at the Alert modal
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype> StealthyAlertProtoId = "Stealthy";
    // Moffstation - End
}
