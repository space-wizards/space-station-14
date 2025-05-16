using Robust.Shared.GameStates;

namespace Content.Server.Revolutionary.Components;

/// <summary>
/// Component used for storing the implant UID for a head revolutionary.
/// This is a server-side component to avoid access permission issues.
/// </summary>
[RegisterComponent]
public sealed partial class HeadRevolutionaryImplantComponent : Component
{
    /// <summary>
    /// The entity UID of the USSP uplink implant associated with this head revolutionary.
    /// This allows for tracking which implant belongs to which head revolutionary.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ImplantUid;
}
