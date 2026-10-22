using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Makes a clothing item despawn when unequipped, stripped or removed in any other way.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FleetingClothingComponent : Component
{
    /// <summary>
    /// The sound to play when unequipping.
    /// </summary>
    [DataField]
    public SoundSpecifier? RemovedSound;

    /// <summary>
    /// Should the sound also be played when the player wearing the item unequips it themselves?
    /// </summary>
    [DataField]
    public bool PlaySoundOnSelfUnequip = true;

    /// <summary>
    /// The popup to show to the wearer if they unequip this item themselves.
    /// </summary>
    [DataField]
    public LocId? SelfUnquipPopupWearer = "fleeting-clothing-component-default-popup";

    /// <summary>
    /// The popup to show to others if the wearer unequips this item themselves.
    /// </summary>
    [DataField]
    public LocId? SelfUnquipPopupOthers = "fleeting-clothing-component-default-popup";

    /// <summary>
    /// The popup to show to everone if this item was removed by any other means.
    /// </summary>
    /// <remarks>
    /// We can't split this one up into wearer/others popups because EntGotRemovedFromContainerEvent does not have the user passed in.
    /// </remarks>
    [DataField]
    public LocId? RemovedPopup = "fleeting-clothing-component-default-popup";

    /// <summary>
    /// Examine text shown to the wearer.
    /// </summary>
    [DataField]
    public LocId? ExamineWearer = "fleeting-clothing-component-default-examine";

    /// <summary>
    /// Examine text shown to others.
    /// </summary>
    [DataField]
    public LocId? ExamineOthers = "fleeting-clothing-component-default-examine";

    /// <summary>
    /// If true this entity will use <see cref="SharedDestructibleSystem.DestroyEntity"/> rather than simply be deleted.
    /// Use this if you need to do stuff before deleting it, for example emptying storage containers so that the contents don't get deleted with with.
    /// If false the clothing item will just be deleted instead.
    /// </summary>
    [DataField]
    public bool DestroyOnUnequip = true;
}
