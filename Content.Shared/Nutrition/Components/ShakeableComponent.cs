using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Adds a "Shake" verb to the entity's verb menu.
/// Handles checking the entity can be shaken, displaying popups when shaking,
/// and raising a ShakeEvent when a shake occurs.
/// Reacting to being shaken is left up to other components.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShakeableComponent : Component
{
    /// <summary>
    /// How long it takes to shake this item.
    /// </summary>
    [DataField]
    public TimeSpan ShakeDuration = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// Does the entity need to be in the user's hand in order to be shaken?
    /// </summary>
    [DataField]
    public bool RequireInHand;

    /// <summary>
    /// Label to display in the verbs menu for this item's shake action.
    /// </summary>
    [DataField]
    public LocId ShakeVerbText = "shakeable-verb";

    /// <summary>
    /// Text that will be displayed to the user when shaking this item.
    /// </summary>
    [DataField]
    public LocId ShakePopupMessageSelf = "shakeable-popup-message-self";

    /// <summary>
    /// Text that will be displayed to other users when someone shakes this item.
    /// </summary>
    [DataField]
    public LocId ShakePopupMessageOthers = "shakeable-popup-message-others";

    /// <summary>
    /// The sound that will be played when shaking this item.
    /// </summary>
    [DataField]
    public SoundSpecifier ShakeSound = new SoundPathSpecifier("/Audio/Items/soda_shake.ogg");
}
