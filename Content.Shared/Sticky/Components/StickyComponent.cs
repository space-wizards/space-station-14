using Content.Shared.Sticky.Systems;
﻿using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Sticky.Components;

/// <summary>
/// Items that can be stuck to other structures or entities.
/// For example, paper stickers or C4 charges.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StickySystem))]
[AutoGenerateComponentState]
public sealed partial class StickyComponent : Component
{
    /// <summary>
    /// What target entities are valid to be surface for sticky entity.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// What target entities can't be used as surface for sticky entity.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// How much time it takes to stick the entity to a target.
    /// If zero, it will immediately be stuck.
    /// </summary>
    [DataField]
    public TimeSpan StickDelay = TimeSpan.Zero;

    /// <summary>
    /// Whether users can unstick the entity after it has been stuck.
    /// </summary>
    [DataField]
    public bool CanUnstick = true;

    /// <summary>
    /// How much time it takes to unstick the entity.
    /// If zero, it will immediately be unstuck.
    /// </summary>
    [DataField]
    public TimeSpan UnstickDelay = TimeSpan.Zero;

    /// <summary>
    /// Popup message shown when player starts sticking the entity to another entity.
    /// </summary>
    [DataField]
    public LocId? StickPopupStart;

    /// <summary>
    /// Popup message shown when a player successfully sticks the entity.
    /// </summary>
    [DataField]
    public LocId? StickPopupSuccess;

    /// <summary>
    /// Popup message shown when a player starts unsticking the entity from another entity.
    /// </summary>
    [DataField]
    public LocId? UnstickPopupStart;

    /// <summary>
    /// Popup message shown when a player successfully unsticks the entity.
    /// </summary>
    [DataField]
    public LocId? UnstickPopupSuccess;

    /// <summary>
    /// Entity that is used as a surface for the sticky entity.
    /// Null if entity isn't stuck to anything.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? StuckTo;

    /// <summary>
    /// Text to use for the unstick verb.
    /// </summary>
    [DataField]
    public LocId VerbText = "comp-sticky-unstick-verb-text";

    /// <summary>
    /// Icon to use for the unstick verb.
    /// </summary>
    [DataField]
    public SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png"));
}
