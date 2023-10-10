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
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solution")]
    public string Solution { get; set; } = "default";

    /// <summary>
    /// Whether the solution can be dumped into infinitely.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("unlimited")]
    public bool Unlimited { get; set; } = false;
}
