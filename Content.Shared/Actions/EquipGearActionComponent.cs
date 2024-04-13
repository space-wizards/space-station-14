using Robust.Shared.GameStates;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Audio;

namespace Content.Shared.Actions;

/// <summary>
/// An action which equips and unequips a gear prototype.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EquipGearActionSystem))]
public sealed partial class EquipGearActionComponent : Component
{
    /// <summary>
    ///     The starting gear prototype ID.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype> PrototypeID;

    /// <summary>
    /// Popup that shows up when the gear is equipped to the user.
    /// </summary>
    [DataField]
    public LocId PopupEquipSelf = string.Empty;

    /// <summary>
    /// Popup that shows up when the gear is unequipped to the user.
    /// </summary>
    [DataField]
    public LocId PopupUnequipSelf = string.Empty;

    /// <summary>
    /// Popup that shows up when the gear is equipped, to other entities.
    /// </summary>
    [DataField]
    public LocId PopupEquipOthers = string.Empty;

    /// <summary>
    /// Popup that shows up when the gear is equipped, to other entities.
    /// </summary>
    [DataField]
    public LocId PopupUnequipOthers = string.Empty;

    /// <summary>
    /// The popup type of the popups.
    /// </summary>
    [DataField]
    public PopupType PopupType = PopupType.MediumCaution;

    /// <summary>
    /// If the gear is equipped or not.
    /// </summary>
    [DataField]
    public bool Equipped = false;

    /// <summary>
    /// Sound that plays on gear toggle.
    /// </summary>
    [DataField]
    public SoundSpecifier? ToggleSound;
}
