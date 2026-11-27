using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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
    public Dictionary<ActionRequirementTarget, ActionRequirementCondition[]> Requirements = new();

    /// <summary>
    /// What entity conditions the related entities need to meet for the action to count as valid.
    /// Can be used, for example, to check if the performer has enough satiation.
    /// </summary>
    [DataField, Obsolete("Use the Requirements field instead, the indentation is different")]
    public Dictionary<ActionRequirementTarget, EntityCondition[]> Conditions = new();

    /// <summary>
    /// Effects to use on the related entities AFTER the action is successfully performed.
    /// Can be used, for example, to spend satiation after the action is performed.
    /// </summary>
    [DataField]
    public Dictionary<ActionRequirementTarget, EntityEffect[]> Effects = new();

    /// <summary>
    /// The text to show to the performer about why their action failed.
    /// Gets "Performer", "Target" and "Action" passed into it as identities.
    /// </summary>
    [DataField]
    public LocId? FailPopup = null;

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

/// <summary>
/// An <see cref="EntityCondition"/> paired with the popup shown to the performer if it fails.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class ActionRequirementCondition
{
    /// <summary>
    /// The condition that must be met.
    /// </summary>
    [DataField(required: true)]
    public EntityCondition Condition = default!;

    /// <summary>
    /// Popup shown to the performer if this condition fails.
    /// Takes priority over <see cref="ActionRequirementsComponent.FailPopup"/>.
    /// </summary>
    [DataField]
    public LocId? FailPopup;

    /// <summary>
    /// Popup type for <see cref="FailPopup"/>.
    /// Falls back to <see cref="ActionRequirementsComponent.FailPopupType"/> if null.
    /// </summary>
    [DataField]
    public PopupType? FailPopupType;
}
