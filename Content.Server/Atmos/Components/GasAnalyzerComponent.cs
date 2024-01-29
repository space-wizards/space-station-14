namespace Content.Server.Atmos.Components;

/// <summary>
/// Used to keep track of which analyzers are active for update purposes
/// </summary>
[RegisterComponent]
public sealed partial class ActiveGasAnalyzerComponent : Component
{
    // Set to a tiny bit after the default because otherwise the user often gets a blank window when first using
    [DataField("accumulatedFrameTime"), ViewVariables(VVAccess.ReadWrite)]
    public float AccumulatedFrametime = 2.01f;

    /// <summary>
    /// How often to update the analyzer
    /// </summary>
    [DataField("updateInterval"), ViewVariables(VVAccess.ReadWrite)]
    public float UpdateInterval = 1f;
}
