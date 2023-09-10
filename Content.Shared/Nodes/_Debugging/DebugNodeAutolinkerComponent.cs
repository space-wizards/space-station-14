using Robust.Shared.GameStates;

namespace Content.Shared.Nodes.Debugging;

/// <summary>
/// 
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DebugNodeAutolinkerComponent : Component
{
    /// <summary>
    /// 
    /// </summary>
    [AutoNetworkedField]
    [DataField("baseRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BaseRange = 3f;

    /// <summary>
    /// 
    /// </summary>
    [AutoNetworkedField]
    [DataField("hysteresisRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float HysteresisRange = 1f;
}
