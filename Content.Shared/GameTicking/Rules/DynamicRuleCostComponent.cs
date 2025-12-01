namespace Content.Shared.GameTicking.Rules;

/// <summary>
/// Component that tracks how much a rule "costs" for Dynamic
/// </summary>
[RegisterComponent]
public sealed partial class DynamicRuleCostComponent : Component
{
    /// <summary>
    /// The amount of budget a rule takes up
    /// </summary>
    [DataField(required: true)]
    public int Cost;
}
