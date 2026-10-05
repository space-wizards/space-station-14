namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule that closes random closets and entity storages with the <see cref="HauntedClosetComponent"> component.
/// </summary>
[RegisterComponent]
public sealed partial class HauntedClosetRuleComponent : Component
{
    /// <summary>
    /// Multiplier applied to the odds of each individual storage. Higher is more often; 1 is always, 0 is never.
    /// </summary>
    [DataField]
    public float ChanceMultiplier = 1f;

    /// <summary>
    /// If this is true, the closets will have their state toggled. If this is false, then they will only be closed.
    /// </summary>
    [DataField]
    public bool Toggle = true;
}
