namespace Content.Server.Cargo.Components;

/// <summary>
/// This is used for setting a static, unchanging price for an object.
/// </summary>
[RegisterComponent]
public sealed partial class StaticPriceComponent : Component
{
    /// <summary>
    /// The price of the object this component is on.
    /// </summary>
    [DataField("price", required: true)]
    public double Price;
}
