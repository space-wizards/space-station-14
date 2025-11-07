using Robust.Shared.GameStates;

namespace Content.Shared.Tests;

/// <summary>
/// Component that is used by integration tests to mark and easily find certain entities on a test map.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TestMarkerComponent : Component
{
    /// <summary>
    /// A string to keep multiple markers on the same map distinct.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Id = "marker";
}
