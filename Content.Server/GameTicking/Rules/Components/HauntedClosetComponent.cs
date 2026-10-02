namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Configures this specific closet for random closing
/// </summary>
[RegisterComponent]
public sealed partial class HauntedClosetComponent : Component
{
    /// <summary>
    /// 1 = always closed, 0 = never closed
    /// </summary>
    [DataField]
    public float CloseChance = 0.01f;
}
