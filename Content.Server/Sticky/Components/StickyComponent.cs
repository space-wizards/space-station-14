using Content.Shared.Whitelist;
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
    ///     What target entities are valid to be surface for sticky entity.
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     How much time does it take to stick entity to target.
    ///     If zero will stick entity immediately.
    /// </summary>
    [DataField("stickDelay")]
    public TimeSpan StickDelay = TimeSpan.Zero;

    /// <summary>
    ///     What sprite draw depth set when entity stuck.
    ///     Work only when Sprite <see cref="SpriteComponent.NetSyncEnabled"/> is true.
    /// </summary>
    [DataField("stuckDrawDepth")]
    public int StuckDrawDepth = (int) DrawDepth.Overdoors;

    /// <summary>
    ///     Whether users can unstick item when it was stuck to surface.
    /// </summary>
    [DataField("canUnstick")]
    public bool CanUnstick = true;

    /// <summary>
    ///     How much time does it take to unstick entity.
    ///     If zero will unstick entity immediately.
    /// </summary>
    [DataField("unstickDelay")]
    public TimeSpan UnstickDelay = TimeSpan.Zero;

    /// <summary>
    ///     Popup message shown when player started sticking entity to another entity.
    /// </summary>
    [DataField("stickPopupStart")]
    public string? StickPopupStart;

    /// <summary>
    ///     Popup message shown when player successfully stuck entity.
    /// </summary>
    [DataField("stickPopupSuccess")]
    public string? StickPopupSuccess;

    /// <summary>
    ///     Popup message shown when player started unsticking entity from another entity.
    /// </summary>
    [DataField("unstickPopupStart")]
    public string? UnstickPopupStart;

    /// <summary>
    ///     Popup message shown when player successfully unstuck entity.
    /// </summary>
    [DataField("unstickPopupSuccess")]
    public string? UnstickPopupSuccess;

    /// <summary>
    ///     Entity that is used as surface for sticky entity.
    ///     Null if entity doesn't stuck to anything.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? StuckTo;
}
