using Content.Server.Friends.Systems;

namespace Content.Server.Friends.Components;

/// <summary>
/// Pet something to become friends with it (use in hand, press Z)
/// Uses FactionExceptionComponent behind the scenes
/// </summary>
[RegisterComponent, Access(typeof(PettableFriendSystem))]
public sealed partial class PettableFriendComponent : Component
{
    /// <summary>
    /// Localized popup sent when petting for the first time
    /// </summary>
    [DataField("successString", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string SuccessString = string.Empty;

    /// <summary>
    /// Localized popup sent when petting multiple times
    /// </summary>
    [DataField("failureString", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string FailureString = string.Empty;
}
