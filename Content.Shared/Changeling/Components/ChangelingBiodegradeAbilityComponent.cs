using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Allows the changeling to vomit acid over restraints, setting themselves free and stunning whoever is pulling them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingBiodegradeAbilityComponent : Component
{
    /// <summary>
    /// How long to stun the puller of the changeling when this action is activated.
    /// </summary>
    [DataField]
    public TimeSpan PullerStunDuration = TimeSpan.FromSeconds(3f);

    /// <summary>
    /// The popup to display over the user when the action is used.
    /// Only visible to other players.
    /// </summary>
    [DataField]
    public LocId ActivatedPopup = "changeling-biodegrade-used-popup";

    /// <summary>
    /// The popup to display over the user when the action is used.
    /// Only visible to the user
    /// </summary>
    [DataField]
    public LocId ActivatedPopupSelf = "changeling-biodegrade-used-popup-self";

    /// <summary>
    /// The sound to play when the ability is successfully used.
    /// </summary>
    [DataField]
    public SoundSpecifier ActivatedSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    /// <summary>
    /// The reagents to spill at the location of the entity when this action is used.
    /// </summary>
    [DataField]
    public Solution? SpillSolution = new([new("SulfuricAcid", 30)]);
}

/// <summary>
/// Action event for Biodegrade, raised on the ability when used.
/// </summary>
public sealed partial class ChangelingBiodegradeActionEvent : InstantActionEvent;
