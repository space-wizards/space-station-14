using Content.Server._FTL.Economy;

namespace Content.Server._FTL.Economy;

/// <summary>
/// This is used for tracking outpost ATMs
/// </summary>
[RegisterComponent, Access(typeof(EconomySystem))]
public sealed class OutpostATMComponent : Component
{
    /// <summary>
    /// Whether you can access the ATM or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public bool Enabled;
}
