using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Inflicts configured damage when this entity lands.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageOnLandComponent : Component
{
    /// <summary>
    /// Whether to ignore damage modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreResistances = false;

    /// <summary>
    /// The amount of damage to deal when this entity lands.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = new();
}
