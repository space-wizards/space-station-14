namespace Content.Shared.Bank.Components;

/// <summary>
/// This is used for applying a pricing modifier to things like vending machines.
/// It's used to ensure that a purchased product costs more than it is actually worth.
/// The float is applied to the StaticPrice component in the various systems that utilize it.
/// </summary>
[RegisterComponent]
public sealed class MarketModifierComponent : Component
{
    /// <summary>
    /// The amount to multiply a Static Price by
    /// </summary>
    [DataField("mod", required: true)]
    public float Mod;
}
