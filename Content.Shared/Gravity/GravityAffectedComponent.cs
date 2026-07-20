using Robust.Shared.GameStates;

namespace Content.Shared.Gravity;

/// <summary>
/// This Component allows a target to be considered "weightless" when Weightless is true. Without this component, the
/// target will never be weightless.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GravityAffectedComponent : Component
{
    /// <summary>
    /// If true, this entity will be considered "weightless"
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool Weightless = true;

    /// <summary>
    /// If true, the <see cref="Weightless"/> value is currently being provided via the grid through <see cref="SharedGravitySystem.EntityGridOrMapHaveGravity"/>.
    /// If false, the value is provided by the <see cref="IsWeightlessEvent"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool GridWeightlessStatus = false;
}
