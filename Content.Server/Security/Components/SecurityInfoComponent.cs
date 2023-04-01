using Content.Shared.Security;

namespace Content.Server.Security.Components;

/// <summary>
/// This is used for containing security-related information
/// about any person.
/// </summary>
[RegisterComponent]
public sealed class SecurityInfoComponent : Component
{
    [DataField("status")]
    public SecurityStatus Status = SecurityStatus.None;
}
