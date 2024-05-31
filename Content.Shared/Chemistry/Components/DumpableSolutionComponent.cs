using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
///     Denotes the solution that can be easily dumped into (completely removed from the dumping container into this one)
///     Think pouring a container fully into this.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DumpableSolutionComponent : Component
{
    /// <summary>
    /// Solution name that can be dumped into.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "default";

    /// <summary>
    /// Whether the solution can be dumped into infinitely.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Unlimited = false;
}
