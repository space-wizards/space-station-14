using Robust.Shared.GameStates;

namespace Content.Shared.Metabolism;

/// <summary>
/// Causes altered speed in the afflicted entity's metabolism.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MetabolizerSystem))]
public sealed partial class RushedMetabolismStatusEffectComponent : Component
{
    /// <summary>
    /// What the metabolic update rate will be multiplied by, (higher = slower metabolism)
    /// </summary>
    /// <example>
    /// 0.5 = fast metabolism, i.e. half the time between ticks.<br/>
    /// 1 = no change.<br/>
    /// 2 = slower metabolism, i.e. twice the time between ticks.
    /// </example>
    [DataField(required: true), AutoNetworkedField]
    public float Multiplier;
}
