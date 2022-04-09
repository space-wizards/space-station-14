namespace Content.Server.Sticky.Components;

/// <summary>
///     Items that can be stick to other structures or entities.
///     For example paper stickers or C4 charges.
/// </summary>
[RegisterComponent]
public sealed class StickyComponent : Component
{
    /// <summary>
    ///     How much time does it take to place entity.
    /// </summary>
    public TimeSpan Delay = TimeSpan.FromSeconds(0.1);
}
