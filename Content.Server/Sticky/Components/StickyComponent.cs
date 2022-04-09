using Robust.Server.GameObjects;

namespace Content.Server.Sticky.Components;
using Content.Shared.DrawDepth;

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
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     What sprite draw depth set when entity sticked.
    ///     Work only when Sprite <see cref="SpriteComponent.NetSyncEnabled"/> is true.
    /// </summary>
    [DataField("stickedDrawDepth")]
    public int StickedDrawDepth = (int) DrawDepth.Overdoors;
}
