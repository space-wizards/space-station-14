namespace Content.Server.Cargo.Components;

[RegisterComponent]
public sealed partial class CargoTrackedComponent: Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int OrderId;

    public CargoTrackedComponent(int orderId)
    {
        OrderId = orderId;
    }
}
