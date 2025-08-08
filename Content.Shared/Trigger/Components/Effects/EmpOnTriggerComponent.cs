using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will cause an EMP at the entity's location when triggered.
/// If TargetUser is true then it will be spawned at their position.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmpOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// EMP range.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 1.0f;

    /// <summary>
    /// How much energy (in Joules) will be consumed per battery in range.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EnergyConsumption;

    /// <summary>
    /// How long it disables targets.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DisableDuration = TimeSpan.FromSeconds(60);
}
