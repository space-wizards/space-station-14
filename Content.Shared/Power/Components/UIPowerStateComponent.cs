namespace Content.Shared.Power.Components;

/// <summary>
/// Component for entities that want to increase their power usage to a working state when
/// a UI on the machine is open. Requires <see cref="PowerStateComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class UIPowerStateComponent : Component
{
    /// <summary>
    /// List of UI keys that will trigger the working state.
    /// If null, any UI open will trigger the working state.
    /// </summary>
    [DataField]
    public List<Enum>? Keys;
}
