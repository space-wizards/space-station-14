namespace Content.Server.Cargo.Components;

[RegisterComponent]
public sealed partial class CargoTrackingComponent: Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int? TrackedOrderId;
}
