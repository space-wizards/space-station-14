using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Specifies the action performer/target needs to meet specific conditions for the action to be correctly validated.
/// Can optionally use entity effects to do things such as subtracting satiation.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ActionRequirementsSystem))]
public sealed partial class ActionRequirementsComponent : Component
{
    /// <summary>
    /// What entity conditions the related entities need to meet for the action to count as valid.
    /// Can be used, for example, to check if the performer has enough satiation.
    /// </summary>
    [DataField]
    public Dictionary<ActionRequirementTarget, EntityCondition[]> Conditions = new ();

    /// <summary>
    /// Effects to use on the related entities AFTER the action is successfully performed.
    /// Can be used, for example, to spend satiation after the action is performed.
    /// </summary>
    [DataField]
    public Dictionary<ActionRequirementTarget, EntityEffect[]> Effects = new ();

    /// <summary>
    /// The text to show to the performer about why their action failed.
    /// Gets "Performer", "Target" and "Action" passed into it as identities.
    /// </summary>
    [DataField]
    public LocId? FailPopup; // TODO: Conditions should return their own text for why they failed to use in popups. Then this can be optional and be made to ignore that text instead.

    /// <summary>
    /// The type the popup for the performer should be.
    /// </summary>
    [DataField]
    public PopupType FailPopupType = PopupType.Small;
}

/// <summary>
/// Who the condition/effect will target.
/// </summary>
[Flags]
public enum ActionRequirementTarget : byte
{
    /// <summary>
    /// Literally nothing. The condition and effect will target nothing, and as such, cannot happen.
    /// </summary>
    None = 0,

    /// <summary>
    /// The target is the action performer.
    /// </summary>
    Performer = 1 << 0,

    /// <summary>
    /// The target is the action target (if any)
    /// </summary>
    Target = 1 << 1,

    /// <summary>
    /// The target is both the performer and the target.
    /// </summary>
    Both = Performer | Target,

    // World - Maybe once conditions and effects support tile/world targeting properly.
}
