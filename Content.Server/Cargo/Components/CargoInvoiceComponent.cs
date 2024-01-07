namespace Content.Server.Cargo.Components;

[RegisterComponent]
public sealed partial class CargoInvoiceComponent: Component
{
    /// <summary>
    ///  The order id this invoice is related to.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int? OrderId;

    /// <summary>
    ///  The name of the order this invoice is related to.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string? OrderName;
}
