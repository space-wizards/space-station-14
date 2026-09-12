using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Changes the date used for deciding what holidays to celebrate. The trigger target is ignored.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HolidayOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The new holiday date.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DateTime Time = new(2001, 9, 11); // Never forget
}
