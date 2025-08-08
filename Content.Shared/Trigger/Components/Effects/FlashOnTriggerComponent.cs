using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Will cause a flash in an area around the entity when triggered.
/// If TargetUser is true then their location will be used.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlashOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The range in which to flash entities in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 1.0f;

    /// <summary>
    /// The duration of the status effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The probability to apply the status effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Probability = 1.0f;
}
