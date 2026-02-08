namespace Content.Server.Nuke;

/// <summary>
/// This is used for storing the round's current nuke codes in the event global nuke codes are enabled.
/// </summary>
[RegisterComponent]
public sealed partial class NukeCodeManagerComponent : Component
{
    /// <summary>
    /// Current code required to detonate this nuke.
    /// </summary>
    [DataField] public string Code = string.Empty;
}
