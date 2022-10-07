namespace Content.Server.Cargo.Components;

/// <summary>
/// This is used for pricing solutions contained in items.
/// </summary>
[RegisterComponent]
public sealed class SolutionPriceComponent : Component
{
    /// <summary>
    /// the solution to get price from
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solution")]
    public string Solution { get; set; } = "default";
}
