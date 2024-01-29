using Content.Shared.Whitelist;

namespace Content.Server.Sticky.Components;

/// <summary>
///     Items that can be stick to other structures or entities.
///     For example paper stickers or C4 charges.
/// </summary>
[RegisterComponent]
public sealed partial class StickyComponent : Component
{
    /// <summary>
    ///     What target entities are valid to be surface for sticky entity.
    /// </summary>
    [DataField("whitelist")]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     What target entities can't be used as surface for sticky entity.
    /// </summary>
    [DataField("blacklist")]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Blacklist;

    /// <summary>
    ///     How much time does it take to stick entity to target.
    ///     If zero will stick entity immediately.
    /// </summary>
    [DataField("stickDelay")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StickDelay = TimeSpan.Zero;

    /// <summary>
    ///     Whether users can unstick item when it was stuck to surface.
    /// </summary>
    [DataField("canUnstick")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanUnstick = true;

    /// <summary>
    ///     How much time does it take to unstick entity.
    ///     If zero will unstick entity immediately.
    /// </summary>
    [DataField("unstickDelay")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan UnstickDelay = TimeSpan.Zero;

    /// <summary>
    ///     Popup message shown when player started sticking entity to another entity.
    /// </summary>
    [DataField("stickPopupStart")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StickPopupStart;

    /// <summary>
    ///     Popup message shown when player successfully stuck entity.
    /// </summary>
    [DataField("stickPopupSuccess")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? StickPopupSuccess;

    /// <summary>
    ///     Popup message shown when player started unsticking entity from another entity.
    /// </summary>
    [DataField("unstickPopupStart")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? UnstickPopupStart;

    /// <summary>
    ///     Popup message shown when player successfully unstuck entity.
    /// </summary>
    [DataField("unstickPopupSuccess")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? UnstickPopupSuccess;

    /// <summary>
    ///     Entity that is used as surface for sticky entity.
    ///     Null if entity doesn't stuck to anything.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? StuckTo;

    /// <summary>
    /// For the DoAfter event to tell if it should stick or unstick
    /// </summary>
    public bool Stick;
}
