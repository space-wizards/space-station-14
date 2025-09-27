using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Gravity;

/// <summary>
/// This Component allows a target to be considered "weightless" when Weightless is true. Without this component, the
/// target will never be weightless.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GravityAffectedComponent : Component
{
    /// <summary>
    /// If true, this entity will be considered "weightless"
    /// </summary>
    /// <remarks>
    /// Not a datafield to keep the map file size sane.
    /// This value is only cached and will be refreshed on component init.
    /// </remarks>
    [ViewVariables]
    public bool Weightless = true;
}

[Serializable, NetSerializable]
public sealed class GravityAffectedComponentState(bool weightless) : ComponentState
{
    public bool Weightless = weightless;
}
