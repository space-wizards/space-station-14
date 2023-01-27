using Content.Shared.Whitelist;

namespace Content.Server.Shuttles.Components;

[RegisterComponent]
public sealed partial class FTLDestinationComponent : Component
{
    /// <summary>
    /// Should this destination be restricted in some form from console visibility.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Is this destination visible but available to be warped to?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("enabled")]
    public bool Enabled = true;
}
