using Robust.Shared.GameStates;

namespace Content.Shared.Armable;

/// <summary>
/// Makes an item armable, needs ItemToggleComponent to work.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ArmableSystem))]
public sealed partial class ArmableComponent : Component
{
    /// <summary>
    /// Does it show its status on examination?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowStatusOnExamination = true;

    /// <summary>
    /// Does it change appearance when activated?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ChangeAppearance = true;

    /// <summary>
    /// Text to show on examination when the entity is armed.
    /// </summary>
    [DataField]
    public LocId? ExamineTextArmed = "armable-examine-armed";

    /// <summary>
    /// Text to show on examination when the entity is not armed
    /// </summary>
    [DataField]
    public LocId? ExamineTextNotArmed ="armable-examine-not-armed";
}
