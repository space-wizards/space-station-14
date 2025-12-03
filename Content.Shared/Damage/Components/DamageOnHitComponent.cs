using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Damages the held item by a set amount when it hits someone. Can be used to make melee items limited-use.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageOnHitComponent : Component
{
    /// <summary>
    /// Whether to ignore damage modifiers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreResistances = true;

    /// <summary>
    /// The damage amount to deal on hit.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = new();
}
