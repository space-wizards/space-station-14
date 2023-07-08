namespace Content.Server._FTL.OutpostATM;

/// <summary>
/// This is used for tracking outpost ATMs
/// </summary>
[RegisterComponent]
public sealed class OutpostATMComponent : Component
{
    /// <summary>
    /// Whether you can access the ATM or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public bool Enabled;
}
