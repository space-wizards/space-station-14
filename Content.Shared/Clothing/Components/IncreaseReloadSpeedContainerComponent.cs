using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     A container with this component will modify the reloading speed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IncreaseReloadSpeedContainerComponent : Component
{
    /// <summary>
    ///     The multiplicative modifier of the reloading doAfter duration.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public float Modifier;
}
