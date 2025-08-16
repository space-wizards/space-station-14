using Robust.Shared.GameStates;

namespace Content.Shared.Lube;

/// <summary>
/// If you try to pick up an item with this component it will be thrown.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LubedComponent : Component
{
    /// <summary>
    /// The number of throws before this component will be removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SlipsLeft;

    /// <summary>
    /// The throwing velocity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SlipStrength = 10.0f;
}
