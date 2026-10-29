namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule that closes random closets and entity storages with the <see cref="HauntedClosetComponent"> component.
/// </summary>
[RegisterComponent]
public sealed partial class HauntedClosetRuleComponent : Component
{
    /// <summary>
    /// Fraction of the closets/storages that will be affected
    /// </summary>
    [DataField]
    public float ClosetFraction = 0.05f;

    /// <summary>
    /// If this is true, the closets will have their state toggled. If this is false, then they will only be closed.
    /// </summary>
    [DataField]
    public bool Toggle = true;
}
