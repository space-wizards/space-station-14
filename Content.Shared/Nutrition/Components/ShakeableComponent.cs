using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShakeableComponent : Component
{
    /// <summary>
    /// How long it takes to shake this item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ShakeDuration = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// Does the entity need to be in the user's hand in order to be shaken?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireInHand = true;

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
    [DataField, AutoNetworkedField]
    public SoundSpecifier ShakeSound = new SoundPathSpecifier("/Audio/Items/soda_shake.ogg");
}
