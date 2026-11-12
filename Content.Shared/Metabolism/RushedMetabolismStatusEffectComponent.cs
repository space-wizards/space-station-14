using Robust.Shared.GameStates;

namespace Content.Shared.Metabolism;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MetabolizerSystem))]
public sealed partial class RushedMetabolismStatusEffectComponent : Component
{
    /// <summary>
    /// What the metabolic update rate will be multiplied by (higher = slower metabolism)
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Multiplier;
}
